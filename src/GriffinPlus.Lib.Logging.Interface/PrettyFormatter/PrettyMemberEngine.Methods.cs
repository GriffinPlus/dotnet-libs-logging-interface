using System;
using System.Reflection;
using System.Text;

namespace GriffinPlus.Lib.Logging;

static partial class PrettyMemberEngine
{
	/// <summary>
	/// Formats a method including accessibility, modifiers, generic arguments/constraints,
	/// parameter list and return type, according to <paramref name="options"/>.
	/// </summary>
	/// <param name="methodInfo">The method to format.</param>
	/// <param name="options">Member formatting options.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A single-line representation of the method signature.
	/// </returns>
	private static string FormatMethod(MethodInfo methodInfo, PrettyMemberOptions options, TextFormatContext tfc)
	{
		var builder = new StringBuilder();

		if (options.ShowAttributes) builder.Append(FormatAttributes(methodInfo, options, tfc));
		if (options.ShowAccessibility) builder.Append(GetAccessibility(methodInfo)).Append(' ');
		if (options.ShowMemberModifiers) AppendMemberModifiers(builder, methodInfo);
		if (options.ShowAsyncForAsyncMethods && ReturnsTaskLike(methodInfo)) builder.Append("async ");

		PrettyTypeOptions typeOptions = options.UseNamespaceForTypes ? PrettyTypePresets.Full : PrettyTypePresets.Compact;
		if (options.IncludeDeclaringType && methodInfo.DeclaringType != null)
		{
			PrettyTypeEngine.AppendType(builder, methodInfo.DeclaringType, typeOptions, tfc);
			builder.Append('.');
		}

		builder.Append(methodInfo.Name);

		if (methodInfo.IsGenericMethod)
		{
			Type[] genericArguments = methodInfo.GetGenericArguments();
			if (genericArguments.Length > 0)
			{
				builder.Append("<");
				for (int i = 0; i < genericArguments.Length; i++)
				{
					Type argument = genericArguments[i];
					if (i > 0) builder.Append(", ");
					if (argument.IsGenericParameter) builder.Append(argument.Name);
					else PrettyTypeEngine.AppendType(builder, argument, typeOptions, tfc);
				}
				builder.Append(">");
			}
		}

		AppendParameterList(builder, methodInfo.GetParameters(), options, methodInfo, tfc);

		PrettyTypeOptions returnTypeOptions = options.UseNamespaceForTypes ? PrettyTypePresets.Full : PrettyTypePresets.Compact;
		string returnText = PrettyTypeEngine.Format(methodInfo.ReturnType, returnTypeOptions, tfc);

		builder.Append(" : ").Append(returnText);
		if (options.ShowNullabilityAnnotations &&
		    IsNullableReturn(methodInfo, methodInfo.ReturnType) &&
		    !returnText.EndsWith("?", StringComparison.Ordinal))
		{
			builder.Append('?');
		}

		if (options.ShowGenericConstraintsOnMethods && methodInfo.IsGenericMethodDefinition)
		{
			string where = BuildWhereClauses(methodInfo.GetGenericArguments(), options, tfc);
			if (!string.IsNullOrEmpty(where)) builder.Append(" ").Append(where);
		}

		return NormalizeSpaces(builder.ToString());
	}

	/// <summary>
	/// Formats a constructor including accessibility/modifiers and its parameter list.
	/// </summary>
	/// <param name="constructorInfo">The constructor to format.</param>
	/// <param name="options">Member formatting options.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>A single-line representation of the constructor signature.</returns>
	private static string FormatConstructor(ConstructorInfo constructorInfo, PrettyMemberOptions options, TextFormatContext tfc)
	{
		var builder = new StringBuilder();

		if (options.ShowAttributes) builder.Append(FormatAttributes(constructorInfo, options, tfc));
		if (options.ShowAccessibility) builder.Append(GetAccessibility(constructorInfo)).Append(' ');
		if (options.ShowMemberModifiers) AppendMemberModifiers(builder, constructorInfo);

		PrettyTypeOptions typeOptions = options.UseNamespaceForTypes ? PrettyTypePresets.Full : PrettyTypePresets.Compact;
		Type? declaringType = constructorInfo.DeclaringType;

		if (options.IncludeDeclaringType && declaringType != null)
		{
			// Append the the full namespaced type, e.g., "MyNamespace.MyClass<T>".
			PrettyTypeEngine.AppendType(builder, declaringType, typeOptions, tfc);
			builder.Append('.');
		}

		// Append the constructor name, which is the simple name of the type, stripped of arity.
		if (declaringType != null)
		{
			string name = declaringType.Name; // z.B. "MyClass`1" oder "MySimpleClass"
			int tick = name.IndexOf('`');     // find the arity separator

			if (tick >= 0)
				builder.Append(name, 0, tick); // Appends "MyClass" only (avoids substring allocation)
			else
				builder.Append(name); // Appends "MySimpleClass" (no arity present)
		}
		else
		{
			builder.Append(constructorInfo.Name); // Fallback (should be ".ctor")
		}

		AppendParameterList(builder, constructorInfo.GetParameters(), options, constructorInfo, tfc);
		return NormalizeSpaces(builder.ToString());
	}
}
