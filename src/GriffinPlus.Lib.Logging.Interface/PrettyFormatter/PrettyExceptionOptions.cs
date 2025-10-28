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
/// The options are mutable for convenience. Use <see cref="PrettyOptionsBase{PrettyExceptionOptions}.Clone"/>
/// to safely customize presets per call.
/// </remarks>
public sealed class PrettyExceptionOptions : PrettyOptionsBase<PrettyExceptionOptions>
{
	#region Properties

	private bool             mIncludeType            = true;
	private bool             mUseNamespaceForTypes   = true;
	private bool             mIncludeHResult         = false;
	private bool             mIncludeSource          = false;
	private bool             mIncludeHelpLink        = false;
	private bool             mIncludeTargetSite      = true;
	private bool             mIncludeStackTrace      = true;
	private int              mStackFrameLimit        = 50;
	private bool             mIncludeData            = true;
	private int              mDataMaxItems           = 10;
	private bool             mFlattenAggregates      = true;
	private int              mMaxInnerExceptionDepth = 4;
	private DictionaryFormat mDataDictionaryFormat   = DictionaryFormat.KeyEqualsValue;

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
	/// A value of <c>0</c> shows no frames. A negative value disables the limit.<br/>
	/// Default is <c>50</c>.
	/// </summary>
	public int StackFrameLimit
	{
		get => mStackFrameLimit;
		set
		{
			EnsureMutable();
			mStackFrameLimit = value;
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
	/// A value of <c>0</c> shows no items. A negative value disables the limit.<br/>
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

	/// <summary>
	/// Gets a value controlling how entries in <see cref="Exception.Data"/> are rendered.<br/>
	/// Default is <see cref="DictionaryFormat.KeyEqualsValue"/>.
	/// </summary>
	public DictionaryFormat DataDictionaryFormat
	{
		get => mDataDictionaryFormat;
		set
		{
			EnsureMutable();
			mDataDictionaryFormat = value;
		}
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
