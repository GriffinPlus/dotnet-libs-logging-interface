///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Text;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Options that control how <see cref="Exception"/> instances are formatted by <see cref="PrettyExceptionEngine"/>.
/// </summary>
/// <remarks>
/// The options are mutable for convenience.<br/>
/// Use <see cref="PrettyOptionsBase{PrettyExceptionOptions}.Clone"/> to safely customize presets per call.
/// </remarks>
public sealed class PrettyExceptionOptions : PrettyOptionsBase<PrettyExceptionOptions>
{
	#region Properties

	private bool                 mIncludeType            = true;
	private bool                 mUseNamespaceForTypes   = true;
	private bool                 mIncludeHResult         = false;
	private bool                 mIncludeSource          = false;
	private bool                 mIncludeHelpLink        = false;
	private bool                 mIncludeTargetSite      = true;
	private bool                 mIncludeStackTrace      = true;
	private int                  mStackFrameLimit        = 50;
	private bool                 mIncludeData            = true;
	private bool                 mFlattenAggregates      = true;
	private int                  mMaxInnerExceptionDepth = 4;
	private PrettyObjectOptions? mObjectOptionsForData   = null;

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
	/// Gets or sets the optional object-formatting profile used when rendering <see cref="Exception.Data"/> via
	/// <see cref="PrettyObjectEngine"/>. If <see langword="null"/>, a profile derived from
	/// <see cref="PrettyObjectPresets.Standard"/> with Data-tailored adjustments is used.
	/// The following adjustments are applied to the derived default:
	/// <list type="bullet">
	///     <item>
	///         <description><see cref="PrettyObjectOptions.ShowTypeHeader"/> = <c>false</c> (render content only)</description>
	///     </item>
	///     <item>
	///         <description><see cref="PrettyObjectOptions.AllowMultiline"/> = <c>true</c> (block-style, indented dictionaries)</description>
	///     </item>
	///     <item>
	///         <description><see cref="PrettyObjectOptions.MaxDepth"/> = <c>1</c> (shallow object expansion for readability)</description>
	///     </item>
	/// </list>
	/// </summary>
	/// <seealso cref="ResolveDataObjectOptions"/>
	public PrettyObjectOptions? ObjectOptionsForData
	{
		get => mObjectOptionsForData;
		set
		{
			EnsureMutable();
			mObjectOptionsForData = value;
		}
	}

	/// <summary>
	/// Resolves the effective object-formatting profile to use for rendering <see cref="Exception.Data"/>.
	/// </summary>
	/// <returns>
	/// A frozen <see cref="PrettyObjectOptions"/> instance. If <see cref="ObjectOptionsForData"/> is not set,
	/// the returned options are derived from <see cref="PrettyObjectPresets.Standard"/> with targeted adjustments
	/// that are tailored for <see cref="Exception.Data"/> rendering.
	/// </returns>
	/// <remarks>
	/// The derived default currently applies the following adjustments on top of <see cref="PrettyObjectPresets.Standard"/>:
	/// <list type="bullet">
	///     <item>
	///         <description><see cref="PrettyObjectOptions.ShowTypeHeader"/> = <c>false</c> (render content only)</description>
	///     </item>
	///     <item>
	///         <description><see cref="PrettyObjectOptions.AllowMultiline"/> = <c>true</c> (block-style, indented dictionaries)</description>
	///     </item>
	///     <item>
	///         <description><see cref="PrettyObjectOptions.MaxDepth"/> = <c>1</c> (shallow object expansion for readability)</description>
	///     </item>
	/// </list>
	/// All other options follow <see cref="PrettyObjectPresets.Standard"/>.
	/// </remarks>
	/// <seealso cref="ObjectOptionsForData"/>
	/// <seealso cref="PrettyObjectPresets.Standard"/>
	internal PrettyObjectOptions ResolveDataObjectOptions()
	{
		if (mObjectOptionsForData != null) return mObjectOptionsForData;

		PrettyObjectOptions objectOptions = PrettyObjectPresets.Standard.Clone();
		objectOptions.ShowTypeHeader = false;                      // omit type header for Data
		objectOptions.AllowMultiline = true;                       // ensure block-style readability for Data
		objectOptions.MaxDepth = 1;                                // keep Data object expansion shallow for readability
		objectOptions.UseNamespaceForTypes = UseNamespaceForTypes; // align type rendering with exception options

		// Keep other defaults from Standard (DictionaryFormat/Namespace/Sort…)
		return objectOptions.Freeze();
	}

