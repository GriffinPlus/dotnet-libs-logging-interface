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
	/// <returns>
	/// A single-line representation of the method signature.
	/// </returns>
	private static string FormatMethod(MethodInfo methodInfo, PrettyMemberOptions options)
	{
		var builder = new StringBuilder();

		if (options.ShowAttributes) builder.Append(FormatAttributes(methodInfo, options));
		if (options.ShowAccessibility) builder.Append(GetAccessibility(methodInfo)).Append(' ');
		if (options.ShowMemberModifiers) builder.Append(GetMemberModifiers(methodInfo));
		if (options.ShowAsyncForAsyncMethods && ReturnsTaskLike(methodInfo)) builder.Append("async ");

		if (options.IncludeDeclaringType && methodInfo.DeclaringType != null)
			builder.Append(PrettyTypeEngine.Format(methodInfo.DeclaringType, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes })).Append('.');

		builder.Append(methodInfo.Name);

		if (methodInfo.IsGenericMethod)
		{
			Type[] genericArguments = methodInfo.GetGenericArguments();
			if (genericArguments.Length > 0)
			{
				var typeOptions = new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes };
				builder.Append("<");
				for (int i = 0; i < genericArguments.Length; i++)
				{
					Type argument = genericArguments[i];
					if (i > 0) builder.Append(", ");
					builder.Append(argument.IsGenericParameter ? argument.Name : PrettyTypeEngine.Format(argument, typeOptions));
				}
				builder.Append(">");
			}
		}

		AppendParameterList(builder, methodInfo.GetParameters(), options, methodInfo);

		var returnTypeOptions = new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes };
		string returnText = PrettyTypeEngine.Format(methodInfo.ReturnType, returnTypeOptions);

		if (options.ShowNullabilityAnnotations &&
		    IsNullableReturn(methodInfo, methodInfo.ReturnType) &&
		    !returnText.EndsWith("?", StringComparison.Ordinal))
		{
			returnText += "?";
		}

		builder.Append(" : ").Append(returnText);

		if (options.ShowGenericConstraintsOnMethods && methodInfo.IsGenericMethodDefinition)
		{
			string where = BuildWhereClauses(methodInfo.GetGenericArguments(), options);
			if (!string.IsNullOrEmpty(where)) builder.Append(" ").Append(where);
		}

		return NormalizeSpaces(builder.ToString());
	}

	/// <summary>
	/// Formats a constructor including accessibility/modifiers and its parameter list.
	/// </summary>
	/// <param name="constructorInfo">The constructor to format.</param>
	/// <param name="options">Member formatting options.</param>
	/// <returns>A single-line representation of the constructor signature.</returns>
	private static string FormatConstructor(ConstructorInfo constructorInfo, PrettyMemberOptions options)
	{
		var builder = new StringBuilder();

		if (options.ShowAttributes) builder.Append(FormatAttributes(constructorInfo, options));
		if (options.ShowAccessibility) builder.Append(GetAccessibility(constructorInfo)).Append(' ');
		if (options.ShowMemberModifiers) builder.Append(GetMemberModifiers(constructorInfo));

		if (options.IncludeDeclaringType && constructorInfo.DeclaringType != null)
			builder.Append(PrettyTypeEngine.Format(constructorInfo.DeclaringType, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes })).Append('.');

		builder.Append(constructorInfo.DeclaringType != null ? constructorInfo.DeclaringType.Name : constructorInfo.Name);
		AppendParameterList(builder, constructorInfo.GetParameters(), options, constructorInfo);
		return NormalizeSpaces(builder.ToString());
	}
}
