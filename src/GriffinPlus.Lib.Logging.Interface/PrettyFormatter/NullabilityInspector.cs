///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// ReSharper disable RedundantUsingDirective

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Collections.Generic;

// ReSharper restore RedundantUsingDirective

#pragma warning disable CS1574, CS1584, CS1581, CS1580

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Describes the inferred nullability state for a type or member.
/// </summary>
/// <remarks>
///     <para>
///     Values align with Roslyn's nullability model:
///     <list type="bullet">
///         <item>
///             <description>
///             <see cref="Unknown"/>: No reliable information found (e.g., no attributes, old compiler, or assemblies compiled without <c>#nullable</c>
///             ).
///             </description>
///         </item>
///         <item>
///             <description><see cref="Oblivious"/>: Legacy / oblivious context — compiler does not enforce nullability.</description>
///         </item>
///         <item>
///             <description><see cref="NonNullable"/>: Explicitly non-nullable (e.g., <c>string</c> under an enabled <c>#nullable</c> context).</description>
///         </item>
///         <item>
///             <description><see cref="Nullable"/>: Explicitly nullable (e.g., <c>string?</c> or <c>int?</c>).</description>
///         </item>
///     </list>
///     Note: For value types (structs), <see cref="Nullable"/> is returned only for <c>Nullable&lt;T&gt;</c> (<c>T?</c>).
///     </para>
/// </remarks>
public enum Nullability
{
	/// <summary>
	/// No information could be determined.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Oblivious/legacy context; the compiler assumes neither nullable nor non-nullable.
	/// </summary>
	Oblivious = 1,

	/// <summary>
	/// Explicitly non-nullable.
	/// </summary>
	NonNullable = 2,

	/// <summary>
	/// Explicitly nullable.
	/// </summary>
	Nullable = 3
}

/// <summary>
/// Cross-target utility to inspect nullability for types and members.
/// </summary>
/// <remarks>
///     <para>
///     The <see cref="NullabilityInspector"/> determines the nullability state of reflected symbols
///     (such as <see cref="System.Type"/>, <see cref="System.Reflection.PropertyInfo"/>,
///     <see cref="System.Reflection.ParameterInfo"/>, etc.) by analyzing compiler-emitted attributes
///     and contextual metadata.
///     </para>
///     <para>
///     On .NET 6 and later, the implementation uses <see cref="System.Reflection.NullabilityInfoContext"/> internally,
///     which provides accurate nullability information derived from compiler annotations.
///     On older target frameworks (e.g., .NET Framework 4.8 or .NET Standard 2.0),
///     it falls back to heuristic inspection of attributes such as <c>NullableAttribute</c> and
///     <c>NullableContextAttribute</c>. In those environments, the result may occasionally be reported as
///     <see cref="NullabilityState.Unknown"/> if the required metadata is missing.
///     </para>
///     <para>
///     The class is thread-safe and designed for use in reflection-heavy diagnostic scenarios,
///     such as the pretty-printing of type signatures or runtime inspection of API surfaces.
///     </para>
///     <example>
///         <code language="csharp">
/// // Example: Inspect the nullability of a property
/// var prop = typeof(MyType).GetProperty(nameof(MyType.Name));
/// var info = NullabilityInspector.GetPropertyNullability(prop);
/// Console.WriteLine($"Property '{prop.Name}' → {info}");
/// 
/// // Example: Inspect method parameters
/// var method = typeof(MyService).GetMethod(nameof(MyService.DoSomething));
/// foreach (var p in method.GetParameters())
/// {
///     var state = NullabilityInspector.GetParameterNullability(p);
///     Console.WriteLine($"{p.Name}: {state}");
/// }
/// </code>
///     </example>
/// </remarks>
public static class NullabilityInspector
{
#if NET6_0_OR_GREATER
	/// <summary>
	/// Cached, thread-safe context (single instance for the whole process)
	/// </summary>
	private static readonly NullabilityInfoContext sContext = new();
#else
	private static readonly ConcurrentDictionary<ICustomAttributeProvider, Nullability> sNullabilityCache = new();
#endif

