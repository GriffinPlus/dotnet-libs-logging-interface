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
		// Type header / Type rendering behavior
		ShowTypeHeader = true,                              // show type header for context
		UseNamespaceForTypes = false,                       // use short type names to reduce noise
		DictionaryFormat = DictionaryFormat.KeyEqualsValue, // compact dictionary style

		// Layout / Multiline & Flow Wrapping
		AllowMultiline = false,       // compact single-line representation
		FlowItemsInMultiline = false, // no effect as multiline is disabled
		MaxLineContentWidth = -1,     // no effect as multiline is disabled

		// Depth / Item limits
		MaxDepth = 1,           // shallow depth to keep output compact
		MaxCollectionItems = 3, // few items to reduce noise

		// Member Visibility and Ordering
		IncludeFields = false,    // omit fields to reduce noise
		IncludeProperties = true, // include properties for meaningful data
		IncludeNonPublic = false, // include public members only
		SortMembers = true        // stable output ordering
	}.Freeze();

	/// <summary>
	/// Balanced default preset suitable for most diagnostics.
	/// </summary>
	public static readonly PrettyObjectOptions Standard = new PrettyObjectOptions
	{
		// Type header / Type rendering behavior
		ShowTypeHeader = true,                              // show type header for context
		UseNamespaceForTypes = true,                        // use full type names for clarity
		DictionaryFormat = DictionaryFormat.KeyEqualsValue, // compact dictionary style

		// Layout / Multiline & Flow Wrapping
		AllowMultiline = true,       // allow multiline for better readability
		FlowItemsInMultiline = true, // flow items in multiline for better readability
		MaxLineContentWidth = 120,   // wrap lines exceeding 120 characters

		// Depth / Item limits
		MaxDepth = 2,           // reasonable depth for readability
		MaxCollectionItems = 5, // reasonable number of items for context

		// Member Visibility and Ordering
		IncludeFields = true,     // include fields for completeness
		IncludeProperties = true, // include properties for meaningful data
		IncludeNonPublic = false, // include public members only
		SortMembers = true        // stable output ordering
	}.Freeze();

	/// <summary>
	/// Verbose preset for deep inspection during debugging.<br/>
	/// Includes private and protected members, no depth or item limits.
	/// </summary>
	public static readonly PrettyObjectOptions Verbose = new PrettyObjectOptions
	{
		// Type header / Type rendering behavior
		ShowTypeHeader = true,                              // show type header for context
		UseNamespaceForTypes = true,                        // use full type names for clarity
		DictionaryFormat = DictionaryFormat.KeyEqualsValue, // compact dictionary style

		// Layout / Multiline & Flow Wrapping
		AllowMultiline = true,       // allow multiline for better readability
		FlowItemsInMultiline = true, // flow items in multiline for better readability
		MaxLineContentWidth = 120,   // wrap lines exceeding 120 characters

		// Depth / Item limits
		MaxDepth = 4,                                       // reasonable depth for readability
		MaxCollectionItems = PrettyObjectOptions.Unlimited, // show all items

		// Member Visibility and Ordering
		IncludeFields = true,     // include fields for completeness
		IncludeProperties = true, // include properties for meaningful data
		IncludeNonPublic = true,  // include public and non-public members
		SortMembers = true        // stable output ordering
	}.Freeze();
}