	#endregion

	#region Freeze Support

	/// <summary>
	/// Makes this options instance read-only. Subsequent attempts to mutate it will throw.
	/// </summary>
	/// <returns>
	/// The frozen <see cref="PrettyExceptionOptions"/> instance.
	/// </returns>
	public override PrettyExceptionOptions Freeze()
	{
		base.Freeze();
		mObjectOptionsForData?.Freeze();
		return this;
	}

	#endregion

	#region Cloning

	/// <summary>
	/// Creates a deep unfrozen copy of this options instance.
	/// </summary>
	/// <returns>
	/// A new <see cref="PrettyExceptionOptions"/> instance with identical property values.
	/// </returns>
	public override PrettyExceptionOptions Clone()
	{
		PrettyExceptionOptions clone = base.Clone();
		clone.mObjectOptionsForData = mObjectOptionsForData?.Clone();
		return clone;
	}

	#endregion

	#region ToString()

	/// <summary>
	/// Returns a concise representation of the exception formatting options for debugging/logging.
	/// </summary>
	public override string ToString()
	{
		var builder = new StringBuilder(256);
		builder.Append($"{nameof(IncludeType)}={IncludeType} (UseNamespaceForTypes={UseNamespaceForTypes}), ");
		builder.Append($"{nameof(IncludeHResult)}={IncludeHResult}, ");
		builder.Append($"{nameof(IncludeSource)}={IncludeSource}, ");
		builder.Append($"{nameof(IncludeHelpLink)}={IncludeHelpLink}, ");
		builder.Append($"{nameof(IncludeTargetSite)}={IncludeTargetSite}, ");
		builder.Append($"{nameof(IncludeStackTrace)}={IncludeStackTrace}/{StackFrameLimit}, ");
		builder.Append($"{nameof(IncludeData)}={IncludeData}");

		if (IncludeData)
		{
			builder.Append(" [");
			AppendObjectOptionsSummary(builder, ResolveDataObjectOptions());
			builder.Append(']');
		}

		builder.Append($", {nameof(FlattenAggregates)}={FlattenAggregates}")
			.Append($", {nameof(MaxInnerExceptionDepth)}={MaxInnerExceptionDepth}")
			.Append($", {nameof(IsFrozen)}={IsFrozen}");

		return builder.ToString();
	}

	/// <summary>
	/// Appends a short summary of the object-formatting profile to the specified <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="builder">The target <see cref="StringBuilder"/>.</param>
	/// <param name="options">The object-formatting options to summarize.</param>
	private static void AppendObjectOptionsSummary(StringBuilder builder, PrettyObjectOptions options)
	{
		builder.Append($"{nameof(PrettyObjectOptions.ShowTypeHeader)}={options.ShowTypeHeader}, ");
		builder.Append($"{nameof(PrettyObjectOptions.UseNamespaceForTypes)}={options.UseNamespaceForTypes}, ");
		builder.Append($"{nameof(PrettyObjectOptions.DictionaryFormat)}={options.DictionaryFormat}, ");
		builder.Append($"{nameof(PrettyObjectOptions.AllowMultiline)}={options.AllowMultiline}, ");
		builder.Append($"{nameof(PrettyObjectOptions.MaxDepth)}={options.MaxDepth}, ");
		builder.Append($"{nameof(PrettyObjectOptions.MaxCollectionItems)}={options.MaxCollectionItems}, ");
		builder.Append($"{nameof(PrettyObjectOptions.IncludeProperties)}={options.IncludeProperties}, ");
		builder.Append($"{nameof(PrettyObjectOptions.IncludeFields)}={options.IncludeFields}, ");
		builder.Append($"{nameof(PrettyObjectOptions.IncludeNonPublic)}={options.IncludeNonPublic}, ");
		builder.Append($"{nameof(PrettyObjectOptions.SortMembers)}={options.SortMembers}");
	}

	#endregion
}
