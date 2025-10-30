///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Text;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Specifies how dictionary-like objects are rendered.
/// </summary>
public enum DictionaryFormat
{
	/// <summary>
	/// Render entries as a list of tuples, e.g. <c>{(key1, val1), (key2, val2), ...}</c>.
	/// </summary>
	Tuples = 0,

	/// <summary>
	/// Render entries in the format <c>{key1 = value1, key2 = value2, ...}</c>.
	/// </summary>
	KeyEqualsValue = 1
}

/// <summary>
/// Options that control how arbitrary objects are formatted for logging/diagnostics by the <see cref="PrettyObjectEngine"/>.
/// </summary>
/// <remarks>
/// The options are mutable for convenience.
/// Use <see cref="PrettyOptionsBase{PrettyObjectOptions}.Clone"/> to create a safe copy when based on presets.
/// </remarks>
public sealed class PrettyObjectOptions : PrettyOptionsBase<PrettyObjectOptions>
{
	#region Type header / Type rendering behavior

	private bool             mShowTypeHeader       = true;
	private bool             mUseNamespaceForTypes = false;
	private DictionaryFormat mDictionaryFormat     = DictionaryFormat.KeyEqualsValue;

	/// <summary>
	/// Gets or sets whether a type header (e.g., <c>My.Namespace.Customer</c>) precedes object content.<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool ShowTypeHeader
	{
		get => mShowTypeHeader;
		set
		{
			EnsureMutable();
			mShowTypeHeader = value;
		}
	}

	/// <summary>
	/// Gets or sets whether types should be printed with namespaces where applicable.<br/>
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool UseNamespaceForTypes
	{
		get => mUseNamespaceForTypes;
		set
		{
			EnsureMutable();
			mUseNamespaceForTypes = value;
		}
	}

	/// <summary>
	/// Gets or sets the format that controls how <see cref="System.Collections.IDictionary"/> and
	/// <see cref="IReadOnlyDictionary{TKey,TValue}"/> are rendered.<br/>
	/// Default is <see cref="Logging.DictionaryFormat.KeyEqualsValue"/>.
	/// </summary>
	public DictionaryFormat DictionaryFormat
	{
		get => mDictionaryFormat;
		set
		{
			EnsureMutable();
			mDictionaryFormat = value;
		}
	}

	#endregion

	#region Layout / Multiline & Flow Wrapping

	private bool mAllowMultiline       = true;
	private bool mFlowItemsInMultiline = true;
	private int  mMaxLineContentWidth  = 120;

	/// <summary>
	/// Gets or sets a value indicating whether the formatter may insert line breaks and indentation
	/// for improved readability (e.g., multi-line dictionaries/collections/objects).
	/// </summary>
	public bool AllowMultiline
	{
		get => mAllowMultiline;
		set
		{
			EnsureMutable();
			mAllowMultiline = value;
		}
	}

	/// <summary>
	/// Gets or sets whether multi-line rendering should place as many items as possible on each line up to
	/// <see cref="MaxLineContentWidth"/> instead of putting exactly one item per line.<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool FlowItemsInMultiline
	{
		get => mFlowItemsInMultiline;
		set
		{
			EnsureMutable();
			mFlowItemsInMultiline = value;
		}
	}

	/// <summary>
	/// Gets or sets the maximum content width per line (in characters) used when rendering collections and
	/// dictionaries either inline or in multi-line flow. The width applies to the text inside the braces/brackets
	/// only (i.e., between "{ … }" or "[ … ]").<br/>
	/// A value &lt;= 0 disables the width check.<br/>
	/// Default is 120.
	/// </summary>
	public int MaxLineContentWidth
	{
		get => mMaxLineContentWidth;
		set
		{
			EnsureMutable();
			mMaxLineContentWidth = value;
		}
	}

	#endregion

	#region Depth / Item limits

	private int mMaxDepth           = 2;
	private int mMaxCollectionItems = 5;

	/// <summary>
	/// Gets or sets the maximum recursion depth for nested objects/collections.<br/>
	/// A value of <c>0</c> prints only the root object header.<br/>
	/// Default is <c>2</c>.
	/// </summary>
	public int MaxDepth
	{
		get => mMaxDepth;
		set
		{
			EnsureMutable();
			mMaxDepth = value;
		}
	}

	/// <summary>
	/// Gets or sets the maximum number of items written for a collection or dictionary at a given level.<br/>
	/// A value of <c>0</c> prints no items; a negative value removes the limit.<br/>
	/// Default is <c>5</c>.
	/// </summary>
	public int MaxCollectionItems
	{
		get => mMaxCollectionItems;
		set
		{
			EnsureMutable();
			mMaxCollectionItems = value;
		}
	}

	#endregion

	#region Member Visibility and Ordering

	private bool mIncludeFields     = true;
	private bool mIncludeProperties = true;
	private bool mIncludeNonPublic  = false;
	private bool mSortMembers       = true;

	/// <summary>
	/// Gets or sets whether to include fields when rendering objects.<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool IncludeFields
	{
		get => mIncludeFields;
		set
		{
			EnsureMutable();
			mIncludeFields = value;
		}
	}

	/// <summary>
	/// Gets or sets whether to include properties when rendering objects.<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool IncludeProperties
	{
		get => mIncludeProperties;
		set
		{
			EnsureMutable();
			mIncludeProperties = value;
		}
	}

	/// <summary>
	/// Gets or sets whether non-public members (private/internal) are included.<br/>
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool IncludeNonPublic
	{
		get => mIncludeNonPublic;
		set
		{
			EnsureMutable();
			mIncludeNonPublic = value;
		}
	}

	/// <summary>
	/// Gets or sets whether member names are sorted alphabetically (properties, then fields).<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool SortMembers
	{
		get => mSortMembers;
		set
		{
			EnsureMutable();
			mSortMembers = value;
		}
	}

	#endregion

	#region ToString()

	/// <summary>
	/// Returns a concise representation of the object-formatting options for debugging/logging.
	/// </summary>
	public override string ToString()
	{
		// Use fully qualified name to avoid adding a using directive.
		var builder = new StringBuilder(192);

		// Type header / Type Rendering Behavior
		builder.Append($"{nameof(ShowTypeHeader)}={ShowTypeHeader}, ");
		builder.Append($"{nameof(UseNamespaceForTypes)}={UseNamespaceForTypes}, ");
		builder.Append($"{nameof(DictionaryFormat)}={DictionaryFormat}, ");

		// Layout / Multiline & Flow Wrapping
		builder.Append($"{nameof(AllowMultiline)}={AllowMultiline}, ");
		builder.Append($"{nameof(FlowItemsInMultiline)}={FlowItemsInMultiline}, ");
		builder.Append($"{nameof(MaxLineContentWidth)}={MaxLineContentWidth}, ");

		// Depth / Item limits
		builder.Append($"{nameof(MaxDepth)}={MaxDepth}, ");
		builder.Append($"{nameof(MaxCollectionItems)}={MaxCollectionItems}, ");

		// Member Visibility and Ordering
		builder.Append($"{nameof(IncludeProperties)}={IncludeProperties}, ");
		builder.Append($"{nameof(IncludeFields)}={IncludeFields}, ");
		builder.Append($"{nameof(IncludeNonPublic)}={IncludeNonPublic}, ");
		builder.Append($"{nameof(SortMembers)}={SortMembers}, ");

		// Frozen state (important for diagnosing accidental mutations)
		builder.Append($"{nameof(IsFrozen)}={IsFrozen}");

		return builder.ToString();
	}

	#endregion
}
