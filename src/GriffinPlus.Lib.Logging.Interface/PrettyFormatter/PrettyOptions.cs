///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Globalization;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Global options that coordinate formatting behavior across the Pretty subsystem.
/// </summary>
/// <remarks>
///     <para>
///     All properties in this formatting profile are <see langword="nullable"/> by design:
///     passing <see langword="null"/> for a property means “use the formatter's default or preset”.
///     This allows callers to override only the aspects they care about while relying on sensible defaults
///     (e.g., <see cref="PrettyTypePresets.Full"/>, <see cref="PrettyMemberPresets.Standard"/>,
///     <see cref="PrettyExceptionPresets.Standard"/>, etc.).
///     </para>
///     <para>
///     The specialized option types (e.g., <see cref="PrettyTypeOptions"/>) remain non-nullable internally
///     to keep the engines simple and fast. The façade resolves <see langword="null"/> values to defaults
///     right before rendering.
///     </para>
/// </remarks>
public sealed class PrettyOptions
{
	#region Properties

	private CultureInfo?            mCulture             = null;
	private string?                 mIndent              = null;
	private string?                 mNewLine             = null;
	private int?                    mMaxLineLength       = null;
	private bool?                   mTruncateLongStrings = null;
	private string?                 mTruncationMarker    = null;
	private PrettyTypeOptions?      mTypeOptions         = null;
	private PrettyMemberOptions?    mMemberOptions       = null;
	private PrettyObjectOptions?    mObjectOptions       = null;
	private PrettyExceptionOptions? mExceptionOptions    = null;
	private PrettyAssemblyOptions?  mAssemblyOptions     = null;

	/// <summary>
	/// Gets or sets the culture used when formatting numbers, dates, and other culture-sensitive values.
	/// If <see langword="null"/>, formatters should fall back to <see cref="CultureInfo.InvariantCulture"/>.<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public CultureInfo? Culture
	{
		get => mCulture;
		set
		{
			EnsureMutable();
			mCulture = value;
		}
	}

	/// <summary>
	/// Gets or sets the string used for one level of indentation in multi-line output.<br/>
	/// If <see langword="null"/>, formatters should fall back to <c>"  "</c> (two spaces).<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public string? Indent
	{
		get => mIndent;
		set
		{
			EnsureMutable();
			mIndent = value;
		}
	}

	/// <summary>
	/// Gets or sets the newline sequence used for multi-line output.<br/>
	/// If <see langword="null"/>, formatters should use <see cref="Environment.NewLine"/>.<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public string? NewLine
	{
		get => mNewLine;
		set
		{
			EnsureMutable();
			mNewLine = value;
		}
	}

	/// <summary>
	/// Gets or sets the maximum line length before wrapping or truncation occurs.<br/>
	/// A value &lt;= 0 disables wrapping. If <see langword="null"/>, the formatter decides.<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public int? MaxLineLength
	{
		get => mMaxLineLength;
		set
		{
			EnsureMutable();
			mMaxLineLength = value;
		}
	}

	/// <summary>
	/// Gets or sets whether long strings should be truncated (instead of wrapped) when a limit applies.<br/>
	/// If <see langword="null"/>, the formatter decides (recommended default: <see langword="true"/>).<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public bool? TruncateLongStrings
	{
		get => mTruncateLongStrings;
		set
		{
			EnsureMutable();
			mTruncateLongStrings = value;
		}
	}

	/// <summary>
	/// Gets or sets the marker appended to truncated text (e.g., an ellipsis).<br/>
	/// If <see langword="null"/>, the formatter should default to <c>"…"</c> (U+2026).<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public string? TruncationMarker
	{
		get => mTruncationMarker;
		set
		{
			EnsureMutable();
			mTruncationMarker = value;
		}
	}

