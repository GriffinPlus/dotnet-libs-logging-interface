///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Uniform view over dictionary-like objects to avoid duplicating format logic.
/// </summary>
interface IDictionaryAdapter
{
	/// <summary>
	/// Returns the count if cheap to obtain; otherwise <see langword="null"/>.
	/// </summary>
	/// <param name="instance">The dictionary-like instance.</param>
	/// <returns>The count or <see langword="null"/>.</returns>
	int? TryGetCount(object instance);

	/// <summary>
	/// Enumerates key/value pairs as boxed objects.
	/// </summary>
	/// <param name="instance">The dictionary-like instance.</param>
	/// <returns>An enumeration of key/value pairs.</returns>
	IEnumerable<(object? Key, object? Value)> Enumerate(object instance);
}

static class DictionaryAdapter
{
	/// <summary>
	/// Logger instance for this class.
	/// </summary>
	private static readonly LogWriter sLog = LogWriter.Get(typeof(PrettyFormatter));

	/// <summary>
	/// A thread-safe cache that associates a <see cref="Type"/> with a function capable of  converting an object to an
	/// <see cref="IDictionaryAdapter"/> instance.
	/// </summary>
	/// <remarks>
	/// This cache uses <see cref="ConditionalWeakTable{TKey,TValue}"/> to ensure that the associations are automatically
	/// removed when the key (a <see cref="Type"/>) is no longer  referenced elsewhere. This helps prevent memory leaks
	/// in scenarios where dynamic type-to-function mappings are required.
	/// </remarks>
	private static readonly ConditionalWeakTable<Type, Func<object, IDictionaryAdapter?>> sCache = new();

	/// <summary>
	/// Ensures the AOT warning is logged only once per process.
	/// </summary>
	private static int sAotWarningLogged = 0; // 0 = not yet logged, 1 = logged

	/// <summary>
	/// Tries to create (or retrieve from cache) an adapter for the runtime type.<br/>
	/// Returns <see langword="null"/> if the object is not dictionary-like.
	/// </summary>
	/// <param name="instance">The dictionary-like instance to adapt.</param>
	/// <returns>
	/// An <see cref="IDictionaryAdapter"/> instance if <paramref name="instance"/> is dictionary-like;
	/// </returns>
	public static IDictionaryAdapter? TryCreate(object instance)
	{
		Func<object, IDictionaryAdapter?> factory = sCache.GetValue(instance.GetType(), BuildFactory);
		return factory(instance);
	}

	/// <summary>
	/// Creates a factory method that generates an <see cref="IDictionaryAdapter"/> instance for the specified dictionary
	/// type.
	/// </summary>
	/// <param name="type">
	/// The type of the dictionary for which the factory method is to be created.
	/// </param>
	/// <returns>
	/// A function that takes an object and returns an <see cref="IDictionaryAdapter"/> instance if the type
	/// <paramref name="type"/> represents a dictionary-like structure; otherwise, returns <see langword="null"/>.
	/// </returns>
	/// <remarks>
	/// The method attempts to create a high-performance, compiled expression. If this fails (e.g., in AOT
	/// environments), it provides a slower, reflection-based fallback factory.
	/// </remarks>
	private static Func<object, IDictionaryAdapter?> BuildFactory(Type type)
	{
		// 1) Non-generic IDictionary (remains unchanged)
		if (typeof(IDictionary).IsAssignableFrom(type))
		{
			return obj => new NonGenericAdapter((IDictionary)obj);
		}

		// 2) Generic IDictionary<,> / IReadOnlyDictionary<,> (remains unchanged)
		Type? iDict = type.GetInterfaces()
			.FirstOrDefault(i =>
				i.IsGenericType &&
				(i.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
				 i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));

		if (iDict != null)
		{
			Type[] args = iDict.GetGenericArguments(); // TKey, TValue
			Type adapterType = typeof(GenericAdapter<,>).MakeGenericType(args[0], args[1]);
			ConstructorInfo ctor = adapterType.GetConstructor([typeof(object)])!;

			try
			{
				// --- Fast Path: Expression.Compile() ---

				// (object obj)
				ParameterExpression paramObj = Expression.Parameter(typeof(object), "obj");

				// new GenericAdapter<TKey, TValue>(obj)
				NewExpression newExpr = Expression.New(ctor, paramObj);

				// (IDictionaryAdapter)new GenericAdapter<TKey, TValue>(obj)
				UnaryExpression castExpr = Expression.Convert(newExpr, typeof(IDictionaryAdapter));

				// Compile the lambda
				Expression<Func<object, IDictionaryAdapter?>> lambda = Expression.Lambda<Func<object, IDictionaryAdapter?>>(castExpr, paramObj);
				return lambda.Compile();
			}
			catch (Exception ex)
			{
				// --- Fallback Path: Reflection-based ConstructorInfo.Invoke ---
				// Expression.Compile() failed (likely an AOT environment).
				// We log this once and return a slower delegate
				// that invokes the constructor via reflection.

				LogAotWarningOnce(ex);

				return obj =>
				{
					try
					{
						// Use the cached constructor.
						// ctor.Invoke is the reflection fallback for "new()".
						return (IDictionaryAdapter)ctor.Invoke([obj]);
					}
					catch
					{
						// Should not happen, but included for safety.
						return null;
					}
				};
			}
		}

		// not dictionary-like
		return _ => null;
	}

