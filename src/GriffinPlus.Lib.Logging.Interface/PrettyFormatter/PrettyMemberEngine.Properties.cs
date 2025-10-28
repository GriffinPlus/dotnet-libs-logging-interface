using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery

namespace GriffinPlus.Lib.Logging;

static partial class PrettyMemberEngine
{
	/// <summary>
	/// A thread-safe cache that associates <see cref="MethodInfo"/> instances with corresponding objects.
	/// </summary>
	/// <remarks>
	/// This cache is implemented using <see cref="ConditionalWeakTable{TKey,TValue}"/>, which ensures that the objects
	/// stored in the cache are eligible for garbage collection when their associated keys (<see cref="MethodInfo"/> instances)
	/// are no longer referenced elsewhere.
	/// </remarks>
	private static readonly ConditionalWeakTable<MethodInfo, object> sInitOnlyCache = new();

	/// <summary>
	/// Formats a property or indexer. Indexers are printed as <c>this[...]</c> and include accessors.
	/// </summary>
	/// <param name="propertyInfo">The property to format.</param>
	/// <param name="options">Member formatting options.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A single-line representation of the property or indexer.
	/// </returns>
	private static string FormatProperty(PropertyInfo propertyInfo, PrettyMemberOptions options, TextFormatContext tfc)
	{
		var builder = new StringBuilder();

		MethodInfo? getMethod = propertyInfo.GetMethod;
		MethodInfo? setMethod = propertyInfo.SetMethod;
		MethodInfo? accessor = getMethod ?? setMethod;

		PrettyTypeOptions typeOptions = options.UseNamespaceForTypes ? PrettyTypePresets.Full : PrettyTypePresets.Compact;
		if (options.ShowAttributes) builder.Append(FormatAttributes(propertyInfo, options, tfc));
		if (options.ShowAccessibility && accessor != null) builder.Append(GetAccessibility(accessor)).Append(' ');
		if (options.ShowMemberModifiers && accessor != null) AppendMemberModifiers(builder, accessor);
		if (options.IncludeDeclaringType && propertyInfo.DeclaringType != null)
		{
			PrettyTypeEngine.AppendType(builder, propertyInfo.DeclaringType, typeOptions, tfc);
			builder.Append('.');
		}

		bool isIndexer = propertyInfo.GetIndexParameters().Length > 0;
		if (isIndexer)
		{
			builder.Append("this[");
			ParameterInfo[] indexParameters = propertyInfo.GetIndexParameters();
			for (int i = 0; i < indexParameters.Length; i++)
			{
				if (i > 0) builder.Append(", ");
				builder.Append(Format(indexParameters[i], options, accessor, tfc));
			}
			builder.Append(']');
		}
		else
		{
			builder.Append(propertyInfo.Name);
		}

		string typeText = PrettyTypeEngine.Format(propertyInfo.PropertyType, typeOptions, tfc);

		builder.Append(" : ").Append(typeText);
		if (options.ShowNullabilityAnnotations &&
		    IsNullableReference(propertyInfo, propertyInfo.PropertyType) &&
		    !typeText.EndsWith("?", StringComparison.Ordinal))
		{
			builder.Append('?');
		}

		bool hasGet = getMethod != null;
		bool hasSet = setMethod != null;
		builder.Append(" {")
			.Append(hasGet ? " " + AccessorDecl("get", getMethod) : "")
			.Append(hasGet && hasSet ? " " : "")
			.Append(hasSet ? " " + AccessorDecl(IsInitOnly(propertyInfo) ? "init" : "set", setMethod) : "")
			.Append(" }");

		return NormalizeSpaces(builder.ToString());

		string AccessorDecl(string kind, MethodInfo? m)
		{
			if (m == null) return "";
			string accVis = GetAccessibility(m);
			string outerVis = accessor != null ? GetAccessibility(accessor) : accVis;
			return accVis == outerVis
				       ? $"{kind};"
				       : $"{accVis} {kind};";
		}
	}


#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
	/// <summary>
	/// Determines whether the specified property is <c>init</c>-only,
	/// that is, whether its setter is compiled with the
	/// <see cref="System.Runtime.CompilerServices.IsExternalInit"/> modifier.
	/// </summary>
	/// <param name="propertyInfo">
	/// The <see cref="PropertyInfo"/> representing the property to inspect.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the property defines an <c>init</c>-only setter;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	/// <remarks>
	///     <para>
	///     In C#, an <c>init</c>-only property is compiled as a regular <c>set</c> method whose return parameter
	///     carries a required modifier of type <see cref="System.Runtime.CompilerServices.IsExternalInit"/>.
	///     This helper examines that metadata to distinguish <c>init</c> from a standard <c>set</c> accessor.
	///     </para>
	///     <para>
	///     The method does not throw if <paramref name="propertyInfo"/> has no setter
	///     or if the modifier cannot be resolved; such cases simply return <see langword="false"/>.
	///     </para>
	/// </remarks>
	private static bool IsInitOnly(PropertyInfo propertyInfo)
	{
		MethodInfo? setMethodInfo = propertyInfo.SetMethod;
		if (setMethodInfo == null) return false;

		// Get the cached result (atomically)
		// The cache key is the MethodInfo of the setter
		object result = sInitOnlyCache.GetValue(
			setMethodInfo,
			static methodInfo =>
			{
				// Use GetCustomAttributesData() on the return parameter for performance.
				// This avoids allocating the attributes themselves.
				try
				{
					// ReSharper disable once PossibleNullReferenceException
					IList<CustomAttributeData> attributes = methodInfo.ReturnParameter.GetCustomAttributesData();

					// ReSharper disable once ForCanBeConvertedToForeach
					for (int i = 0; i < attributes.Count; i++)
					{
						Type attrType = attributes[i].AttributeType;
						if (string.Equals(attrType.Name, "IsExternalInit", StringComparison.Ordinal) &&
						    string.Equals(attrType.Namespace, "System.Runtime.CompilerServices", StringComparison.Ordinal))
						{
							return sTrueBox;
						}
					}
				}
				catch
				{
					// Defensive: In case GetCustomAttributesData fails
				}

				return sFalseBox;
			});

		// Return the unboxed bool
		return ReferenceEquals(result, sTrueBox);
	}
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
}
