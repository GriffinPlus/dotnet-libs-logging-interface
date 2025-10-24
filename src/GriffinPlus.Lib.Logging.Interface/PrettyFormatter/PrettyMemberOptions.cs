///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Configuration options that control how <see cref="MemberInfo"/> and <see cref="ParameterInfo"/> values are formatted.
/// </summary>
/// <remarks>
/// Use <see cref="PrettyMemberPresets"/> for common configurations and call <see cref="Clone"/> if you need
/// a mutable copy to tweak.
/// </remarks>
public sealed class PrettyMemberOptions
{
	#region Properties

	private bool                             mIncludeDeclaringType            = true;
	private bool                             mShowAccessibility               = true;
	private bool                             mShowMemberModifiers             = true;
	private bool                             mShowAsyncForAsyncMethods        = true;
	private bool                             mShowParameterNames              = true;
	private bool                             mShowNullabilityAnnotations      = true;
	private bool                             mShowAttributes                  = false;
	private bool                             mShowParameterAttributes         = false;
	private Func<CustomAttributeData, bool>? mAttributeFilter                 = null;
	private int                              mAttributeMaxElements            = 3;
	private bool                             mShowGenericConstraintsOnMethods = true;
	private bool                             mShowGenericConstraintsOnTypes   = true;
	private bool                             mUseNamespaceForTypes            = true;

	/// <summary>
	/// Gets or sets whether the declaring type prefix (e.g., <c>MyType.</c>) is included.
	/// </summary>
	public bool IncludeDeclaringType
	{
		get => mIncludeDeclaringType;
		set
		{
			EnsureMutable();
			mIncludeDeclaringType = value;
		}
	}

	/// <summary>
	/// Gets or sets whether accessibility keywords are shown (e.g., <see langword="public"/>,
	/// <see langword="internal"/>, <see langword="protected"/>).
	/// </summary>
	public bool ShowAccessibility
	{
		get => mShowAccessibility;
		set
		{
			EnsureMutable();
			mShowAccessibility = value;
		}
	}

	/// <summary>
	/// Gets or sets whether member modifiers are shown (e.g., <see langword="static"/>,
	/// <see langword="virtual"/>, <see langword="override"/>, <see langword="readonly"/>).
	/// </summary>
	public bool ShowMemberModifiers
	{
		get => mShowMemberModifiers;
		set
		{
			EnsureMutable();
			mShowMemberModifiers = value;
		}
	}

	/// <summary>
	/// Gets or sets whether methods that return <c>Task</c>/<c>ValueTask</c> are decorated with the
	/// cosmetic <c>async</c> keyword in the output.
	/// </summary>
	public bool ShowAsyncForAsyncMethods
	{
		get => mShowAsyncForAsyncMethods;
		set
		{
			EnsureMutable();
			mShowAsyncForAsyncMethods = value;
		}
	}

	/// <summary>
	/// Gets or sets whether parameter names are included in parameter lists.
	/// </summary>
	public bool ShowParameterNames
	{
		get => mShowParameterNames;
		set
		{
			EnsureMutable();
			mShowParameterNames = value;
		}
	}

	/// <summary>
	/// Gets or sets whether nullable reference annotations (<c>?</c>) are shown when
	/// metadata is available (from compilers that emit nullability info).
	/// </summary>
	public bool ShowNullabilityAnnotations
	{
		get => mShowNullabilityAnnotations;
		set
		{
			EnsureMutable();
			mShowNullabilityAnnotations = value;
		}
	}

	/// <summary>
	/// Gets or sets whether attributes applied to members are shown.
	/// </summary>
	public bool ShowAttributes
	{
		get => mShowAttributes;
		set
		{
			EnsureMutable();
			mShowAttributes = value;
		}
	}

	/// <summary>
	/// Gets or sets whether attributes applied to parameters are shown (only relevant if
	/// <see cref="ShowAttributes"/> is <see langword="true"/>).
	/// </summary>
	public bool ShowParameterAttributes
	{
		get => mShowParameterAttributes;
		set
		{
			EnsureMutable();
			mShowParameterAttributes = value;
		}
	}

	/// <summary>
	/// Optional filter invoked for each <see cref="CustomAttributeData"/>; return
	/// <see langword="true"/> to keep the attribute, <see langword="false"/> to hide it.
	/// If <see langword="null"/>, no filtering is applied.
	/// </summary>
	public Func<CustomAttributeData, bool>? AttributeFilter
	{
		get => mAttributeFilter;
		set
		{
			EnsureMutable();
			mAttributeFilter = value;
		}
	}

	/// <summary>
	/// Maximum number of constructor/named arguments shown per attribute (0 = only the attribute name).
	/// </summary>
	public int AttributeMaxElements
	{
		get => mAttributeMaxElements;
		set
		{
			EnsureMutable();
			mAttributeMaxElements = value;
		}
	}

	/// <summary>
	/// Gets or sets whether generic method constraints (<c>where T : …</c>) are included.
	/// </summary>
	public bool ShowGenericConstraintsOnMethods
	{
		get => mShowGenericConstraintsOnMethods;
		set
		{
			EnsureMutable();
			mShowGenericConstraintsOnMethods = value;
		}
	}

	/// <summary>
	/// Gets or sets whether generic type constraints (<c>where T : …</c>) are included when formatting a
	/// <see cref="System.Type"/> through the member engine.
	/// </summary>
	public bool ShowGenericConstraintsOnTypes
	{
		get => mShowGenericConstraintsOnTypes;
		set
		{
			EnsureMutable();
			mShowGenericConstraintsOnTypes = value;
		}
	}

	/// <summary>
	/// Gets or sets whether types that appear inside member signatures should be printed with namespaces (fully qualified).
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
	/// The frozen <see cref="PrettyMemberOptions"/> instance.
	/// </returns>
	public PrettyMemberOptions Freeze()
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
	/// A new <see cref="PrettyMemberOptions"/> instance with identical property values.
	/// </returns>
	public PrettyMemberOptions Clone()
	{
		var clone = (PrettyMemberOptions)MemberwiseClone();
		clone.IsFrozen = false;
		return clone;
	}

	#endregion

	#region ToString()

	/// <summary>
	/// Returns a concise string for diagnostics.
	/// </summary>
	public override string ToString()
	{
		return
			$"DeclaringType={IncludeDeclaringType}, Access={ShowAccessibility}, Mods={ShowMemberModifiers}, " +
			$"Async={ShowAsyncForAsyncMethods}, ParamNames={ShowParameterNames}, Nullability={ShowNullabilityAnnotations}, " +
			"Attrs={ShowAttributes}/{ShowParameterAttributes}, AttrMax={AttributeMaxElements}, WhereM={ShowGenericConstraintsOnMethods}, " +
			$"WhereT={{ShowGenericConstraintsOnTypes}}, UseNs={{UseNamespaceForTypes}}, IsFrozen={IsFrozen}";
	}

	#endregion
}
