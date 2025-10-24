///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;

using Xunit;

namespace GriffinPlus.Lib.Logging.Tests.PrettyFormatter;

/// <summary>
/// Unit tests verifying the correctness and cross-target behavior of the <see cref="NullabilityInspector"/>.
/// </summary>
/// <remarks>
///     <para>
///     These tests ensure consistent behavior across both modern (.NET 6+) and legacy targets (&lt; .NET 6),
///     covering value types, reference types, parameters, return types, fields, properties, and events.
///     </para>
///     <para>
///         <b>Test strategy:</b>
///         <list type="bullet">
///             <item>
///                 <description>
///                 For assemblies compiled with <c>#nullable enable</c>, nullability information is explicitly encoded and should
///                 be reported as <see cref="Nullability.Nullable"/> or <see cref="Nullability.NonNullable"/>.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                 For assemblies compiled with <c>#nullable disable</c>, no metadata is emitted; therefore,
///                 the result should be <see cref="Nullability.Oblivious"/> (legacy decoding) or <see cref="Nullability.Unknown"/>
///                 depending on runtime capabilities.
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public sealed partial class NullableInspectorTests
{
	#region Helper Methods

	/// <summary>
	/// Asserts that a <see cref="Nullability"/> value equals <see cref="Nullability.NonNullable"/>.
	/// </summary>
	/// <param name="actual">The actual nullability value.</param>
	private static void ExpectNonNullable(Nullability actual) => Assert.Equal(Nullability.NonNullable, actual);

	/// <summary>
	/// Asserts that a <see cref="Nullability"/> value equals <see cref="Nullability.Nullable"/>.
	/// </summary>
	/// <param name="actual">The actual nullability value.</param>
	private static void ExpectNullable(Nullability actual) => Assert.Equal(Nullability.Nullable, actual);

	/// <summary>
	/// Asserts that the actual nullability matches the expected nullability, or allows the actual nullability to
	/// be <see cref="Nullability.Unknown"/> in non-.NET 6.0 or later environments.
	/// </summary>
	/// <remarks>
	/// In .NET 6.0 or later, the assertion strictly checks for equality between the expected and actual
	/// nullability values. In earlier versions, the assertion allows the actual nullability to be <see cref="Nullability.Unknown"/>
	/// as an alternative to matching the expected value.
	/// </remarks>
	/// <param name="expected">The expected nullability value.</param>
	/// <param name="actual">The actual nullability value to compare against the expected value.</param>
	private static void ExpectOrUnknown(Nullability expected, Nullability actual)
	{
#if NET6_0_OR_GREATER
		Assert.Equal(expected, actual);
#else
		Assert.True(actual == expected || actual == Nullability.Unknown);
#endif
	}

	/// <summary>
	/// Asserts that a <see cref="Nullability"/> value equals <see cref="Nullability.Oblivious"/> or <see cref="Nullability.Unknown"/>,
	/// depending on the platform and compiler.
	/// </summary>
	private static void ExpectObliviousOrUnknown(Nullability actual)
	{
#if NET6_0_OR_GREATER
		Assert.Equal(Nullability.Unknown, actual);
#else
		Assert.True(actual == Nullability.Oblivious || actual == Nullability.Unknown);
#endif
	}

	#endregion

	#region Value Types

	/// <summary>
	/// Verifies that <see cref="NullabilityInspector.IsNullableValueType"/> correctly identifies <c>T?</c>.
	/// </summary>
	[Fact]
	public void IsNullableValueType_Works()
	{
		Assert.True(NullabilityInspector.IsNullableValueType(typeof(int?)));
		Assert.False(NullabilityInspector.IsNullableValueType(typeof(int)));
	}

	/// <summary>
	/// Verifies <see cref="NullabilityInspector.IsNullableValueType(Type)"/> throws on null.
	/// </summary>
	[Fact]
	public void IsNullableValueType_Throws_On_Null()
	{
		var ex = Assert.Throws<ArgumentNullException>(() => NullabilityInspector.IsNullableValueType(null!));
		Assert.Equal("type", ex.ParamName);
	}

	#endregion

	#region Fields

	/// <summary>
	/// Ensures that the nullability of value-typed fields is correctly inferred.
	/// </summary>
	[Fact]
	public void GetNullability_Field_ValueType_Nullability()
	{
		Type type = typeof(EnabledTypes.ForFields);
		ExpectNullable(NullabilityInspector.GetNullability(type.GetField(nameof(EnabledTypes.ForFields.NullableValue))!));
		ExpectNonNullable(NullabilityInspector.GetNullability(type.GetField(nameof(EnabledTypes.ForFields.NonNullableValue))!));
	}

	/// <summary>
	/// Ensures that the nullability of reference-typed fields is correctly inferred.
	/// </summary>
	[Fact]
	public void GetNullability_Field_RefType_Nullability()
	{
		Type type = typeof(EnabledTypes.ForFields);
		ExpectNullable(NullabilityInspector.GetNullability(type.GetField(nameof(EnabledTypes.ForFields.NullableRef))!));
		ExpectNonNullable(NullabilityInspector.GetNullability(type.GetField(nameof(EnabledTypes.ForFields.NonNullableRef))!));
	}

	/// <summary>
	/// Verifies <see cref="NullabilityInspector.GetNullability(FieldInfo)"/> throws on null.
	/// </summary>
	[Fact]
	public void GetNullability_Field_Throws_On_Null()
	{
		var ex = Assert.Throws<ArgumentNullException>(() => NullabilityInspector.GetNullability((FieldInfo)null!));
		Assert.Equal("field", ex.ParamName);
	}

	/// <summary>
	/// Verifies that a field compiled under <c>#nullable disable</c> is reported as oblivious or unknown.
	/// </summary>
	[Fact]
	public void GetNullability_Field_Oblivious_From_Disabled_Context()
	{
		Type type = typeof(DisabledTypes.ForFields);
		FieldInfo fieldInfo = type.GetField(nameof(DisabledTypes.ForFields.RefField))!;
		ExpectObliviousOrUnknown(NullabilityInspector.GetNullability(fieldInfo));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Ensures that the nullability of value-typed properties is correctly inferred.
	/// </summary>
	[Fact]
	public void GetNullability_Property_ValueType_Nullability()
	{
		Type type = typeof(EnabledTypes.ForProperties);
		ExpectNullable(NullabilityInspector.GetNullability(type.GetProperty(nameof(EnabledTypes.ForProperties.NullableValue))!));
		ExpectNonNullable(NullabilityInspector.GetNullability(type.GetProperty(nameof(EnabledTypes.ForProperties.NonNullableValue))!));
	}

	/// <summary>
	/// Ensures that the nullability of reference-typed properties is correctly inferred.
	/// </summary>
	[Fact]
	public void GetNullability_Property_RefType_Nullability()
	{
		Type type = typeof(EnabledTypes.ForProperties);
		PropertyInfo nullableProperty = type.GetProperty(nameof(EnabledTypes.ForProperties.NullableRef))!;
		PropertyInfo nonNullableProperty = type.GetProperty(nameof(EnabledTypes.ForProperties.NonNullableRef))!;

		ExpectOrUnknown(Nullability.Nullable, NullabilityInspector.GetNullability(nullableProperty));
		ExpectOrUnknown(Nullability.NonNullable, NullabilityInspector.GetNullability(nonNullableProperty));
	}

	/// <summary>
	/// Verifies <see cref="NullabilityInspector.GetNullability(PropertyInfo)"/> throws on null.
	/// </summary>
	[Fact]
	public void GetNullability_Property_Throws_On_Null()
	{
		var ex = Assert.Throws<ArgumentNullException>(() => NullabilityInspector.GetNullability((PropertyInfo)null!));
		Assert.Equal("property", ex.ParamName);
	}

	/// <summary>
	/// Verifies that a property compiled under <c>#nullable disable</c> is reported as oblivious or unknown.
	/// </summary>
	[Fact]
	public void GetNullability_Property_Oblivious_From_Disabled_Context()
	{
		Type type = typeof(DisabledTypes.ForProperties);
		PropertyInfo propertyInfo = type.GetProperty(nameof(DisabledTypes.ForProperties.RefProp))!;
		ExpectObliviousOrUnknown(NullabilityInspector.GetNullability(propertyInfo));
	}

	#endregion

	#region Methods

	/// <summary>
	/// Verifies nullability inference for method return types.
	/// </summary>
	[Fact]
	public void GetReturnNullability_Method_ReturnType_Nullability()
	{
		Type type = typeof(EnabledTypes.ForMethods);
		MethodInfo methodWithNonNullableReturn = type.GetMethod(nameof(EnabledTypes.ForMethods.ReturnNonNullable))!;
		MethodInfo methodWithNullableReturn = type.GetMethod(nameof(EnabledTypes.ForMethods.ReturnNullable))!;

		ExpectOrUnknown(Nullability.NonNullable, NullabilityInspector.GetReturnNullability(methodWithNonNullableReturn));
		ExpectOrUnknown(Nullability.Nullable, NullabilityInspector.GetReturnNullability(methodWithNullableReturn));
	}

	/// <summary>
	/// Verifies <see cref="NullabilityInspector.GetReturnNullability(MethodInfo)"/> throws on null.
	/// </summary>
	[Fact]
	public void GetReturnNullability_Throws_On_Null()
	{
		var ex = Assert.Throws<ArgumentNullException>(() => NullabilityInspector.GetReturnNullability(null!));
		Assert.Equal("method", ex.ParamName);
	}

	/// <summary>
	/// Verifies that method return values compiled under <c>#nullable disable</c> are reported as oblivious or unknown.
	/// </summary>
	[Fact]
	public void GetReturnNullability_Oblivious_From_Disabled_Context()
	{
		Type type = typeof(DisabledTypes.ForMethods);
		MethodInfo methodWithReferenceReturn = type.GetMethod(nameof(DisabledTypes.ForMethods.ReturnRef))!;
		ExpectObliviousOrUnknown(NullabilityInspector.GetReturnNullability(methodWithReferenceReturn));
	}

	/// <summary>
	/// Verifies nullability inference for method parameters.
	/// </summary>
	[Fact]
	public void GetNullability_Method_Parameter_Nullability()
	{
		Type type = typeof(EnabledTypes.ForMethods);
		ParameterInfo nonNullableParameter = type.GetMethod(nameof(EnabledTypes.ForMethods.ParamNonNullable))!.GetParameters()[0];
		ParameterInfo nullableParameter = type.GetMethod(nameof(EnabledTypes.ForMethods.ParamNullable))!.GetParameters()[0];

		ExpectOrUnknown(Nullability.NonNullable, NullabilityInspector.GetNullability(nonNullableParameter));
		ExpectOrUnknown(Nullability.Nullable, NullabilityInspector.GetNullability(nullableParameter));
	}

	/// <summary>
	/// Verifies <see cref="NullabilityInspector.GetNullability(ParameterInfo)"/> throws on null.
	/// </summary>
	[Fact]
	public void GetNullability_Method_Parameter_Throws_On_Null()
	{
		var ex = Assert.Throws<ArgumentNullException>(() => NullabilityInspector.GetNullability((ParameterInfo)null!));
		Assert.Equal("parameter", ex.ParamName);
	}

	/// <summary>
	/// Verifies that method parameters compiled under <c>#nullable disable</c> are reported as oblivious or unknown.
	/// </summary>
	[Fact]
	public void GetNullability_Method_Parameter_Oblivious_From_Disabled_Context()
	{
		Type type = typeof(DisabledTypes.ForMethods);
		MethodInfo methodWithReferenceParameter = type.GetMethod(nameof(DisabledTypes.ForMethods.ParamRef))!;
		ExpectObliviousOrUnknown(NullabilityInspector.GetNullability(methodWithReferenceParameter.GetParameters()[0]));
	}

	#endregion

	#region Events

	/// <summary>
	/// Verifies that event metadata is inspected correctly and does not cause runtime errors.
	/// </summary>
	/// <remarks>
	///     <para>
	///     On .NET 6+, <see cref="NullabilityInspector"/> uses <c>System.Reflection.NullabilityInfoContext</c> to read event metadata,
	///     returning <see cref="Nullability.NonNullable"/> for standard event patterns.
	///     On legacy runtimes, metadata may be unavailable; the result can be <see cref="Nullability.Unknown"/>.
	///     </para>
	/// </remarks>
	[Fact]
	public void GetNullability_Event_Nullability()
	{
		Type type = typeof(EnabledTypes.ForEvents);
		EventInfo eventInfo = type.GetEvent(nameof(EnabledTypes.ForEvents.NullableEvent))!;

#if NET6_0_OR_GREATER
		// Event is declared as `EventHandler?` => Nullable
		Assert.Equal(Nullability.Nullable, NullabilityInspector.GetNullability(eventInfo));
#else
		// Legacy: attribute decoding may or may not carry event-level metadata
		Nullability result = NullabilityInspector.GetNullability(eventInfo);
		Assert.True(result is Nullability.Nullable or Nullability.Unknown);
#endif
	}

	/// <summary>
	/// Verifies that the nullability of an explicitly declared non-nullable event is reported correctly.
	/// </summary>
	/// <remarks>
	/// This test inspects the nullability metadata of the <see cref="EnabledTypes.ForEvents.NonNullableEvent"/>
	/// event to ensure it is interpreted as <see cref="Nullability.NonNullable"/> when using .NET 6.0 or greater.
	/// For earlier versions, the test allows for either <see cref="Nullability.NonNullable"/> or
	/// <see cref="Nullability.Unknown"/> due to potential inconsistencies in legacy attribute decoding.
	/// </remarks>
	[Fact]
	public void GetNullability_Event_NonNullable_Explicit_Event_Is_Reported_Correctly()
	{
		Type type = typeof(EnabledTypes.ForEvents);
		EventInfo eventInfo = type.GetEvent(nameof(EnabledTypes.ForEvents.NonNullableEvent))!;

#if NET6_0_OR_GREATER
		// Event declared as non-nullable EventHandler (no '?') -> expect NonNullable
		Assert.Equal(Nullability.NonNullable, NullabilityInspector.GetNullability(eventInfo));
#else
		// Legacy attribute decoding might not carry event-level metadata consistently.
		// Allow NonNullable (preferred) or Unknown depending on compiler/toolchain.
		Nullability result = NullabilityInspector.GetNullability(eventInfo);
		Assert.True(result is Nullability.NonNullable or Nullability.Unknown);
#endif
	}

	/// <summary>
	/// Verifies <see cref="NullabilityInspector.GetNullability(EventInfo)"/> throws on null.
	/// </summary>
	[Fact]
	public void GetNullability_Event_Throws_On_Null()
	{
		var ex = Assert.Throws<ArgumentNullException>(() => NullabilityInspector.GetNullability((EventInfo)null!));
		Assert.Equal("eventInfo", ex.ParamName);
	}

	#endregion
}
