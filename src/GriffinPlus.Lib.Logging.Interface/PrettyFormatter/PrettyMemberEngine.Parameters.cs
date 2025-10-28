using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

// ReSharper disable LoopCanBeConvertedToQuery

namespace GriffinPlus.Lib.Logging;

static partial class PrettyMemberEngine
{
	/// <summary>
	/// A thread-safe cache that associates <see cref="ParameterInfo"/> objects with their corresponding values.
	/// </summary>
	/// <remarks>
	/// This cache is used to store metadata or computed values related to <see cref="ParameterInfo"/>
	/// instances. The use of <see cref="ConditionalWeakTable{TKey,TValue}"/> ensures that the entries are automatically
	/// removed when the associated <see cref="ParameterInfo"/> objects are no longer referenced.
	/// </remarks>
	private static readonly ConditionalWeakTable<ParameterInfo, object> sInModifierCache = new();

	/// <summary>
	/// Appends a formatted parameter list to the specified <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="builder">The target <see cref="StringBuilder"/>.</param>
	/// <param name="parameters">The parameters to append.</param>
	/// <param name="options">Formatting options controlling nullability and layout.</param>
	/// <param name="owner">
	/// Optional method or constructor that owns the parameters.
	/// Used by <see cref="Format(ParameterInfo, PrettyMemberOptions, MethodBase?, TextFormatContext)"/> to resolve contextual attributes.
	/// </param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <remarks>
	/// This helper avoids <see cref="string.Join(string, IEnumerable{string})"/> and LINQ enumerations to minimize allocations.
	/// It preserves the exact output semantics of previous implementations while being slightly faster for methods with many parameters.
	/// </remarks>
	private static void AppendParameterList(
		StringBuilder       builder,
		ParameterInfo[]     parameters,
		PrettyMemberOptions options,
		MethodBase?         owner,
		TextFormatContext   tfc)
	{
		if (parameters.Length == 0)
		{
			builder.Append("()");
			return;
		}

		// Detect extension method to add `this` on the first parameter.
		bool isExtension = owner != null && owner.IsDefined(typeof(ExtensionAttribute), inherit: false);

		builder.Append('(');
		for (int i = 0; i < parameters.Length; i++)
		{
			if (i > 0) builder.Append(", ");
			if (isExtension && i == 0) builder.Append("this ");
			builder.Append(Format(parameters[i], options, owner, tfc));
		}
		builder.Append(')');
	}

	/// <summary>
	/// Derives the parameter modifier (e.g., <c>in</c>/<c>out</c>/<c>ref</c>/<c>params</c>) and the element type for by-ref parameters.<br/>
	/// Also indicates whether the parameter is a nullable reference (decision is handled by <see cref="IsNullableReference(ParameterInfo, Type)"/>).
	/// </summary>
	/// <param name="parameterInfo">The parameter being inspected.</param>
	/// <param name="modifier">Outputs the textual modifier (possibly empty).</param>
	/// <param name="coreType">Outputs the parameter's element type for by-ref parameters; otherwise the original type.</param>
	private static void GetParameterModifierAndType(
		ParameterInfo parameterInfo,
		out string    modifier,
		out Type      coreType)
	{
		modifier = "";
		coreType = parameterInfo.ParameterType;

		if (parameterInfo.GetCustomAttribute<ParamArrayAttribute>() != null) modifier = "params ";

		if (coreType.IsByRef)
		{
			coreType = coreType.GetElementType()!;
			if (parameterInfo.IsOut) modifier = "out ";
			else if (HasInModifier(parameterInfo)) modifier = "in ";
			else modifier = "ref ";
		}
	}

	/// <summary>
	/// Detects whether a by-ref parameter carries the C# <c>in</c> modifier (readonly-ref), based on the presence of <c>IsReadOnlyAttribute</c>.
	/// </summary>
	/// <param name="parameterInfo">The parameter to inspect.</param>
	/// <returns><see langword="true"/> if the parameter has the readonly-ref modifier; otherwise <see langword="false"/>.</returns>
	private static bool HasInModifier(ParameterInfo parameterInfo)
	{
		object result = sInModifierCache.GetValue(
			parameterInfo,
			static pi =>
			{
				// use CustomAttributeData for performance
				foreach (CustomAttributeData cad in pi.GetCustomAttributesData())
				{
					Type type = cad.AttributeType;
					if (string.Equals(type.Name, "IsReadOnlyAttribute", StringComparison.Ordinal) &&
					    string.Equals(type.Namespace, "System.Runtime.CompilerServices", StringComparison.Ordinal))
					{
						return sTrueBox;
					}
				}
				return sFalseBox;
			});
		return ReferenceEquals(result, sTrueBox);
	}
}
