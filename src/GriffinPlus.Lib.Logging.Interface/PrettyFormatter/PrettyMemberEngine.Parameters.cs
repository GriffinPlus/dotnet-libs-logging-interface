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
	/// Appends a formatted parameter list to the specified <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="builder">The target <see cref="StringBuilder"/>.</param>
	/// <param name="parameters">The parameters to append.</param>
	/// <param name="options">Formatting options controlling nullability and layout.</param>
	/// <param name="owner">
	/// Optional method or constructor that owns the parameters.
	/// Used by <see cref="Format(ParameterInfo, PrettyMemberOptions, MethodBase?)"/> to resolve contextual attributes.
	/// </param>
	/// <remarks>
	/// This helper avoids <see cref="string.Join(string, IEnumerable{string})"/> and LINQ enumerations to minimize allocations.
	/// It preserves the exact output semantics of previous implementations while being slightly faster for methods with many parameters.
	/// </remarks>
	private static void AppendParameterList(
		StringBuilder       builder,
		ParameterInfo[]     parameters,
		PrettyMemberOptions options,
		MethodBase?         owner = null)
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
			builder.Append(Format(parameters[i], options, owner));
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
		foreach (object attribute in parameterInfo.GetCustomAttributes(true))
		{
			Type type = attribute.GetType();
			if (type.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute")
				return true;
		}
		return false;
	}
}
