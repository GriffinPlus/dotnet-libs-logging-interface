///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Options that control how arbitrary objects are formatted for logging/diagnostics by the <see cref="PrettyObjectEngine"/>.
/// </summary>
/// <remarks>
/// The options are mutable for convenience. Use <see cref="Clone"/> to create a safe copy when based on presets.
/// </remarks>
public sealed class PrettyObjectOptions
{
	#region Properties

	private int  mMaxDepth             = 2;
	private int  mMaxCollectionItems   = 5;
	private int  mMaxStringLength      = 200;
	private bool mIncludeFields        = true;
	private bool mIncludeProperties    = true;
	private bool mIncludeNonPublic     = false;
	private bool mSortMembers          = true;
	private bool mShowTypeHeader       = true;
	private bool mUseNamespaceForTypes = false;

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
	/// Gets or sets the maximum number of characters for string values.<br/>
	/// Longer strings are truncated with an ellipsis.<br/>
	/// A negative value removes the limit. Default is <c>200</c>.
	/// </summary>
	public int MaxStringLength
	{
		get => mMaxStringLength;
		set
		{
			EnsureMutable();
			mMaxStringLength = value;
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

	#endregion

	#region Freeze Support

	/// <summary>
	/// Gets or sets a value indicating whether this options instance is frozen (read-only).
	/// </summary>
	public bool IsFrozen { get; private set; }

	/// <summary>
	/// Makes this options instance read-only. Subsequent attempts to mutate it will throw.
	/// </summary>
	/// <returns>
	/// The frozen <see cref="PrettyObjectOptions"/> instance.
	/// </returns>
	public PrettyObjectOptions Freeze()
	{
		IsFrozen = true;
		return this;
	}

	/// <summary>
	/// Ensures that the current instance is mutable and can be modified.
	/// </summary>
	/// <remarks>
	/// If the instance is frozen, an <see cref="InvalidOperationException"/> is thrown.
	/// To modify a frozen instance, use the <see cref="Clone"/> method to create a mutable copy.
	/// </remarks>
	/// <exception cref="InvalidOperationException">Thrown if the instance is frozen and cannot be modified.</exception>
	private void EnsureMutable()
	{
		if (IsFrozen) throw new InvalidOperationException("Options instance is frozen. Clone() to modify.");
	}

	#endregion

	#region Cloning

	/// <summary>
	/// Creates a deep unfrozen copy of this options instance.
	/// </summary>
	/// <returns>
	/// A new <see cref="PrettyObjectOptions"/> instance with identical property values.
	/// </returns>
	public PrettyObjectOptions Clone()
	{
		var clone = (PrettyObjectOptions)MemberwiseClone();
		clone.IsFrozen = false;
		return clone;
	}

	#endregion

	#region ToString()

	/// <summary>
	/// Returns a concise textual representation for diagnostics.
	/// </summary>
	public override string ToString()
	{
		return $"Depth={MaxDepth}, Items={MaxCollectionItems}, StrMax={MaxStringLength}, " +
		       $"Props={IncludeProperties}, Fields={IncludeFields}, NonPublic={IncludeNonPublic}, " +
		       $"Sort={SortMembers}, Header={ShowTypeHeader}, UseNs={UseNamespaceForTypes} " +
		       $"IsFrozen={IsFrozen}";
	}

	#endregion
}
