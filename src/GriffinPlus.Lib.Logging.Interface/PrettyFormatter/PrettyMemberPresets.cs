///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Commonly used <see cref="PrettyMemberOptions"/> presets.
/// </summary>
/// <remarks>
/// These are immutable singletons used as shorthand for common <see cref="PrettyMemberOptions"/> configurations.
/// You can clone them and modify individual fields for custom scenarios.
/// </remarks>
public static class PrettyMemberPresets
{
	/// <summary>
	/// Verbose, documentation-friendly preset that includes declaring type, accessibility, modifiers,
	/// parameter names, nullability annotations (when available), and generic constraints.
	/// Uses short type names in signatures for readability.
	/// </summary>
	public static readonly PrettyMemberOptions DocFriendly = new PrettyMemberOptions
	{
		IncludeDeclaringType = true,
		ShowAccessibility = true,
		ShowMemberModifiers = true,
		ShowAsyncForAsyncMethods = true,
		ShowParameterNames = true,
		ShowNullabilityAnnotations = true,
		ShowAttributes = false,
		ShowParameterAttributes = false,
		AttributeFilter = null,
		AttributeMaxElements = 3,
		ShowGenericConstraintsOnMethods = true,
		ShowGenericConstraintsOnTypes = true,
		UseNamespaceForTypes = false
	}.Freeze();

	/// <summary>
	/// Balanced default preset suitable for most logs and diagnostics.
	/// </summary>
	public static readonly PrettyMemberOptions Standard = new PrettyMemberOptions
	{
		IncludeDeclaringType = true,
		ShowAccessibility = true,
		ShowMemberModifiers = true,
		ShowAsyncForAsyncMethods = true,
		ShowParameterNames = true,
		ShowNullabilityAnnotations = true,
		ShowAttributes = false,
		ShowParameterAttributes = false,
		AttributeFilter = null,
		AttributeMaxElements = 3,
		ShowGenericConstraintsOnMethods = true,
		ShowGenericConstraintsOnTypes = true,
		UseNamespaceForTypes = true
	}.Freeze();

	/// <summary>
	/// Compact, signature-focused preset: omits accessibility/modifiers/attributes and constraints,
	/// keeps parameter names, and uses short type names.
	/// </summary>
	public static readonly PrettyMemberOptions Compact = new PrettyMemberOptions
	{
		IncludeDeclaringType = true,
		ShowAccessibility = false,
		ShowMemberModifiers = false,
		ShowAsyncForAsyncMethods = false,
		ShowParameterNames = true,
		ShowNullabilityAnnotations = false,
		ShowAttributes = false,
		ShowParameterAttributes = false,
		AttributeFilter = null,
		AttributeMaxElements = 0,
		ShowGenericConstraintsOnMethods = false,
		ShowGenericConstraintsOnTypes = false,
		UseNamespaceForTypes = false
	}.Freeze();

	/// <summary>
	/// Verbose preset: include declaring type, accessibility, modifiers, async hint,
	/// parameter names, nullability annotations, attributes (with arguments),
	/// and show generic constraints for methods *and* types; use fully-qualified type names.
	/// </summary>
	public static readonly PrettyMemberOptions Verbose = new PrettyMemberOptions
	{
		IncludeDeclaringType = true,
		ShowAccessibility = true,
		ShowMemberModifiers = true,
		ShowAsyncForAsyncMethods = true,
		ShowParameterNames = true,
		ShowNullabilityAnnotations = true,
		ShowAttributes = true,
		ShowParameterAttributes = true,
		AttributeFilter = null,
		AttributeMaxElements = 8, // print up to 8 constructor/named args per attribute
		ShowGenericConstraintsOnMethods = true,
		ShowGenericConstraintsOnTypes = true,
		UseNamespaceForTypes = true
	}.Freeze();
}
