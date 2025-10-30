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
		IncludeType = true,           // include exception type
		UseNamespaceForTypes = false, // use short type names
		IncludeHResult = false,       // omit HResult to reduce noise
		IncludeSource = false,        // omit source to reduce noise
		IncludeHelpLink = false,      // omit help link to reduce noise
		IncludeTargetSite = false,    // omit target site to reduce noise
		IncludeStackTrace = true,     // include stack trace
		StackFrameLimit = 20,         // limit stack trace length
		IncludeData = false,          // omit exception data
		FlattenAggregates = true,     // flatten AggregateExceptions
		MaxInnerExceptionDepth = 2,   // shallow inner exception depth
		ObjectOptionsForData = null   // not needed as IncludeData = false
	}.Freeze();

	/// <summary>
	/// Balanced preset suitable for most applications.
	/// </summary>
	public static readonly PrettyExceptionOptions Standard = new PrettyExceptionOptions
	{
		IncludeType = true,          // include exception type
		UseNamespaceForTypes = true, // use full type names
		IncludeHResult = false,      // omit HResult to reduce noise
		IncludeSource = true,        // include source for better diagnostics
		IncludeHelpLink = false,     // omit help link to reduce noise
		IncludeTargetSite = true,    // include target site
		IncludeStackTrace = true,    // include stack trace
		StackFrameLimit = 50,        // reasonable stack trace length
		FlattenAggregates = true,    // flatten AggregateExceptions
		MaxInnerExceptionDepth = 4,  // reasonable depth for inner exceptions
		IncludeData = true,          // include exception data
		ObjectOptionsForData = new PrettyObjectOptions
		{
			IncludeProperties = true,                           // include properties
			IncludeFields = false,                              // exclude fields
			IncludeNonPublic = false,                           // include public members only
			ShowTypeHeader = false,                             // show content only (no type header line)
			MaxDepth = 1,                                       // keep values shallow but readable
			MaxCollectionItems = 10,                            // show a reasonable number of items
			UseNamespaceForTypes = true,                        // align type rendering with exception options
			DictionaryFormat = DictionaryFormat.KeyEqualsValue, // dictionary style: Key = Value
			SortMembers = true,                                 // stable output ordering
			AllowMultiline = true                               // make use of multiple lines for complex data
		}
	}.Freeze();

	/// <summary>
	/// Verbose preset for deep diagnostics during debugging.
	/// </summary>
	public static readonly PrettyExceptionOptions Verbose = new PrettyExceptionOptions
	{
		IncludeType = true,                                 // include exception type
		UseNamespaceForTypes = true,                        // use full type names
		IncludeHResult = true,                              // include HResult for completeness
		IncludeSource = true,                               // include source for better diagnostics
		IncludeHelpLink = true,                             // include help link for completeness
		IncludeTargetSite = true,                           // include target site
		IncludeStackTrace = true,                           // include stack trace
		StackFrameLimit = PrettyExceptionOptions.Unlimited, // show full stack trace
		IncludeData = true,                                 // include exception data
		FlattenAggregates = true,                           // flatten AggregateExceptions
		MaxInnerExceptionDepth = 16,                        // deep inner exception depth
		ObjectOptionsForData = new PrettyObjectOptions
		{
			IncludeProperties = true,                           // include properties
			IncludeFields = true,                               // include fields
			IncludeNonPublic = true,                            // include non-public members
			ShowTypeHeader = false,                             // show content only (no type header line)
			MaxDepth = 2,                                       // a bit deeper for verbose diagnostics
			MaxCollectionItems = PrettyObjectOptions.Unlimited, // show all items
			UseNamespaceForTypes = true,                        // align type rendering with exception options
			DictionaryFormat = DictionaryFormat.KeyEqualsValue, // dictionary style: Key = Value
			SortMembers = true,                                 // stable output ordering
			AllowMultiline = true                               // make use of multiple lines for complex data
		}
	}.Freeze();
}