	/// <summary>
	/// Gets or sets specialized options for type-name rendering.<br/>
	/// If <see langword="null"/>, callers expect <see cref="PrettyTypePresets.Full"/> to be used.<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public PrettyTypeOptions? TypeOptions
	{
		get => mTypeOptions;
		set
		{
			EnsureMutable();
			mTypeOptions = value;
		}
	}

	/// <summary>
	/// Gets or sets specialized options for member rendering.<br/>
	/// If <see langword="null"/>, callers expect <see cref="PrettyMemberPresets.Standard"/> to be used.<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public PrettyMemberOptions? MemberOptions
	{
		get => mMemberOptions;
		set
		{
			EnsureMutable();
			mMemberOptions = value;
		}
	}

	/// <summary>
	/// Gets or sets specialized options for object rendering.<br/>
	/// If <see langword="null"/>, callers expect <see cref="PrettyObjectPresets.Standard"/> to be used.<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public PrettyObjectOptions? ObjectOptions
	{
		get => mObjectOptions;
		set
		{
			EnsureMutable();
			mObjectOptions = value;
		}
	}

	/// <summary>
	/// Gets or sets specialized options for exception rendering.<br/>
	/// If <see langword="null"/>, callers expect <see cref="PrettyExceptionPresets.Standard"/> to be used.<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public PrettyExceptionOptions? ExceptionOptions
	{
		get => mExceptionOptions;
		set
		{
			EnsureMutable();
			mExceptionOptions = value;
		}
	}

	/// <summary>
	/// Gets or sets specialized options for assembly rendering.<br/>
	/// If <see langword="null"/>, callers expect <see cref="PrettyAssemblyPresets.Minimal"/> to be used.<br/>
	/// Default is <see langword="null"/>.
	/// </summary>
	public PrettyAssemblyOptions? AssemblyOptions
	{
		get => mAssemblyOptions;
		set
		{
			EnsureMutable();
			mAssemblyOptions = value;
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
	/// The frozen <see cref="PrettyOptions"/> instance.
	/// </returns>
	public PrettyOptions Freeze()
	{
		mTypeOptions?.Freeze();
		mMemberOptions?.Freeze();
		mObjectOptions?.Freeze();
		mExceptionOptions?.Freeze();
		mAssemblyOptions?.Freeze();
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
	/// A new <see cref="PrettyOptions"/> instance with identical property values.
	/// </returns>
	public PrettyOptions Clone()
	{
		var clone = (PrettyOptions)MemberwiseClone();
		clone.mTypeOptions = mTypeOptions?.Clone();
		clone.mMemberOptions = mMemberOptions?.Clone();
		clone.mObjectOptions = mObjectOptions?.Clone();
		clone.mExceptionOptions = mExceptionOptions?.Clone();
		clone.mAssemblyOptions = mAssemblyOptions?.Clone();
		clone.IsFrozen = false;
		return clone;
	}

	#endregion

	#region ToString()

	/// <summary>
	/// Returns a concise diagnostic representation of this options bag.
	/// </summary>
	public override string ToString()
	{
		return $"Culture={Culture?.Name ?? "<null>"}, Indent={Indent ?? "<null>"}, NewLine={(NewLine == null ? "<null>" : "set")}, " +
		       $"MaxLineLen={MaxLineLength?.ToString() ?? "<null>"}, Truncate={TruncateLongStrings?.ToString() ?? "<null>"}, " +
		       $"Marker={TruncationMarker ?? "<null>"}, " +
		       $"Type={(TypeOptions == null ? "<null>" : TypeOptions.ToString())}, " +
		       $"Member={(MemberOptions == null ? "<null>" : MemberOptions.ToString())}, " +
		       $"Object={(ObjectOptions == null ? "<null>" : ObjectOptions.ToString())}, " +
		       $"Exception={(ExceptionOptions == null ? "<null>" : ExceptionOptions.ToString())}, " +
		       $"Assembly={(AssemblyOptions == null ? "<null>" : AssemblyOptions.ToString())}";
	}

	#endregion
}
