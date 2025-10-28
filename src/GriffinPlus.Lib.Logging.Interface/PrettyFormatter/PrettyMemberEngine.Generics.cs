using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

// ReSharper disable LoopCanBeConvertedToQuery

namespace GriffinPlus.Lib.Logging;

static partial class PrettyMemberEngine
{
	/// <summary>
	/// A thread-safe cache that associates types with their corresponding unmanaged constraint evaluation results.
	/// </summary>
	/// <remarks>
	/// This cache is used to store and retrieve the results of evaluating whether a given type satisfies
	/// the unmanaged constraint. The use of <see cref="ConditionalWeakTable{TKey,TValue}"/> ensures that the entries
	/// are automatically removed when the key (type) is no longer referenced elsewhere, preventing memory leaks.
	/// </remarks>
	private static readonly ConditionalWeakTable<Type, object> sUnmanagedConstraintCache = new();

	/// <summary>
	/// Builds a whitespace-separated block of C# <c>where</c>-clauses for the given type parameters,
	/// honoring constraint flags and explicit base/interface constraints.
	/// </summary>
	/// <param name="genericTypeParameters">The generic type parameters to inspect.</param>
	/// <param name="options">Member formatting options (used for type-name formatting).</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A concatenated string of <c>where</c>-clauses, or an empty string if none apply.
	/// </returns>
	private static string BuildWhereClauses(Type[] genericTypeParameters, PrettyMemberOptions options, TextFormatContext tfc)
	{
		var clauses = new List<string>();
		foreach (Type type in genericTypeParameters)
		{
			if (!type.IsGenericParameter) continue;

			var constraints = new List<string>();
			GenericParameterAttributes genericParameterAttributes = type.GenericParameterAttributes;

			if ((genericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0) constraints.Add("class");
			if ((genericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) constraints.Add("struct");

			// Unmanaged constraint: encoded via System.Runtime.CompilerServices.IsUnmanagedAttribute on the generic parameter.
			// Use a cached attribute lookup to keep this fast in hot paths.
			if (HasUnmanagedConstraint(type))
				constraints.Add("unmanaged");

			foreach (Type constraint in type.GetGenericParameterConstraints())
			{
				PrettyTypeOptions typeOptions = options.UseNamespaceForTypes ? PrettyTypePresets.Full : PrettyTypePresets.Compact;
				constraints.Add(PrettyTypeEngine.Format(constraint, typeOptions, tfc));
			}

			if ((genericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
				constraints.Add("new()");

			if (constraints.Count > 0)
				clauses.Add("where " + type.Name + " : " + string.Join(", ", constraints));
		}
		return string.Join(" ", clauses);
	}

	/// <summary>
	/// Returns <see langword="true"/> if the generic type parameter has the C# <c>unmanaged</c> constraint.
	/// </summary>
	/// <param name="type">A <see cref="Type"/> representing a generic type parameter.</param>
	/// <remarks>
	/// The C# compiler encodes <c>where T : unmanaged</c> by attaching the marker attribute
	/// <c>System.Runtime.CompilerServices.IsUnmanagedAttribute</c> to the generic type parameter.
	/// There is NO <see cref="GenericParameterAttributes"/> flag for this constraint.
	/// This method detects the marker via <see cref="CustomAttributeData"/> and caches the result
	/// to keep performance O(1) for repeated queries in hot logging paths.
	/// </remarks>
	/// <returns>
	/// <see langword="true"/> if the unmanaged constraint is present;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool HasUnmanagedConstraint(Type type)
	{
		// Only generic parameters can carry this marker.
		if (!type.IsGenericParameter) return false;

		// Check the cache first, compute and cache the value, if not present.
		object boxedBool = sUnmanagedConstraintCache.GetValue(
			type,
			static t => ComputeHasUnmanaged(t) ? sTrueBox : sFalseBox);
		return ReferenceEquals(boxedBool, sTrueBox);

		// Computes whether the unmanaged constraint is present.
		// This method is invoked only once per generic type parameter.
		// It uses reflection to inspect the custom attributes of the type parameter.
		// Defensive coding is applied to ignore any reflection issues.
		static bool ComputeHasUnmanaged(Type t)
		{
			bool found = false;
			try
			{
				IList<CustomAttributeData> attributes = t.GetCustomAttributesData();
				// ReSharper disable once ForCanBeConvertedToForeach
				for (int i = 0; i < attributes.Count; i++)
				{
					Type attributeType = attributes[i].AttributeType;

					// Check for IsUnmanagedAttribute without allocating new strings.
					// DO NOT use FullName here as it may be null for open generics / dynamic types.
					// Furthermore, FullName allocates a new string.
					if (string.Equals(attributeType.Name, "IsUnmanagedAttribute", StringComparison.Ordinal) &&
					    string.Equals(attributeType.Namespace, "System.Runtime.CompilerServices", StringComparison.Ordinal))
					{
						found = true;
						break;
					}
				}
			}
			catch
			{
				// Defensive: ignore reflection issues; treat as "not unmanaged".
			}
			return found;
		}
	}
}
