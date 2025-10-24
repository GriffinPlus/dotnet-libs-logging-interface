///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Internal engine for <see cref="Type"/> formatting (aliases, arrays, pointers, by-ref, tuples,
/// nullable value types, generics, nested types, and anonymous types).
/// </summary>
/// <remarks>
/// The engine is pure and thread-safe. It does not maintain mutable state.
/// </remarks>
static class PrettyTypeEngine
{
	/// <summary>
	/// Formats a <see cref="Type"/> into a C#-like representation, honoring the provided <paramref name="options"/>.
	/// </summary>
	/// <param name="type">
	/// The runtime type to render. If <see langword="null"/>, the literal string <c>"&lt;null&gt;"</c> is returned.
	/// </param>
	/// <param name="options">
	/// Type formatting options. Must not be <see langword="null"/> (callers pass a preset if needed).<br/>
	/// Only <see cref="PrettyTypeOptions.UseNamespace"/> is currently consumed.
	/// </param>
	/// <returns>
	/// A readable type name (optionally namespace-qualified) including array ranks, generic arguments,
	/// tuple syntax, nullable suffix for value types, and nested type chains where applicable.
	/// </returns>
	/// <remarks>
	/// <b>ByRef types:</b> When <paramref name="type"/> is a ByRef type (for example <c>System.String&</c>),
	/// the type formatter emits the CLR-style suffix <c>&</c> (e.g., <c>string&</c>).
	/// C#-specific parameter modifiers (<c>ref</c>, <c>out</c>, <c>in</c>) are added later
	/// by the member formatter (<see cref="PrettyMemberEngine"/>).
	/// </remarks>
	public static string Format(Type? type, PrettyTypeOptions options)
	{
		if (type == null) return "<null>";
		var builder = new StringBuilder(64);
		AppendType(builder, type, options);
		return builder.ToString();
	}

	/// <summary>
	/// Internal recursive workhorse for Format(). Appends the formatted type to the builder.
	/// </summary>
	/// <param name="builder">The string builder to append the formatted type to.</param>
	/// <param name="type">
	/// The runtime type to render. If <see langword="null"/>, the literal string <c>"&lt;null&gt;"</c> is returned.
	/// </param>
	/// <param name="options">Type formatting options.</param>
	internal static void AppendType(StringBuilder builder, Type? type, PrettyTypeOptions options)
	{
		// Null
		if (type == null)
		{
			builder.Append("<null>");
			return;
		}

		// C# keyword aliases
		if (TryAlias(type, out string? alias))
		{
			builder.Append(alias);
			return;
		}

		// ByRef / Pointer
		if (type.IsByRef)
		{
			AppendType(builder, type.GetElementType()!, options);
			builder.Append('&');
			return;
		}
		if (type.IsPointer)
		{
			AppendType(builder, type.GetElementType()!, options);
			builder.Append('*');
			return;
		}

		// Arrays ([], [,], [,,], …)
		if (type.IsArray)
		{
			Type? elem = type.GetElementType();
			int rank = type.GetArrayRank();
			string suffix = rank == 1 ? "[]" : "[" + new string(',', rank - 1) + "]";
			AppendType(builder, elem!, options);
			builder.Append(suffix);
			return;
		}

		// Nullable<T> => T?
		if (IsNullableValue(type, out Type? underlying))
		{
			AppendType(builder, underlying!, options);
			builder.Append('?');
			return;
		}

		// Tuples => (T1, T2, …)
		if (IsTuple(type))
		{
			builder.Append('(');
			bool first = true;
			foreach (Type tupleType in FlattenTupleArgs(type))
			{
				if (!first) builder.Append(", ");
				AppendType(builder, tupleType, options); // Recurse
				first = false;
			}
			builder.Append(')');
			return;
		}

		// Anonymous types => anonymous{ Prop1, Prop2, … }
		if (IsAnonymous(type))
		{
			builder.Append("anonymous{ ");
			PropertyInfo[] props = type.GetProperties();
			for (int i = 0; i < props.Length; i++)
			{
				if (i > 0) builder.Append(", ");
				builder.Append(props[i].Name);
			}
			builder.Append(" }");
			return;
		}

		// Generic parameter (T, TKey, …)
		if (type.IsGenericParameter)
		{
			builder.Append(type.Name);
			return;
		}

		// Closed/open generics and nested types
		if (type.IsGenericType)
		{
			AppendGenericType(builder, type, options);
			return;
		}

		bool useNamespace = options.UseNamespace;

		// Non-generic nested type
		if (type.DeclaringType != null)
		{
			AppendType(builder, type.DeclaringType, options); // Recurse
			builder.Append('.').Append(type.Name);
			return;
		}

		// Simple non-generic
		if (useNamespace && !string.IsNullOrEmpty(type.Namespace))
		{
			builder.Append(type.Namespace).Append('.').Append(type.Name);
		}
		else
		{
			builder.Append(type.Name);
		}
	}