	/// <summary>
	/// Gets the nullability of a <see cref="PropertyInfo"/>.
	/// </summary>
	/// <param name="property">The property to inspect.</param>
	/// <returns>
	/// A <see cref="Nullability"/> value representing the property's nullability.
	/// For value types, <see cref="Nullability.Nullable"/> is only reported for <c>Nullable&lt;T&gt;</c>.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="property"/> is <see langword="null"/>.</exception>
	public static Nullability GetNullability(PropertyInfo property)
	{
		if (property == null) throw new ArgumentNullException(nameof(property));
		return GetNullabilityCore(property.PropertyType, member: property, returnParameter: null, parameter: null);
	}

	/// <summary>
	/// Gets the nullability of a <see cref="FieldInfo"/>.
	/// </summary>
	/// <param name="field">The field to inspect.</param>
	/// <returns>
	/// A <see cref="Nullability"/> value representing the field's nullability.
	/// For value types, <see cref="Nullability.Nullable"/> is only reported for <c>Nullable&lt;T&gt;</c>.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="field"/> is <see langword="null"/>.</exception>
	public static Nullability GetNullability(FieldInfo field)
	{
		if (field == null) throw new ArgumentNullException(nameof(field));
		return GetNullabilityCore(field.FieldType, member: field, returnParameter: null, parameter: null);
	}

	/// <summary>
	/// Gets the nullability of a method <see cref="ParameterInfo"/>.
	/// </summary>
	/// <param name="parameter">The parameter to inspect.</param>
	/// <returns>
	/// A <see cref="Nullability"/> value representing the parameter's nullability.
	/// For value types, <see cref="Nullability.Nullable"/> is only reported for <c>Nullable&lt;T&gt;</c>.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="parameter"/> is <see langword="null"/>.</exception>
	public static Nullability GetNullability(ParameterInfo parameter)
	{
		if (parameter == null) throw new ArgumentNullException(nameof(parameter));
		return GetNullabilityCore(parameter.ParameterType, member: parameter.Member, returnParameter: null, parameter: parameter);
	}

	/// <summary>
	/// Gets the nullability of a method return type.
	/// </summary>
	/// <param name="method">The method to inspect.</param>
	/// <returns>
	/// A <see cref="Nullability"/> value representing the return type's nullability.
	/// For value types, <see cref="Nullability.Nullable"/> is only reported for <c>Nullable&lt;T&gt;</c>.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> is <see langword="null"/>.</exception>
	public static Nullability GetReturnNullability(MethodInfo method)
	{
		if (method == null) throw new ArgumentNullException(nameof(method));
		return GetNullabilityCore(method.ReturnType, member: method, returnParameter: method.ReturnParameter, parameter: null);
	}

	/// <summary>
	/// Gets the nullability of an <see cref="EventInfo"/> delegate type.
	/// </summary>
	/// <param name="eventInfo">The event to inspect.</param>
	/// <returns>
	/// A <see cref="Nullability"/> value representing the event handler's nullability context.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="eventInfo"/> is <see langword="null"/>.</exception>
	/// <remarks>
	///     <para>
	///     On .NET 6 or newer, this uses <c>System.Reflection.NullabilityInfoContext</c> for accurate event metadata analysis.
	///     On older targets, the result is derived from compiler attributes, if available.
	///     </para>
	/// </remarks>
	public static Nullability GetNullability(EventInfo eventInfo)
	{
		if (eventInfo == null)
			throw new ArgumentNullException(nameof(eventInfo));

		// Defensive: EventHandlerType is annotated as non-null,
		// but can be null in rare dynamic/reflection scenarios.
		// ReSharper disable once RedundantSuppressNullableWarningExpression
		Type handlerType = eventInfo.EventHandlerType!;

		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (handlerType == null)
			return Nullability.Unknown;

		return GetNullabilityCore(handlerType, member: eventInfo, returnParameter: null, parameter: null);
	}

	/// <summary>
	/// Checks whether a <see cref="Type"/> is a <c>Nullable&lt;T&gt;</c> (i.e., <c>T?</c>).
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns><see langword="true"/> if <paramref name="type"/> is <c>Nullable&lt;T&gt;</c>; otherwise <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
	/// <remarks>This is always reliable for value types and does not rely on compiler attributes.</remarks>
	public static bool IsNullableValueType(Type type)
	{
		if (type == null) throw new ArgumentNullException(nameof(type));
		return Nullable.GetUnderlyingType(type) != null;
	}

