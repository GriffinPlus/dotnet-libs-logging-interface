///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Commonly used <see cref="PrettyTypeOptions"/> presets that define typical formatting modes.
/// </summary>
/// <remarks>
/// These are immutable singletons used as shorthand for common <see cref="PrettyTypeOptions"/> configurations.
/// You can clone them and modify individual fields for custom scenarios.
/// </remarks>
public static class PrettyTypePresets
{
	/// <summary>
	/// The default preset that includes the full namespace in type names.
	/// </summary>
	public static readonly PrettyTypeOptions Full = new PrettyTypeOptions
	{
		UseNamespace = true
	}.Freeze();

	/// <summary>
	/// A preset that omits namespaces, showing only simple (unqualified) type names.
	/// </summary>
	public static readonly PrettyTypeOptions Compact = new PrettyTypeOptions
	{
		UseNamespace = false
	}.Freeze();
}