	/// <summary>
	/// Logs a one-time warning about the AOT fallback.
	/// </summary>
	/// <param name="ex">The exception that caused the AOT fallback.</param>
	private static void LogAotWarningOnce(Exception ex)
	{
		// Thread-safe "execute only once"
		if (Interlocked.CompareExchange(ref sAotWarningLogged, 1, 0) == 0)
		{
			// Log the AOT warning
			// The message does not use the PrettyFormatter to avoid recursion.
			sLog.Write(
				LogLevel.Warning,
				"[{0}] Expression.Compile() failed (likely an AOT environment). Dictionary formatting is falling back to slower reflection. Error: {1}",
				typeof(PrettyFormatter).FullName,
				ex.Message);
		}
	}

	/// <summary>
	/// Provides an adapter for non-generic <see cref="IDictionary"/> instances, enabling operations such as retrieving
	/// the count and enumerating key-value pairs in a consistent manner.
	/// </summary>
	/// <remarks>
	/// This class is designed to work with non-generic <see cref="IDictionary"/> implementations, providing a unified
	/// interface for interacting with such collections. It is a sealed class and cannot be inherited.
	/// </remarks>
	private sealed class NonGenericAdapter : IDictionaryAdapter
	{
		private readonly IDictionary mDict;

		/// <summary>
		/// Initializes a new instance of the <see cref="NonGenericAdapter"/> class, wrapping a non-generic
		/// <see cref="IDictionary"/> to provide adapter functionality.
		/// </summary>
		/// <param name="dict">
		/// The non-generic <see cref="IDictionary"/> to be wrapped.
		/// This dictionary provides the underlying data storage for the adapter.
		/// </param>
		public NonGenericAdapter(IDictionary dict)
		{
			mDict = dict;
		}

		/// <summary>
		/// Attempts to retrieve the count of items in the dictionary.
		/// </summary>
		/// <param name="_">This parameter is not used and can be ignored.</param>
		/// <returns>
		/// The number of items in the dictionary, or <see langword="null"/> if the count cannot be determined.
		/// </returns>
		public int? TryGetCount(object _) => mDict.Count;

		/// <summary>
		/// Enumerates the key-value pairs in the dictionary.
		/// </summary>
		/// <param name="_">This parameter is unused and has no effect on the method's behavior.</param>
		/// <returns>
		/// An enumerable collection of tuples, where each tuple contains a key and its associated value from the dictionary.
		/// </returns>
		/// <remarks>
		/// The method yields each key-value pair as a tuple in the order they are stored in the dictionary.
		/// </remarks>
		public IEnumerable<(object? Key, object? Value)> Enumerate(object _)
		{
			foreach (DictionaryEntry e in mDict)
			{
				yield return (e.Key, e.Value);
			}
		}
	}

	/// <summary>
	/// Provides an adapter for generic dictionary-like objects, enabling access to their key-value pairs and count
	/// in a non-generic manner.
	/// </summary>
	/// <typeparam name="TKey">The type of the keys in the underlying dictionary.</typeparam>
	/// <typeparam name="TValue">The type of the values in the underlying dictionary.</typeparam>
	/// <remarks>
	/// This adapter is designed to work with objects that implement either <see cref="IReadOnlyCollection{T}"/> or
	/// <see cref="ICollection{T}"/> for key-value pairs. It provides a way to interact with such objects without
	/// requiring compile-time knowledge of their generic types.
	/// </remarks>
	private sealed class GenericAdapter<TKey, TValue> : IDictionaryAdapter
	{
		private readonly object mDict; // keeps underlying instance

		/// <summary>
		/// Initializes a new instance of the <see cref="GenericAdapter{TKey,TValue}"/> class with the specified dictionary object.
		/// </summary>
		/// <param name="dict">
		/// The dictionary object to be adapted. This object is expected to provide the data or functionality that the
		/// adapter will expose.
		/// </param>
		public GenericAdapter(object dict)
		{
			mDict = dict;
		}

		/// <summary>
		/// Attempts to retrieve the number of key-value pairs in the dictionary.
		/// </summary>
		/// <param name="_">An unused parameter. This parameter is ignored.</param>
		/// <returns>
		/// The number of key-value pairs in the dictionary if the underlying collection supports counting;
		/// otherwise, <see langword="null"/>.
		/// </returns>
		/// <remarks>
		/// This method checks if the underlying dictionary implements <see cref="IReadOnlyCollection{T}"/>  or
		/// <see cref="ICollection{T}"/> to determine if the count can be retrieved. If neither interface  is implemented,
		/// the method returns <see langword="null"/>.
		/// </remarks>
		public int? TryGetCount(object _)
		{
			return mDict switch
			{
				IReadOnlyCollection<KeyValuePair<TKey, TValue>> roc => roc.Count,
				ICollection<KeyValuePair<TKey, TValue>> c           => c.Count,
				var _                                               => null
			};
		}

		/// <summary>
		/// Enumerates the key-value pairs in the collection.
		/// </summary>
		/// <param name="_">This parameter is not used and can be ignored.</param>
		/// <returns>
		/// An <see cref="IEnumerable{T}"/> of tuples, where each tuple contains a key and a value from the collection.
		/// </returns>
		/// <remarks>
		/// The method yields each key-value pair as a tuple in the order they appear in the collection.
		/// </remarks>
		public IEnumerable<(object? Key, object? Value)> Enumerate(object _)
		{
			// Safe cast is guaranteed by BuildFactory()
			foreach (KeyValuePair<TKey, TValue> kv in (IEnumerable<KeyValuePair<TKey, TValue>>)mDict)
			{
				yield return (kv.Key, kv.Value);
			}
		}
	}
}