	/// <summary>
	/// Internal core logic that resolves nullability using the best mechanism available for the target.
	/// </summary>
	/// <param name="type">The concrete type (property/field/parameter/return type).</param>
	/// <param name="member">The associated member providing context (if any).</param>
	/// <param name="returnParameter">Optional return <see cref="ParameterInfo"/> (for method returns).</param>
	/// <param name="parameter">Optional parameter <see cref="ParameterInfo"/> (for method parameters).</param>
	/// <returns>
	/// A <see cref="Nullability"/> value describing the inferred nullability for the specified element.
	/// </returns>
	/// <remarks>
	///     <para>
	///     This method is the internal workhorse for all public <c>GetNullability(...)</c> overloads.
	///     It expects the caller to pass the most specific available reflection context
	///     (for example, the <see cref="ParameterInfo"/> for a method parameter, or
	///     the <see cref="MethodInfo.ReturnParameter"/> for a return value).
	///     </para>
	///     <para>
	///     The method will attempt to infer nullability in the following order:
	///     value type → return parameter → method parameter → property → field → event → (fallback to unknown).
	///     On .NET 6 or newer, it uses a cached <see cref="NullabilityInfoContext"/> for optimal performance.
	///     On earlier frameworks, it decodes compiler-emitted attributes defensively.
	///     </para>
	///     <para>
	///     ⚠️ <b>Internal Use Only:</b>
	///     This routine assumes the caller has validated inputs and should normally be accessed
	///     only through the public <see cref="NullabilityInspector"/> API. Direct use is discouraged
	///     because missing context (e.g. passing a <see cref="MethodInfo"/> without its <see cref="MethodInfo.ReturnParameter"/>)
	///     can cause incomplete analysis.
	///     </para>
	/// </remarks>
	private static Nullability GetNullabilityCore(
		Type           type,
		MemberInfo     member,
		ParameterInfo? returnParameter,
		ParameterInfo? parameter)
	{
		// 1) Value types: Nullable<T> is always reliable.
		if (type.IsValueType)
			return Nullable.GetUnderlyingType(type) != null ? Nullability.Nullable : Nullability.NonNullable;

#if NET6_0_OR_GREATER
		// 2) .NET 6+: precise evaluation via NullabilityInfoContext
		if (returnParameter != null)
			return Map(sContext.Create(returnParameter).ReadState);

		if (parameter != null)
			return Map(sContext.Create(parameter).ReadState);

		if (member is PropertyInfo propertyInfo)
			return Map(sContext.Create(propertyInfo).ReadState);

		if (member is FieldInfo fieldInfo)
			return Map(sContext.Create(fieldInfo).ReadState);

		if (member is MethodInfo methodInfo && returnParameter is null && parameter is null)
			return Map(sContext.Create(methodInfo.ReturnParameter).ReadState);

		if (member is EventInfo eventInfo)
			return Map(sContext.Create(eventInfo).ReadState);

		// No supported member/parameter context available -> cannot infer ref-type nullability.
		return Nullability.Unknown;

		static Nullability Map(NullabilityState state) => state switch
		{
			NullabilityState.Nullable => Nullability.Nullable,
			NullabilityState.NotNull  => Nullability.NonNullable,
			var _                     => Nullability.Unknown
		};
#else
		// 2) < .NET 6: defensive decoding of compiler attributes (no hard type references)

		// Check the cache.
		ICustomAttributeProvider cacheKey = (ICustomAttributeProvider?)parameter ??
		                                    (ICustomAttributeProvider?)returnParameter ??
		                                    member;
		if (sNullabilityCache.TryGetValue(cacheKey, out Nullability cachedResult))
			return cachedResult;

		// Most-specific to least-specific lookup order:
		byte? flag =
			TryGetNullableFlag(returnParameter) ??
			TryGetNullableFlag(parameter) ??
			TryGetNullableFlag(member) ??
			TryGetNullableContextFlag(member) ??
			TryGetNullableContextFlag(member.DeclaringType) ??
			TryGetNullableContextFlag(member.Module) ??
			TryGetNullableContextFlag(member.Module.Assembly);

		Nullability result;
		if (!flag.HasValue)
		{
			result = Nullability.Unknown;
		}
		else
		{
			// Roslyn encoding: 1 = NonNullable, 2 = Nullable, 0 = Oblivious
			switch (flag.Value)
			{
				case 1:  result = Nullability.NonNullable; break;
				case 2:  result = Nullability.Nullable; break;
				case 0:  result = Nullability.Oblivious; break;
				default: result = Nullability.Unknown; break;
			}
		}

		sNullabilityCache.TryAdd(cacheKey, result);
		return result;
#endif
	}

#if !NET6_0_OR_GREATER
	/// <summary>
	/// Attempts to read a <c>NullableAttribute</c> flag from an attribute provider.
	/// </summary>
	/// <param name="provider">
	/// An <see cref="ICustomAttributeProvider"/> such as <see cref="ParameterInfo"/>, <see cref="MemberInfo"/>,
	/// <see cref="Module"/>, or <see cref="Assembly"/>.
	/// </param>
	/// <returns>
	/// A <c>byte</c> flag per Roslyn encoding (0 = Oblivious, 1 = NonNullable, 2 = Nullable),
	/// or <see langword="null"/> if not present.
	/// </returns>
	/// <remarks>
	/// The constructor of <c>NullableAttribute</c> is either <c>.ctor(byte)</c> or <c>.ctor(byte[])</c>.
	/// For common top-level cases (e.g., <c>string?</c>), the first byte is sufficient.
	/// Complex generic trees may require extended mapping logic.
	/// </remarks>
	private static byte? TryGetNullableFlag(ICustomAttributeProvider? provider)
	{
		if (provider == null) return null;

		foreach (CustomAttributeData? cad in GetCustomAttributes(provider))
		{
			if (cad.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute")
			{
				if (cad.ConstructorArguments.Count > 0)
				{
					CustomAttributeTypedArgument arg = cad.ConstructorArguments[0];

					// .ctor(byte)
					if (arg.ArgumentType == typeof(byte))
						return (byte)arg.Value;

					// .ctor(byte[])
					if (arg.Value is IList<CustomAttributeTypedArgument> { Count: > 0 } array &&
					    array[0].ArgumentType == typeof(byte))
					{
						return (byte)array[0].Value;
					}
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Attempts to read a <c>NullableContextAttribute</c> flag (context default) from an attribute provider.
	/// </summary>
	/// <param name="provider">
	/// An <see cref="ICustomAttributeProvider"/> such as <see cref="MemberInfo"/>, <see cref="Type"/>,
	/// <see cref="Module"/>, or <see cref="Assembly"/>.
	/// </param>
	/// <returns>
	/// A <c>byte</c> flag per Roslyn encoding (0 = Oblivious, 1 = NonNullable, 2 = Nullable),
	/// or <see langword="null"/> if not present.
	/// </returns>
	private static byte? TryGetNullableContextFlag(ICustomAttributeProvider? provider)
	{
		if (provider == null) return null;

		foreach (CustomAttributeData? cad in GetCustomAttributes(provider))
		{
			if (cad.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute")
			{
				if (cad.ConstructorArguments.Count > 0)
				{
					CustomAttributeTypedArgument arg = cad.ConstructorArguments[0];
					if (arg.ArgumentType == typeof(byte))
						return (byte)arg.Value;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Retrieves a sequence of <see cref="CustomAttributeData"/> for various provider types
	/// without requiring hard references to the attribute classes.
	/// </summary>
	/// <param name="provider">The attribute provider.</param>
	/// <returns>An enumerable of <see cref="CustomAttributeData"/>.</returns>
	private static IEnumerable<CustomAttributeData> GetCustomAttributes(ICustomAttributeProvider provider)
	{
		if (provider is ParameterInfo pi) return pi.CustomAttributes;
		if (provider is MemberInfo mi) return mi.CustomAttributes;
		if (provider is Module m) return CustomAttributeData.GetCustomAttributes(m);
		if (provider is Assembly a) return CustomAttributeData.GetCustomAttributes(a);
		return [];
	}
#endif
}
