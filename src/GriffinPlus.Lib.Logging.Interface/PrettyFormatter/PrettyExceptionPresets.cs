///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Predefined <see cref="PrettyExceptionOptions"/> configurations for common logging scenarios.
/// </summary>
/// <remarks>
/// These are immutable singletons used as shorthand for common <see cref="PrettyExceptionOptions"/> configurations.
/// You can clone them and modify individual fields for custom scenarios.
/// </remarks>
public static class PrettyExceptionPresets
{
	/// <summary>
	/// Compact preset for high-volume logs: short type names, stack limited, data omitted.
	/// </summary>
	public static readonly PrettyExceptionOptions Compact = new PrettyExceptionOptions
	{
		IncludeType = true,
		UseNamespaceForTypes = false,
		IncludeHResult = false,
		IncludeSource = false,
		IncludeHelpLink = false,
		IncludeTargetSite = false,
		IncludeStackTrace = true,
		StackFrameLimit = 20,
		IncludeData = false,
		DataMaxItems = 0,
		FlattenAggregates = true,
		MaxInnerExceptionDepth = 2
	}.Freeze();

	/// <summary>
	/// Balanced preset suitable for most applications.
	/// </summary>
	public static readonly PrettyExceptionOptions Standard = new PrettyExceptionOptions
	{
		IncludeType = true,
		UseNamespaceForTypes = true,
		IncludeHResult = false,
		IncludeSource = true,
		IncludeHelpLink = false,
		IncludeTargetSite = true,
		IncludeStackTrace = true,
		StackFrameLimit = 50,
		IncludeData = true,
		DataMaxItems = 10,
		FlattenAggregates = true,
		MaxInnerExceptionDepth = 4
	}.Freeze();

	/// <summary>
	/// Verbose preset for deep diagnostics during debugging.
	/// </summary>
	public static readonly PrettyExceptionOptions Verbose = new PrettyExceptionOptions
	{
		IncludeType = true,
		UseNamespaceForTypes = true,
		IncludeHResult = true,
		IncludeSource = true,
		IncludeHelpLink = true,
		IncludeTargetSite = true,
		IncludeStackTrace = true,
		StackFrameLimit = 0, // unlimited
		IncludeData = true,
		DataMaxItems = 0, // unlimited
		FlattenAggregates = true,
		MaxInnerExceptionDepth = 16
	}.Freeze();
}
