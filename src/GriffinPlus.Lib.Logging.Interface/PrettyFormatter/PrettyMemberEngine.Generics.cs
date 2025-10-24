using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable LoopCanBeConvertedToQuery

namespace GriffinPlus.Lib.Logging;

static partial class PrettyMemberEngine
{
	private static readonly ConcurrentDictionary<Type, bool> sUnmanagedConstraintCache = new();

	/// <summary>
	/// Builds a whitespace-separated block of C# <c>where</c>-clauses for the given type parameters,
	/// honoring constraint flags and explicit base/interface constraints.
	/// </summary>
	/// <param name="genericTypeParameters">The generic type parameters to inspect.</param>
	/// <param name="opts">Member formatting options (used for type-name formatting).</param>
	/// <returns>
	/// A concatenated string of <c>where</c>-clauses, or an empty string if none apply.
	/// </returns>
	private static string BuildWhereClauses(Type[] genericTypeParameters, PrettyMemberOptions opts)
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
				constraints.Add(PrettyTypeEngine.Format(constraint, new PrettyTypeOptions { UseNamespace = opts.UseNamespaceForTypes }));
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
	/// <see langword="true"/> if the unmanaged constraint is present; otherwise <see langword="false"/>.
	/// </returns>
	private static bool HasUnmanagedConstraint(Type type)
	{
		// Only generic parameters can carry this marker.
		if (!type.IsGenericParameter) return false;

		if (sUnmanagedConstraintCache.TryGetValue(type, out bool cached))
			return cached;

		bool found = false;
		try
		{
			IList<CustomAttributeData> attributes = type.GetCustomAttributesData();
			// ReSharper disable once ForCanBeConvertedToForeach
			for (int i = 0; i < attributes.Count; i++)
			{
				Type attributeType = attributes[i].AttributeType;
				if (string.Equals(attributeType.FullName, "System.Runtime.CompilerServices.IsUnmanagedAttribute", StringComparison.Ordinal))
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

		sUnmanagedConstraintCache[type] = found;
		return found;
	}
}
