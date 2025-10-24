///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
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
	/// <param name="assembly">
	/// The assembly to format. If <see langword="null"/>, returns the literal string <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <param name="options">
	/// Options controlling which sections are included. Must not be <see langword="null"/>; callers should pass a preset.
	/// </param>
	/// <param name="tfc">
	/// An optional <see cref="TextFormatContext"/> providing newline, indentation, and culture information.<br/>
	/// If <see langword="null"/>, the engine uses platform defaults.
	/// </param>
	/// <returns>
	/// A human-readable summary string. Depending on <paramref name="options"/>, this can be a multi-line text
	/// including identity, image runtime, modules, referenced assemblies and (optionally) exported types.
	/// </returns>
	public static string FormatAssembly(Assembly? assembly, PrettyAssemblyOptions options, TextFormatContext? tfc = null)
	{
		if (assembly == null) return "<null>";
		if (options == null) throw new ArgumentNullException(nameof(options));

		var builder = new StringBuilder();
		TextFormatContext tf = tfc ?? TextFormatContext.From(null);

		// Header (identity + basic info)
		if (options.IncludeHeader)
		{
			builder.Append(FormatAssemblyName(assembly.GetName())).Append(tf.NewLine);
			if (options.IncludeImageRuntime)
			{
				string? imageRuntime = SafeGetImageRuntimeVersion(assembly);
				if (!string.IsNullOrEmpty(imageRuntime))
					builder.Append("ImageRuntime: ").Append(imageRuntime).Append(tf.NewLine);
			}
			if (options.IncludeLocation)
			{
				string? location = SafeGetLocation(assembly);
				if (!string.IsNullOrEmpty(location))
					builder.Append("Location: ").Append(location).Append(tf.NewLine);
			}
		}

		// Modules
		if (options.IncludeModules)
		{
			Module[] modules = SafeGetModules(assembly);
			Array.Sort(modules, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
			if (modules.Length > 0)
			{
				builder.Append("Modules:").Append(tf.NewLine);
				foreach (Module module in modules)
				{
					builder.Append("  - ").Append(FormatModule(module, options)).Append(tf.NewLine);
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
				builder.Append("References:").Append(tf.NewLine);
				foreach (AssemblyName an in references)
				{
					builder.Append("  - ").Append(FormatAssemblyName(an)).Append(tf.NewLine);
				}
			}
		}

		// Exported/Public types (optionally capped)
		if (options.IncludeExportedTypes)
		{
			Type[] types = SafeGetExportedTypes(assembly);
			Array.Sort(types, (a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));

			if (types.Length > 0)
			{
				var typeOptions = new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes };
				builder.Append("Exported Types:").Append(tf.NewLine);

				if (options.ExportedTypesMax > 0)
				{
					foreach (Type type in types.Take(options.ExportedTypesMax))
					{
						builder.Append("  - ").Append(PrettyTypeEngine.Format(type, typeOptions)).Append(tf.NewLine);
					}

					if (types.Length > options.ExportedTypesMax)
						builder.Append("  …").Append(tf.NewLine);
				}
				else
				{
					foreach (Type type in types)
					{
						builder.Append("  - ").Append(PrettyTypeEngine.Format(type, typeOptions)).Append(tf.NewLine);
					}
				}
			}
		}

		string s = builder.ToString().TrimEnd();
		return TextPostProcessor.ApplyWhole(string.IsNullOrEmpty(s) ? FormatAssemblyName(assembly.GetName()) : s, tf);
	}

	/// <summary>
	/// Formats an <see cref="AssemblyName"/> identity on a single line.
	/// </summary>
	/// <param name="name">The assembly identity. If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.</param>
	/// <returns>
	/// A CLR-style identity such as <c>"MyLib, Version=1.2.3.4, Culture=neutral, PublicKeyToken=abcdef1234567890"</c>.
	/// </returns>
	public static string FormatAssemblyName(AssemblyName? name)
	{
		try
		{
			return name?.ToString() ?? "<null>";
		}
		catch
		{
			return "<unavailable>";
		}
	}

	/// <summary>
	/// Formats a <see cref="Module"/> concisely using its simple name, or <c>"&lt;null&gt;"</c> if <paramref name="module"/> is <see langword="null"/>.
	/// </summary>
	public static string FormatModule(Module? module, PrettyAssemblyOptions options)
	{
		try
		{
			return module?.Name ?? "<null>";
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
