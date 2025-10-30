///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Provides a base class for options objects that support immutability through freezing and cloning.
/// </summary>
/// <typeparam name="TOptions">
/// The type of the derived options class. This type must inherit from <see cref="PrettyOptionsBase{TOptions}"/>.
/// </typeparam>
/// <remarks>
/// This class is designed to facilitate the creation of options objects that can be made immutable
/// (frozen) to prevent further modifications. Once frozen, any attempt to modify the instance will result in an
/// exception. To modify a frozen instance, use the <see cref="Clone"/> method to create a mutable copy.
/// </remarks>
public class PrettyOptionsBase<TOptions>
	where TOptions : PrettyOptionsBase<TOptions>
{
	/// <summary>
	/// Constant indicating that there is no limit.
	/// </summary>
	public const int Unlimited = -1;

	#region Freeze Support

	/// <summary>
	/// Gets or sets a value indicating whether this options instance is frozen (read-only).
	/// </summary>
	public bool IsFrozen { get; private set; }

	/// <summary>
	/// Makes this options instance read-only. Subsequent attempts to mutate it will throw.
	/// </summary>
	/// <returns>
	/// The frozen <see cref="TOptions"/> instance.
	/// </returns>
	public virtual TOptions Freeze()
	{
		IsFrozen = true;
		return (TOptions)this;
	}

	/// <summary>
	/// Ensures that the current instance is mutable and can be modified.
	/// </summary>
	/// <remarks>
	/// If the instance is frozen, an <see cref="InvalidOperationException"/> is thrown.
	/// To modify a frozen instance, use the <see cref="Clone"/> method to create a mutable copy.
	/// </remarks>
	/// <exception cref="InvalidOperationException">Thrown if the instance is frozen and cannot be modified.</exception>
	protected void EnsureMutable()
	{
		if (IsFrozen) throw new InvalidOperationException("Options instance is frozen. Clone() to modify.");
	}

	#endregion

	#region Cloning

	/// <summary>
	/// Creates a deep unfrozen copy of the current options object.
	/// </summary>
	/// <returns>
	/// A new options instance with identical property values.
	/// </returns>
	public virtual TOptions Clone()
	{
		var clone = (TOptions)MemberwiseClone();
		clone.IsFrozen = false;
		return clone;
	}

	#endregion
}
