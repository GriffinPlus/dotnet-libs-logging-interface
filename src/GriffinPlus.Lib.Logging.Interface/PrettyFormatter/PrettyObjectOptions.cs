///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Specifies how dictionary-like objects are rendered.
/// </summary>
public enum DictionaryFormat
{
	/// <summary>
	/// Render entries as a list of tuples, e.g. <c>[(key1, val1), (key2, val2), ...]</c>.
	/// </summary>
	Tuples = 0,

	/// <summary>
	/// Render entries in the format <c>[key1 = value1, key2 = value2, ...]</c>.
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
	#region Properties

	private int              mMaxDepth             = 2;
	private int              mMaxCollectionItems   = 5;
	private bool             mIncludeFields        = true;
	private bool             mIncludeProperties    = true;
	private bool             mIncludeNonPublic     = false;
	private bool             mSortMembers          = true;
	private bool             mShowTypeHeader       = true;
	private bool             mUseNamespaceForTypes = false;
	private DictionaryFormat mDictionaryFormat     = DictionaryFormat.KeyEqualsValue;

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

	#region ToString()

	/// <summary>
	/// Returns a concise textual representation for diagnostics.
	/// </summary>
	public override string ToString()
	{
		return $"Depth={MaxDepth}, Items={MaxCollectionItems}, Props={IncludeProperties}, " +
		       $"Fields={IncludeFields}, NonPublic={IncludeNonPublic}, " +
		       $"Sort={SortMembers}, Header={ShowTypeHeader}, UseNs={UseNamespaceForTypes} " +
		       $"IsFrozen={IsFrozen}";
	}

	#endregion
}
