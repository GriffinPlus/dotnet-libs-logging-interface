///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Options that control how <see cref="System.Reflection.Assembly"/> instances are formatted
/// by <see cref="PrettyAssemblyEngine"/>. Designed for logging, debugging and diagnostic output.
/// </summary>
/// <remarks>
///     <para>
///     The default configuration produces a compact single-line header containing the assembly name,
///     version, culture, and public key token. Other sections can be enabled for more detailed inspection.
///     </para>
///     <para>
///     The class is mutable for convenience and implements <see cref="PrettyOptionsBase{PrettyAssemblyOptions}.Clone"/>
///     to allow safe duplication and modification without affecting other formatter instances.
///     </para>
/// </remarks>
public sealed class PrettyAssemblyOptions : PrettyOptionsBase<PrettyAssemblyOptions>
{
	#region Properties

	private bool mIncludeHeader        = true;
	private bool mIncludeImageRuntime  = false;
	private bool mIncludeLocation      = false;
	private bool mIncludeModules       = false;
	private bool mIncludeReferences    = false;
	private bool mIncludeExportedTypes = false;
	private int  mExportedTypesMax     = 20;
	private bool mUseNamespaceForTypes = false;

	/// <summary>
	/// Gets or sets whether the formatted output includes the top-level header with identity information.<br/>
	/// Default is <see langword="true"/>.
	/// </summary>
	public bool IncludeHeader
	{
		get => mIncludeHeader;
		set
		{
			EnsureMutable();
			mIncludeHeader = value;
		}
	}

	/// <summary>
	/// Gets or sets whether to include the CLR image runtime version in the header section.<br/>
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool IncludeImageRuntime
	{
		get => mIncludeImageRuntime;
		set
		{
			EnsureMutable();
			mIncludeImageRuntime = value;
		}
	}

	/// <summary>
	/// Gets or sets whether to include the physical <see cref="System.Reflection.Assembly.Location"/>
	/// path of the assembly in the header section.
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool IncludeLocation
	{
		get => mIncludeLocation;
		set
		{
			EnsureMutable();
			mIncludeLocation = value;
		}
	}

	/// <summary>
	/// Gets or sets whether to list all modules contained in the assembly.<br/>
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool IncludeModules
	{
		get => mIncludeModules;
		set
		{
			EnsureMutable();
			mIncludeModules = value;
		}
	}

	/// <summary>
	/// Gets or sets whether to list all referenced assemblies (dependencies).<br/>
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool IncludeReferences
	{
		get => mIncludeReferences;
		set
		{
			EnsureMutable();
			mIncludeReferences = value;
		}
	}

	/// <summary>
	/// Gets or sets whether to list exported (public) types from the assembly.<br/>
	/// Default is <see langword="false"/>.
	/// </summary>
	public bool IncludeExportedTypes
	{
		get => mIncludeExportedTypes;
		set
		{
			EnsureMutable();
			mIncludeExportedTypes = value;
		}
	}

	/// <summary>
	/// Gets or sets the maximum number of exported types to include in the output.<br/>
	/// A value of <c>0</c> shows no types. A negative value disables the limit.<br/>
	/// Default is <c>20</c>.
	/// </summary>
	public int ExportedTypesMax
	{
		get => mExportedTypesMax;
		set
		{
			EnsureMutable();
			mExportedTypesMax = value;
		}
	}

	/// <summary>
	/// Gets or sets whether type names in module/type listings are shown with their full namespace.<br/>
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

	#region ToString()

	/// <summary>
	/// Creates a string representation of this options instance for debugging purposes.
	/// </summary>
	/// <returns>A readable summary of active flags.</returns>
	public override string ToString()
	{
		return $"Header={IncludeHeader}, ImageRuntime={IncludeImageRuntime}, Location={IncludeLocation}, " +
		       $"Modules={IncludeModules}, References={IncludeReferences}, ExportedTypes={IncludeExportedTypes}({ExportedTypesMax}), " +
		       $"UseNamespace={UseNamespaceForTypes}, IsFrozen={IsFrozen}";
	}

	#endregion
}