	/// <summary>
	/// C# keyword aliases for common CLR types.
	/// </summary>
	private static readonly Dictionary<Type, string> sTypeAliases = new()
	{
		{ typeof(void), "void" },
		{ typeof(string), "string" },
		{ typeof(bool), "bool" },
		{ typeof(byte), "byte" },
		{ typeof(sbyte), "sbyte" },
		{ typeof(short), "short" },
		{ typeof(ushort), "ushort" },
		{ typeof(int), "int" },
		{ typeof(uint), "uint" },
		{ typeof(long), "long" },
		{ typeof(ulong), "ulong" },
		{ typeof(char), "char" },
		{ typeof(float), "float" },
		{ typeof(double), "double" },
		{ typeof(decimal), "decimal" },
		{ typeof(object), "object" }
	};

	/// <summary>
	/// Tries to map a CLR type to its C# keyword alias (e.g., <see cref="System.Int32"/> → <c>int</c>).
	/// </summary>
	/// <param name="type">The type to inspect.</param>
	/// <param name="name">
	/// When the method returns <see langword="true"/>, receives the alias string;<br/>
	/// otherwise, <see langword="null"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if an alias exists;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool TryAlias(Type type, out string? name)
	{
		return sTypeAliases.TryGetValue(type, out name);
	}

	/// <summary>
	/// Determines whether a type is a <see cref="Nullable{T}"/> value type and returns the underlying type.
	/// </summary>
	/// <param name="type">The type to inspect.</param>
	/// <param name="underlyingType">
	/// Receives the underlying non-nullable value type if <paramref name="type"/> is <c>Nullable&lt;T&gt;</c>;<br/>
	/// otherwise, <see langword="null"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="type"/> is <c>Nullable&lt;T&gt;</c>;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsNullableValue(Type type, out Type? underlyingType)
	{
		underlyingType = Nullable.GetUnderlyingType(type);
		return underlyingType != null;
	}

	/// <summary>
	/// Set of all <see cref="ValueTuple"/> generic type definitions.
	/// </summary>
	private static readonly HashSet<Type> sValueTupleDefinitions =
	[
		typeof(ValueTuple<>),
		typeof(ValueTuple<,>),
		typeof(ValueTuple<,,>),
		typeof(ValueTuple<,,,>),
		typeof(ValueTuple<,,,,>),
		typeof(ValueTuple<,,,,,>),
		typeof(ValueTuple<,,,,,,>),
		typeof(ValueTuple<,,,,,,,>)
	];

	/// <summary>
	/// Determines whether a type is a <see cref="System.ValueTuple"/>-based tuple definition ((T1,…,Tn) shape).
	/// </summary>
	/// <param name="type">The type to inspect.</param>
	/// <returns><see langword="true"/> if the type is a tuple; otherwise <see langword="false"/>.</returns>
	private static bool IsTuple(Type type)
	{
		return type.IsGenericType &&
		       sValueTupleDefinitions.Contains(type.GetGenericTypeDefinition());
	}

