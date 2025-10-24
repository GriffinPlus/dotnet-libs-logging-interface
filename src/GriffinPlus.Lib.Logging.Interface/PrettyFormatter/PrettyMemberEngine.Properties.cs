using System;
using System.Reflection;
using System.Text;

// ReSharper disable ForCanBeConvertedToForeach

namespace GriffinPlus.Lib.Logging;

static partial class PrettyMemberEngine
{
	/// <summary>
	/// Formats a property or indexer. Indexers are printed as <c>this[...]</c> and include accessors.
	/// </summary>
	/// <param name="propertyInfo">The property to format.</param>
	/// <param name="options">Member formatting options.</param>
	/// <returns>
	/// A single-line representation of the property or indexer.
	/// </returns>
	private static string FormatProperty(PropertyInfo propertyInfo, PrettyMemberOptions options)
	{
		var builder = new StringBuilder();

		MethodInfo? getMethod = propertyInfo.GetMethod;
		MethodInfo? setMethod = propertyInfo.SetMethod;
		MethodInfo? accessor = getMethod ?? setMethod;

		if (options.ShowAttributes) builder.Append(FormatAttributes(propertyInfo, options));
		if (options.ShowAccessibility && accessor != null) builder.Append(GetAccessibility(accessor)).Append(' ');
		if (options.ShowMemberModifiers && accessor != null) builder.Append(GetMemberModifiers(accessor));
		if (options.IncludeDeclaringType && propertyInfo.DeclaringType != null)
			builder.Append(PrettyTypeEngine.Format(propertyInfo.DeclaringType, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes })).Append('.');

		bool isIndexer = propertyInfo.GetIndexParameters().Length > 0;
		if (isIndexer)
		{
			builder.Append("this[");
			ParameterInfo[] indexParameters = propertyInfo.GetIndexParameters();
			for (int i = 0; i < indexParameters.Length; i++)
			{
				if (i > 0) builder.Append(", ");
				builder.Append(Format(indexParameters[i], options, accessor));
			}
			builder.Append(']');
		}
		else
		{
			builder.Append(propertyInfo.Name);
		}

		string typeText = PrettyTypeEngine.Format(propertyInfo.PropertyType, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes });

		if (options.ShowNullabilityAnnotations &&
		    IsNullableReference(propertyInfo, propertyInfo.PropertyType) &&
		    !typeText.EndsWith("?", StringComparison.Ordinal))
		{
			typeText += "?";
		}

		builder.Append(" : ").Append(typeText);

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
			bool isInit = kind == "set" && IsInitOnly(propertyInfo);
			string accVis = GetAccessibility(m);
			string outerVis = accessor != null ? GetAccessibility(accessor) : accVis;
			string kw = isInit ? "init" : "set";
			return accVis == outerVis
				       ? $"{(kind == "get" ? "get" : kw)};"
				       : $"{accVis} {(kind == "get" ? "get" : kw)};";
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
		MethodInfo? set = propertyInfo.SetMethod;
		if (set == null) return false;
		// C# 'init' is encoded as a required modifier on the return parameter.
		// ReSharper disable once PossibleNullReferenceException
		Type[] mods = set.ReturnParameter.GetRequiredCustomModifiers();
		for (int i = 0; i < mods.Length; i++)
		{
			if (mods[i].FullName == "System.Runtime.CompilerServices.IsExternalInit")
				return true;
		}
		return false;
	}
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
}
