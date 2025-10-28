///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Configuration options that control how <see cref="Type"/> instances are formatted.
/// </summary>
/// <remarks>
///     <para>
///     Currently the only supported setting is <see cref="UseNamespace"/>, which determines
///     whether fully qualified names (including namespaces) are produced.
///     </para>
///     <para>
///     Future versions may add flags for alias behavior, generic-arity visibility,
///     or inclusion of assembly qualification.
///     </para>
/// </remarks>
public sealed class PrettyTypeOptions : PrettyOptionsBase<PrettyTypeOptions>
{
	#region Properties

	private bool mUseNamespace            = false;
	private bool mUseNativeIntegerAliases = false;

	/// <summary>
	/// Gets or sets a value indicating whether namespace prefixes are included in formatted type names.
	/// </summary>
	/// <value>
	/// <see langword="true"/> to include the full namespace;<br/>
	/// <see langword="false"/> to show only the simple (unqualified) type name.
	/// </value>
	public bool UseNamespace
	{
		get => mUseNamespace;
		set
		{
			EnsureMutable();
			mUseNamespace = value;
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether <see cref="IntPtr"/> and <see cref="UIntPtr"/>
	/// are rendered using the C# native integer aliases <see cref="nint"/> and <see cref="nuint"/>.
	/// </summary>
	/// <remarks>
	/// Defaults to <see langword="false"/> to preserve historic output. When set to <see langword="true"/>,
	/// any appearance of <see cref="IntPtr"/> or <see cref="UIntPtr"/> (including within generic arguments,
	/// by-ref types, arrays, and <c>Nullable&lt;T&gt;</c>) is formatted as <c>nint</c> / <c>nuint</c>.
	/// </remarks>
	public bool UseNativeIntegerAliases
	{
		get => mUseNativeIntegerAliases;
		set
		{
			EnsureMutable();
			mUseNativeIntegerAliases = value;
		}
	}

	#endregion

	#region ToString()

	/// <summary>
	/// Returns a string representation for debugging and diagnostics.
	/// </summary>
	public override string ToString()
	{
		return $"UseNamespace={UseNamespace}, UseNativeIntegerAliases={UseNativeIntegerAliases}";
	}

	#endregion
}