	/// <summary>
	/// Flattens the generic type arguments of a <see cref="System.ValueTuple"/> chain.
	/// </summary>
	/// <remarks>
	///     <para>
	///     Tuples with more than seven elements are represented by the runtime as a nested chain:
	///     <c>ValueTuple&lt;T1,…,T7, ValueTuple&lt;T8,…,Tn&gt;&gt;</c>. This helper recursively walks the
	///     trailing <c>TRest</c> argument when the arity is exactly eight and <c>TRest</c> is itself a
	///     <c>ValueTuple&lt;…&gt;</c>, yielding a single flattened sequence of element types.
	///     </para>
	///     <para>
	///     The order of elements is preserved. Non-tuple inputs are not expected; callers should guard with
	///     <see cref="IsTuple(Type)"/> before invoking this method.
	///     </para>
	/// </remarks>
	/// <param name="tuple">A <see cref="System.ValueTuple"/> type to flatten.</param>
	/// <returns>
	/// An ordered sequence of element types representing a single logical tuple.
	/// </returns>
	private static IEnumerable<Type> FlattenTupleArgs(Type tuple)
	{
		Type[] args = tuple.GetGenericArguments();
		if (args.Length == 8 && IsTuple(args[7]))
		{
			// 7 own elements + nested Rest tuple -> recurse
			for (int i = 0; i < 7; i++) yield return args[i];
			foreach (Type type in FlattenTupleArgs(args[7])) yield return type;
		}
		else
		{
			foreach (Type type in args) yield return type;
		}
	}

	/// <summary>
	/// Formats a generic (open or closed) type, including nested declaring-type chains,
	/// and only the generic arguments owned by the current type segment.
	/// </summary>
	/// <param name="builder">The target <see cref="StringBuilder"/>.</param>
	/// <param name="type">The generic type to format.</param>
	/// <param name="options">Type formatting options.</param>
	private static void AppendGenericType(StringBuilder builder, Type type, PrettyTypeOptions options)
	{
		bool useNamespace = options.UseNamespace;

		// Prefix with declaring type chain (already formatted correctly for nested generics)
		if (type.DeclaringType != null)
		{
			AppendType(builder, type.DeclaringType, options); // Recurse
			builder.Append('.');
		}
		else if (useNamespace && !string.IsNullOrEmpty(type.Namespace))
		{
			builder.Append(type.Namespace).Append('.');
		}

		// Name without arity suffix (`N)
		string name = type.Name;
		int arity = GetArityFromName(name);
		int tick = name.IndexOf('`');
		if (tick >= 0)
		{
			// Use the (string, int, int) overload to avoid Substring allocation
			builder.Append(name, 0, tick);
		}
		else
		{
			builder.Append(name);
		}

		// Only take this type's *own* generic arguments (not including declaring type args)
		Type[] allArgs = type.GetGenericArguments();
		if (arity > 0)
		{
			int start = allArgs.Length - arity;
			builder.Append('<');
			for (int i = 0; i < arity; i++)
			{
				if (i > 0) builder.Append(", ");
				Type a = allArgs[start + i];

				// A generic argument can be a parameter (T) or a concrete type (int)
				if (a.IsGenericParameter) builder.Append(a.Name);
				else AppendType(builder, a, options); // Recurse
			}
			builder.Append('>');
		}
	}

	/// <summary>
	/// Extracts the generic arity (the number after the backtick in a CLR type name).
	/// </summary>
	/// <param name="name">The CLR type name, e.g. <c>"Dictionary`2"</c>.</param>
	/// <returns>
	/// The parsed arity (non-negative). Returns <c>0</c> if no arity suffix is present or parsing fails.
	/// </returns>
	private static int GetArityFromName(string name)
	{
		int i = name.IndexOf('`');
		if (i < 0) return 0;

#if NET6_0_OR_GREATER
		// Use ReadOnlySpan<char> to avoid allocating a substring
		return int.TryParse(name.AsSpan(i + 1), out int n) ? n : 0;
#else
		// Fallback for older targets
		// ReSharper disable once ReplaceSubstringWithRangeIndexer
		return int.TryParse(name.Substring(i + 1), out int n) ? n : 0;
#endif
	}

	/// <summary>
	/// Heuristically detects compiler-generated anonymous types by attribute and naming patterns.
	/// </summary>
	/// <param name="type">The type to inspect.</param>
	/// <returns>
	/// <see langword="true"/> if the type looks like an anonymous type from C# or VB.NET;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsAnonymous(Type type)
	{
		return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
		       && type.IsGenericType
		       && type.Name.IndexOf("AnonymousType", StringComparison.Ordinal) >= 0
		       && (type.Name.StartsWith("<>", StringComparison.Ordinal) ||
		           type.Name.StartsWith("VB$", StringComparison.Ordinal))
		       && type is { IsPublic: false, IsNestedPublic: false };
	}
}
