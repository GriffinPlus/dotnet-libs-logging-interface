///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Options that control how <see cref="Exception"/> instances are formatted by <see cref="PrettyExceptionEngine"/>.
/// </summary>
/// <remarks>
/// The options are mutable for convenience. Use <see cref="Clone"/> to safely customize presets per call.
/// </remarks>
public sealed class PrettyExceptionOptions
{
	#region Properties

	private bool mIncludeType            = true;
	private bool mUseNamespaceForTypes   = true;
	private bool mIncludeHResult         = false;
	private bool mIncludeSource          = false;
	private bool mIncludeHelpLink        = false;
	private bool mIncludeTargetSite      = true;
	private bool mIncludeStackTrace      = true;
	private int  mStackFrameLimit        = 50;
	private bool mIncludeData            = true;
	private int  mDataMaxItems           = 10;
	private bool mFlattenAggregates      = true;
	private int  mMaxInnerExceptionDepth = 4;

	/// <summary>
	/// Gets or sets whether the exception type name is included before the message.<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool IncludeType
	{
		get => mIncludeType;
		set
		{
			EnsureMutable();
			mIncludeType = value;
		}
	}

	/// <summary>
	/// Gets or sets whether type names should include their namespaces.<br/>
	/// Default is <see langword="true"/>.
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
	/// Gets or sets whether <see cref="Exception.HResult"/> is shown.<br/>
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool IncludeHResult
	{
		get => mIncludeHResult;
		set
		{
			EnsureMutable();
			mIncludeHResult = value;
		}
	}

	/// <summary>
	/// Gets or sets whether <see cref="Exception.Source"/> is shown.<br/>
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool IncludeSource
	{
		get => mIncludeSource;
		set
		{
			EnsureMutable();
			mIncludeSource = value;
		}
	}

	/// <summary>
	/// Gets or sets whether <see cref="Exception.HelpLink"/> is shown when present.<br/>
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool IncludeHelpLink
	{
		get => mIncludeHelpLink;
		set
		{
			EnsureMutable();
			mIncludeHelpLink = value;
		}
	}

	/// <summary>
	/// Gets or sets whether <see cref="Exception.TargetSite"/> is shown.<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool IncludeTargetSite
	{
		get => mIncludeTargetSite;
		set
		{
			EnsureMutable();
			mIncludeTargetSite = value;
		}
	}

	/// <summary>
	/// Gets or sets whether the stack trace should be included.<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool IncludeStackTrace
	{
		get => mIncludeStackTrace;
		set
		{
			EnsureMutable();
			mIncludeStackTrace = value;
		}
	}

	/// <summary>
	/// Gets or sets a maximum number of stack frames to print per exception.<br/>
	/// A value &lt;= 0 disables the limit.
	/// Default is <c>50</c>.
	/// </summary>
	public int StackFrameLimit
	{
		get => mStackFrameLimit;
		set
		{
			EnsureMutable();
			mStackFrameLimit = value < 0 ? 0 : value; // 0 = unlimited
		}
	}

	/// <summary>
	/// Gets or sets whether the <see cref="Exception.Data"/> dictionary is printed.<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool IncludeData
	{
		get => mIncludeData;
		set
		{
			EnsureMutable();
			mIncludeData = value;
		}
	}

	/// <summary>
	/// Gets or sets the maximum number of <see cref="Exception.Data"/> entries printed.<br/>
	/// A value &lt;= 0 disables the limit.<br/>
	/// Default is <c>10</c>.
	/// </summary>
	public int DataMaxItems
	{
		get => mDataMaxItems;
		set
		{
			EnsureMutable();
			mDataMaxItems = value;
		}
	}

	/// <summary>
	/// Gets or sets whether <see cref="AggregateException"/> should be flattened
	/// before rendering (i.e., using <see cref="AggregateException.Flatten"/>).<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool FlattenAggregates
	{
		get => mFlattenAggregates;
		set
		{
			EnsureMutable();
			mFlattenAggregates = value;
		}
	}

	/// <summary>
	/// Gets or sets the maximum recursion depth for inner exceptions (0 = only the root).<br/>
	/// Default is <c>4</c>.
	/// </summary>
	public int MaxInnerExceptionDepth
	{
		get => mMaxInnerExceptionDepth;
		set
		{
			EnsureMutable();
			mMaxInnerExceptionDepth = value < 0 ? 0 : value;
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
	/// The frozen <see cref="PrettyExceptionOptions"/> instance.
	/// </returns>
	public PrettyExceptionOptions Freeze()
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
	/// A new <see cref="PrettyExceptionOptions"/> instance with identical property values.
	/// </returns>
	public PrettyExceptionOptions Clone()
	{
		var clone = (PrettyExceptionOptions)MemberwiseClone();
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
		return $"Type={IncludeType} (UseNs={UseNamespaceForTypes}), HResult={IncludeHResult}, " +
		       $"Source={IncludeSource}, HelpLink={IncludeHelpLink}, TargetSite={IncludeTargetSite}, " +
		       $"Stack={IncludeStackTrace}/{StackFrameLimit}, Data={IncludeData}/{DataMaxItems}, " +
		       $"FlattenAgg={FlattenAggregates}, Depth={MaxInnerExceptionDepth}, IsFrozen={IsFrozen}";
	}

	#endregion
}
