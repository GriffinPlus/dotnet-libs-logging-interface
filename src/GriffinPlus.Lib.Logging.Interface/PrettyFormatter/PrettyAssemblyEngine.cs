///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Internal engine for formatting <see cref="Assembly"/>, <see cref="Module"/>, and <see cref="AssemblyName"/>.<br/>
/// Produces compact one-liners or multi-line summaries depending on the provided options.
/// </summary>
/// <remarks>
/// The engine is pure and thread-safe; it keeps no mutable state.
/// It relies on <see cref="PrettyTypeEngine"/> for type name rendering where needed.
/// </remarks>
static class PrettyAssemblyEngine
{
	/// <summary>
	/// Formats an <see cref="Assembly"/> to a summary string using the specified <paramref name="options"/>.
	/// </summary>
	/// <param name="assembly">The assembly to format.</param>
	/// <param name="options">Options controlling which sections are included.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A human-readable summary string. Depending on <paramref name="options"/>, this can be a multi-line text
	/// including identity, image runtime, modules, referenced assemblies and (optionally) exported types.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="assembly"/> or <paramref name="options"/> is <see langword="null"/>.
	/// </exception>
	public static string FormatAssembly(Assembly assembly, PrettyAssemblyOptions options, TextFormatContext tfc)
	{
		if (assembly == null) throw new ArgumentNullException(nameof(assembly));
		if (options == null) throw new ArgumentNullException(nameof(options));

		// Quick path: No details requested, just return the assembly identity.
		if (options is { IncludeHeader: false, IncludeModules: false, IncludeReferences: false, IncludeExportedTypes: false })
		{
			return TextPostProcessor.ApplyWhole(FormatAssemblyName(assembly.GetName()), tfc);
		}

		var builder = new StringBuilder();

		// Header (identity + basic info)
		if (options.IncludeHeader)
		{
			builder.Append(FormatAssemblyName(assembly.GetName())).Append(tfc.NewLine);
			if (options.IncludeImageRuntime)
			{
				string? imageRuntime = SafeGetImageRuntimeVersion(assembly);
				if (!string.IsNullOrEmpty(imageRuntime))
					builder.Append("ImageRuntime: ").Append(imageRuntime).Append(tfc.NewLine);
			}
			if (options.IncludeLocation)
			{
				string? location = SafeGetLocation(assembly);
				if (!string.IsNullOrEmpty(location))
					builder.Append("Location: ").Append(location).Append(tfc.NewLine);
			}
		}

		// Modules
		if (options.IncludeModules)
		{
			Module[] modules = SafeGetModules(assembly);
			Array.Sort(modules, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
			if (modules.Length > 0)
			{
				builder.Append("Modules:").Append(tfc.NewLine);
				foreach (Module module in modules)
				{
					builder.Append("  - ").Append(FormatModule(module, options)).Append(tfc.NewLine);
				}
			}
		}

		// References
		if (options.IncludeReferences)
		{
			AssemblyName[] references = SafeGetReferencedAssemblies(assembly);
			Array.Sort(references, (a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
			if (references.Length > 0)
			{
				builder.Append("References:").Append(tfc.NewLine);
				foreach (AssemblyName an in references)
				{
					builder.Append("  - ").Append(FormatAssemblyName(an)).Append(tfc.NewLine);
				}
			}
		}

		// Exported/Public types (optionally capped)
		if (options.IncludeExportedTypes && options.ExportedTypesMax != 0)
		{
			Type[] types = SafeGetExportedTypes(assembly);
			Array.Sort(types, (a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));

			if (types.Length > 0)
			{
				var typeOptions = new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes };
				builder.Append("Exported Types:").Append(tfc.NewLine);

				int limit = options.ExportedTypesMax;
				IEnumerable<Type> typesToTake = types;

				// Apply limit if it's non-negative
				if (limit >= 0)
				{
					typesToTake = types.Take(limit);
				}

				foreach (Type type in typesToTake)
				{
					builder.Append("  - ");
					PrettyTypeEngine.AppendType(builder, type, typeOptions, tfc);
					builder.Append(tfc.NewLine);
				}

				if (limit >= 0 && types.Length > limit)
					builder.Append("  ").Append(tfc.TruncationMarker).Append(tfc.NewLine);
			}
		}

		string s = builder.ToString().TrimEnd();
		return TextPostProcessor.ApplyWhole(string.IsNullOrEmpty(s) ? FormatAssemblyName(assembly.GetName()) : s, tfc);
	}

	/// <summary>
	/// Formats an <see cref="AssemblyName"/> identity on a single line.
	/// </summary>
	/// <param name="name">The assembly identity.</param>
	/// <returns>
	/// A CLR-style identity such as <c>"MyLib, Version=1.2.3.4, Culture=neutral, PublicKeyToken=abcdef1234567890"</c>.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="name"/> is <see langword="null"/>.
	/// </exception>
	public static string FormatAssemblyName(AssemblyName name)
	{
		if (name == null) throw new ArgumentNullException(nameof(name));

		try
		{
			return name.ToString();
		}
		catch
		{
			return "<unavailable>";
		}
	}

	/// <summary>
	/// Formats a <see cref="Module"/> concisely using its simple name.
	/// </summary>
	/// <param name="module">The module to format.</param>
	/// <param name="options">Options controlling the formatting.</param>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="module"/> or <paramref name="options"/> is <see langword="null"/>.
	/// </exception>
	public static string FormatModule(Module module, PrettyAssemblyOptions options)
	{
		if (module == null) throw new ArgumentNullException(nameof(module));
		if (options == null) throw new ArgumentNullException(nameof(options));

		try
		{
			return module.Name;
		}
		catch
		{
			return "<unavailable>";
		}
	}

	// ——————————————————————— Safe getters ———————————————————————

	private static string? SafeGetImageRuntimeVersion(Assembly assembly)
	{
		try { return assembly.ImageRuntimeVersion; }
		catch { return null; }
	}

	private static string? SafeGetLocation(Assembly assembly)
	{
		try { return assembly.Location; }
		catch { return null; }
	}

	private static Module[] SafeGetModules(Assembly assembly)
	{
		try { return assembly.GetModules(); }
		catch { return []; }
	}

	private static AssemblyName[] SafeGetReferencedAssemblies(Assembly assembly)
	{
		try { return assembly.GetReferencedAssemblies(); }
		catch { return []; }
	}

	private static Type[] SafeGetExportedTypes(Assembly assembly)
	{
		try { return assembly.GetExportedTypes(); }
		catch { return []; }
	}
}
