///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Predefined <see cref="PrettyObjectOptions"/> configurations for typical logging scenarios.
/// </summary>
/// <remarks>
/// These are immutable singletons used as shorthand for common <see cref="PrettyObjectOptions"/> configurations.
/// You can clone them and modify individual fields for custom scenarios.
/// </remarks>
public static class PrettyObjectPresets
{
	/// <summary>
	/// Compact, log-friendly preset: shallow depth, few items, short strings.
	/// </summary>
	public static readonly PrettyObjectOptions Compact = new PrettyObjectOptions
	{
		MaxDepth = 1,
		MaxCollectionItems = 3,
		MaxStringLength = 120,
		IncludeFields = false,
		IncludeProperties = true,
		IncludeNonPublic = false,
		SortMembers = true,
		ShowTypeHeader = true,
		UseNamespaceForTypes = false
	}.Freeze();

	/// <summary>
	/// Balanced default preset suitable for most diagnostics.
	/// </summary>
	public static readonly PrettyObjectOptions Standard = new PrettyObjectOptions
	{
		MaxDepth = 2,
		MaxCollectionItems = 5,
		MaxStringLength = 200,
		IncludeFields = true,
		IncludeProperties = true,
		IncludeNonPublic = false,
		SortMembers = true,
		ShowTypeHeader = true,
		UseNamespaceForTypes = true
	}.Freeze();

	/// <summary>
	/// Verbose preset for deep inspection during debugging.<br/>
	/// Includes private and protected members, no depth or item limits.
	/// </summary>
	public static readonly PrettyObjectOptions Verbose = new PrettyObjectOptions
	{
		MaxDepth = 4,
		MaxCollectionItems = -1,
		MaxStringLength = -1,
		IncludeFields = true,
		IncludeProperties = true,
		IncludeNonPublic = true,
		SortMembers = true,
		ShowTypeHeader = true,
		UseNamespaceForTypes = true
	}.Freeze();
}
