///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Provides predefined <see cref="PrettyAssemblyOptions"/> configurations for common use cases.
/// </summary>
/// <remarks>
/// These are immutable singletons used as shorthand for common <see cref="PrettyAssemblyOptions"/> configurations.
/// You can clone them and modify individual fields for custom scenarios.
/// </remarks>
public static class PrettyAssemblyPresets
{
	/// <summary>
	/// A minimal preset showing only the assembly's identity (name, version, culture, token).
	/// </summary>
	public static readonly PrettyAssemblyOptions Minimal = new PrettyAssemblyOptions
	{
		IncludeHeader = true,
		IncludeImageRuntime = false,
		IncludeLocation = false,
		IncludeModules = false,
		IncludeReferences = false,
		IncludeExportedTypes = false,
		UseNamespaceForTypes = false
	}.Freeze();

	/// <summary>
	/// A detailed preset including modules and referenced assemblies but omitting exported types.<br/>
	/// Suitable for dependency diagnostics without overwhelming output.
	/// </summary>
	public static readonly PrettyAssemblyOptions Detailed = new PrettyAssemblyOptions
	{
		IncludeHeader = true,
		IncludeImageRuntime = true,
		IncludeLocation = true,
		IncludeModules = true,
		IncludeReferences = true,
		IncludeExportedTypes = false,
		UseNamespaceForTypes = true
	}.Freeze();

	/// <summary>
	/// A comprehensive preset including all available sections (modules, references, exported types)
	/// with no limit on the number of exported types.
	/// </summary>
	public static readonly PrettyAssemblyOptions Full = new PrettyAssemblyOptions
	{
		IncludeHeader = true,
		IncludeImageRuntime = true,
		IncludeLocation = true,
		IncludeModules = true,
		IncludeReferences = true,
		IncludeExportedTypes = true,
		ExportedTypesMax = 0,
		UseNamespaceForTypes = true
	}.Freeze();

	/// <summary>
	/// Verbose preset identical to <see cref="Full"/>, provided for semantic clarity.<br/>
	/// Enables all sections (header, runtime, location, modules, references, exported types),
	/// removes any element limits, and prints fully-qualified type names.
	/// </summary>
	public static readonly PrettyAssemblyOptions Verbose = new PrettyAssemblyOptions
	{
		IncludeHeader = true,
		IncludeImageRuntime = true,
		IncludeLocation = true,
		IncludeModules = true,
		IncludeReferences = true,
		IncludeExportedTypes = true,
		ExportedTypesMax = 0,
		UseNamespaceForTypes = true
	}.Freeze();
}
