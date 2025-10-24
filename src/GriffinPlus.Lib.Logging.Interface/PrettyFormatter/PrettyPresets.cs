///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Provides predefined <see cref="PrettyOptions"/> bundles for common formatting styles.
/// </summary>
/// <remarks>
/// These are immutable singletons used as shorthand for common <see cref="PrettyOptions"/> configurations.
/// You can clone them and modify individual fields for custom scenarios.
/// </remarks>
public static class PrettyPresets
{
	/// <summary>
	/// Compact preset: minimal metadata, short type names, aggressive truncation.
	/// Suitable for performance-sensitive logging.
	/// </summary>
	public static readonly PrettyOptions Compact = new PrettyOptions
	{
		Indent = " ",
		TruncateLongStrings = true,
		MaxLineLength = 120,

		TypeOptions = PrettyTypePresets.Compact,
		MemberOptions = PrettyMemberPresets.Compact,
		ObjectOptions = PrettyObjectPresets.Compact,
		ExceptionOptions = PrettyExceptionPresets.Compact,
		AssemblyOptions = PrettyAssemblyPresets.Minimal
	}.Freeze();

	/// <summary>
	/// Standard preset: balanced readability, detailed type information, limited stack traces.
	/// </summary>
	public static readonly PrettyOptions Standard = new PrettyOptions
	{
		Indent = "  ",
		TruncateLongStrings = true,
		MaxLineLength = 0,

		TypeOptions = PrettyTypePresets.Full,
		MemberOptions = PrettyMemberPresets.Standard,
		ObjectOptions = PrettyObjectPresets.Standard,
		ExceptionOptions = PrettyExceptionPresets.Standard,
		AssemblyOptions = PrettyAssemblyPresets.Minimal
	}.Freeze();

	/// <summary>
	/// Verbose preset: full metadata, namespaces, no truncation, full stack traces. Ideal for debugging.
	/// </summary>
	public static readonly PrettyOptions Verbose = new PrettyOptions
	{
		Indent = "  ",
		TruncateLongStrings = false,
		MaxLineLength = 0,

		TypeOptions = PrettyTypePresets.Full,
		MemberOptions = PrettyMemberPresets.Verbose,
		ObjectOptions = PrettyObjectPresets.Verbose,
		ExceptionOptions = PrettyExceptionPresets.Verbose,
		AssemblyOptions = PrettyAssemblyPresets.Verbose
	}.Freeze();
}
