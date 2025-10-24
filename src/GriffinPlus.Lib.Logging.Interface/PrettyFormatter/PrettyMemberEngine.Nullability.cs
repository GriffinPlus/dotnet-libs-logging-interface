using System;
using System.Reflection;

namespace GriffinPlus.Lib.Logging;

static partial class PrettyMemberEngine
{
	/// <summary>
	/// Returns <see langword="true"/> if the parameter is annotated as a nullable reference type according to the project-provided inspector.
	/// </summary>
	/// <param name="parameter">The parameter to inspect.</param>
	/// <param name="elementType">The parameter's element type (for by-ref) or the parameter type.</param>
	/// <returns>
	/// <see langword="true"/> if the nullable annotation is present; otherwise <see langword="false"/>.
	/// </returns>
	private static bool IsNullableReference(ParameterInfo? parameter, Type? elementType)
	{
		if (parameter == null || elementType is not { IsClass: true }) return false;
		return NullabilityInspector.GetNullability(parameter) == Nullability.Nullable;
	}

	/// <summary>
	/// Returns <see langword="true"/> if the property is annotated as a nullable reference type according to the project-provided inspector.
	/// </summary>
	/// <param name="property">The property to inspect.</param>
	/// <param name="propertyType">The property's type.</param>
	/// <returns>
	/// <see langword="true"/> if the nullable annotation is present; otherwise <see langword="false"/>.
	/// </returns>
	private static bool IsNullableReference(PropertyInfo? property, Type? propertyType)
	{
		if (property == null || propertyType is not { IsClass: true }) return false;
		return NullabilityInspector.GetNullability(property) == Nullability.Nullable;
	}

	/// <summary>
	/// Returns <see langword="true"/> if the field is annotated as a nullable reference type according to the project-provided inspector.
	/// </summary>
	/// <param name="field">The field to inspect.</param>
	/// <param name="fieldType">The field's type.</param>
	/// <returns>
	/// <see langword="true"/> if the nullable annotation is present;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsNullableReference(FieldInfo? field, Type? fieldType)
	{
		if (field == null || fieldType is not { IsClass: true }) return false;
		return NullabilityInspector.GetNullability(field) == Nullability.Nullable;
	}

	/// <summary>
	/// Returns <see langword="true"/> if the method's return value is annotated as a nullable reference type according to the project-provided inspector.
	/// </summary>
	/// <param name="method">The method to inspect.</param>
	/// <param name="returnType">The method's return type.</param>
	/// <returns>
	/// <see langword="true"/> if the nullable annotation is present;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsNullableReturn(MethodInfo? method, Type? returnType)
	{
		if (method == null || returnType is not { IsClass: true }) return false;
		return NullabilityInspector.GetReturnNullability(method) == Nullability.Nullable;
	}
}
