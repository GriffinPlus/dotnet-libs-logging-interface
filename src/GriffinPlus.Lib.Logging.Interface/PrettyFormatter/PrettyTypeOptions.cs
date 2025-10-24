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
public sealed class PrettyTypeOptions
{
	#region Properties

	private bool mUseNamespace = false;

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
	/// The frozen <see cref="PrettyTypeOptions"/> instance.
	/// </returns>
	public PrettyTypeOptions Freeze()
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
	/// A new <see cref="PrettyTypeOptions"/> instance with identical property values.
	/// </returns>
	public PrettyTypeOptions Clone()
	{
		var clone = (PrettyTypeOptions)MemberwiseClone();
		clone.IsFrozen = false;
		return clone;
	}

	#endregion

	#region ToString()

	/// <summary>
	/// Returns a string representation for debugging and diagnostics.
	/// </summary>
	public override string ToString()
	{
		return $"UseNamespace={UseNamespace}";
	}

	#endregion
}
