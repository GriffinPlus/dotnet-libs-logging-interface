///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// High-level façade that formats reflection artifacts (types, members, assemblies) into
/// compact, human-friendly strings suitable for logs and diagnostics.
/// </summary>
/// <remarks>
/// All methods are pure and thread-safe. Instances of options passed in are not modified.
/// </remarks>
public static class PrettyFormatter
{
	private static readonly LogWriter sLog = LogWriter.Get(typeof(PrettyFormatter));

	// ───────────────────────── Type ─────────────────────────

	/// <summary>
	/// Formats a <see cref="Type"/> using <see cref="PrettyOptions.TypeOptions"/> if provided,
	/// otherwise <see cref="PrettyTypePresets.Full"/>.
	/// </summary>
	/// <param name="type">
	/// The type to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <param name="profile">
	/// Optional formatting profile to resolve <see cref="PrettyOptions.TypeOptions"/>.<br/>
	/// If <see langword="null"/>, <see cref="PrettyTypePresets.Full"/> is used.
	/// </param>
	/// <returns>
	/// A C#-like representation of the type.
	/// </returns>
	public static string Format(Type? type, PrettyOptions? profile)
	{
		if (type == null) return "<null>";
		TextFormatContext tfc = TextFormatContext.From(profile);
		PrettyTypeOptions typeOptions = profile?.TypeOptions ?? PrettyTypePresets.Full;

		try
		{
			return PrettyTypeEngine.Format(type, typeOptions, tfc);
		}
		catch (Exception ex)
		{
			sLog.Write(
				LogLevel.Alert,
				"An unhandled exception occurred while formatting. Exception:\n{0}",
				LogWriter.UnwrapException(ex));

			return "<error>";
		}
	}

	/// <summary>
	/// Formats a <see cref="Type"/> using the <see cref="PrettyTypePresets.Full"/> preset.
	/// </summary>
	/// <param name="type">
	/// The type to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <returns>
	/// A C#-like representation of the type.
	/// </returns>
	public static string Format(Type? type) => Format(type, profile: null);

	/// <summary>
	/// Formats an array of <see cref="Type"/> values using <see cref="PrettyOptions.TypeOptions"/> if provided,
	/// otherwise <see cref="PrettyTypePresets.Full"/>.
	/// </summary>
	/// <param name="types">
	/// The types to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.<br/>
	/// If empty, returns <c>"[]"</c>.
	/// </param>
	/// <param name="profile">
	/// Optional formatting profile to resolve <see cref="PrettyOptions.TypeOptions"/>.<br/>
	/// If <see langword="null"/>, <see cref="PrettyTypePresets.Full"/> is used.
	/// </param>
	/// <returns>
	/// A bracketed, comma-separated list of type representations (e.g., <c>"[int, string]"</c>).
	/// </returns>
	public static string Format(Type[]? types, PrettyOptions? profile)
	{
		if (types == null) return "<null>";
		if (types.Length == 0) return "[]";

		TextFormatContext tfc = TextFormatContext.From(profile);
		PrettyTypeOptions typeOptions = profile?.TypeOptions ?? PrettyTypePresets.Full;

		try
		{
			int capacity = Math.Max(types.Length * 32, 64);
			var builder = new StringBuilder(capacity);
			builder.Append('[');
			for (int i = 0; i < types.Length; i++)
			{
				if (i > 0) builder.Append(", ");
				PrettyTypeEngine.AppendType(builder, types[i], typeOptions, tfc);
			}
			builder.Append(']');
			return builder.ToString();
		}
		catch (Exception ex)
		{
			sLog.Write(
				LogLevel.Alert,
				"An unhandled exception occurred while formatting. Exception:\n{0}",
				LogWriter.UnwrapException(ex));

			return "<error>";
		}
	}

	/// <summary>
	/// Formats an array of <see cref="Type"/> instances using the <see cref="PrettyTypePresets.Full"/> preset.
	/// </summary>
	/// <param name="types">
	/// The types to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.<br/>
	/// If empty, returns <c>"[]"</c>.
	/// </param>
	/// <returns>
	/// A bracketed, comma-separated list of type representations (e.g., <c>"[int, string]"</c>).
	/// </returns>
	public static string Format(Type[]? types) => Format(types, profile: null);

	/// <summary>
	/// Formats a sequence of <see cref="Type"/> values using <see cref="PrettyOptions.TypeOptions"/> if provided,
	/// otherwise <see cref="PrettyTypePresets.Full"/>.
	/// </summary>
	/// <param name="types">
	/// The sequence of types to format.
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// If empty, returns <c>"[]"</c>.
	/// </param>
	/// <param name="profile">
	/// Optional formatting profile to resolve <see cref="PrettyOptions.TypeOptions"/>.<br/>
	/// If <see langword="null"/>, <see cref="PrettyTypePresets.Full"/> is used.
	/// </param>
	/// <returns>
	/// A bracketed, comma-separated list of type representations (e.g., <c>"[int, string]"</c>).
	/// </returns>
	public static string Format(IEnumerable<Type>? types, PrettyOptions? profile)
	{
		if (types == null) return "<null>";

		// Avoid multiple enumeration and reuse the array overload.
		if (types is Type[] array) return Format(array, profile);

		try
		{
			int capacity = 64; // fallback capacity for the StringBuilder
			if (types is ICollection<Type> collection)
			{
				capacity = Math.Max(collection.Count * 32, 64);
			}

			TextFormatContext tfc = TextFormatContext.From(profile);
			PrettyTypeOptions options = profile?.TypeOptions ?? PrettyTypePresets.Full;
			using IEnumerator<Type> enumerator = types.GetEnumerator();

			// Empty enumeration
			if (!enumerator.MoveNext())
				return "[]";

			var builder = new StringBuilder(capacity);
			builder.Append('[');

			// First element (already moved)
			PrettyTypeEngine.AppendType(builder, enumerator.Current, options, tfc);

			// Remaining elements
			while (enumerator.MoveNext())
			{
				builder.Append(", ");
				PrettyTypeEngine.AppendType(builder, enumerator.Current, options, tfc);
			}

			builder.Append(']');
			return builder.ToString();
		}
		catch (Exception ex)
		{
			sLog.Write(
				LogLevel.Alert,
				"An unhandled exception occurred while formatting. Exception:\n{0}",
				LogWriter.UnwrapException(ex));

			return "<error>";
		}
	}

	/// <summary>
	/// Formats a sequence of <see cref="Type"/> values using the <see cref="PrettyTypePresets.Full"/> preset.
	/// </summary>
	/// <param name="types">
	/// The sequence of types to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.<br/>
	/// If empty, returns <c>"[]"</c>.
	/// </param>
	/// <returns>
	/// A bracketed, comma-separated list of type representations (e.g., <c>"[int, string]"</c>).
	/// </returns>
	public static string Format(IEnumerable<Type>? types) => Format(types, profile: null);

	// ───────────────────────── Member ─────────────────────────

	/// <summary>
	/// Formats a <see cref="MemberInfo"/> using <see cref="PrettyOptions.MemberOptions"/> if provided,
	/// otherwise <see cref="PrettyMemberPresets.Standard"/>.
	/// </summary>
	/// <param name="member">
	/// The member to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <param name="profile">
	/// Optional formatting profile to resolve <see cref="PrettyOptions.MemberOptions"/>.<br/>
	/// If <see langword="null"/>, <see cref="PrettyMemberPresets.Standard"/> is used.
	/// </param>
	/// <returns>
	/// A one-line, human-readable signature for the member.
	/// </returns>
	public static string Format(MemberInfo? member, PrettyOptions? profile)
	{
		if (member == null) return "<null>";
		TextFormatContext tfc = TextFormatContext.From(profile);
		PrettyMemberOptions memberOptions = profile?.MemberOptions ?? PrettyMemberPresets.Standard;

		try
		{
			return PrettyMemberEngine.Format(member, memberOptions, tfc);
		}
		catch (Exception ex)
		{
			sLog.Write(
				LogLevel.Alert,
				"An unhandled exception occurred while formatting. Exception:\n{0}",
				LogWriter.UnwrapException(ex));

			return "<error>";
		}
	}

	/// <summary>
	/// Formats a <see cref="MemberInfo"/> using the <see cref="PrettyMemberPresets.Standard"/> preset.
	/// </summary>
	/// <param name="member">
	/// The member to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <returns>
	/// A one-line, human-readable signature for the member.
	/// </returns>
	public static string Format(MemberInfo? member) => Format(member, profile: null);

	/// <summary>
	/// Formats a <see cref="ParameterInfo"/> using <see cref="PrettyOptions.MemberOptions"/> if provided,
	/// otherwise <see cref="PrettyMemberPresets.Standard"/>.
	/// </summary>
	/// <param name="parameter">
	/// The parameter to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <param name="profile">
	/// Optional formatting profile to resolve <see cref="PrettyOptions.MemberOptions"/>.<br/>
	/// If <see langword="null"/>, <see cref="PrettyMemberPresets.Standard"/> is used.
	/// </param>
	/// <param name="owner">
	/// The owning method/constructor; used to detect extension methods and for nullability context.
	/// May be <see langword="null"/>.
	/// </param>
	/// <returns>A formatted parameter segment, including modifiers and (optionally) its name.</returns>
	public static string Format(ParameterInfo? parameter, PrettyOptions? profile, MethodBase? owner = null)
	{
		if (parameter == null) return "<null>";
		TextFormatContext tfc = TextFormatContext.From(profile);
		PrettyMemberOptions memberOptions = profile?.MemberOptions ?? PrettyMemberPresets.Standard;

		try
		{
			return PrettyMemberEngine.Format(parameter, memberOptions, owner, tfc);
		}
		catch (Exception ex)
		{
			sLog.Write(
				LogLevel.Alert,
				"An unhandled exception occurred while formatting. Exception:\n{0}",
				LogWriter.UnwrapException(ex));

			return "<error>";
		}
	}

	// ───────────────────────── Assembly ─────────────────────────

	/// <summary>
	/// Formats an <see cref="Assembly"/> using <see cref="PrettyOptions.AssemblyOptions"/> if provided,
	/// otherwise <see cref="PrettyAssemblyPresets.Minimal"/>.
	/// </summary>
	/// <param name="assembly">
	/// The assembly to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <param name="profile">
	/// Optional formatting profile to resolve <see cref="PrettyOptions.AssemblyOptions"/>.<br/>
	/// If <see langword="null"/>, <see cref="PrettyAssemblyPresets.Minimal"/> is used.
	/// </param>
	/// <returns>
	/// A human-readable description of the assembly.
	/// </returns>
	public static string Format(Assembly? assembly, PrettyOptions? profile)
	{
		if (assembly == null) return "<null>";
		TextFormatContext tfc = TextFormatContext.From(profile);
		PrettyAssemblyOptions assemblyOptions = profile?.AssemblyOptions ?? PrettyAssemblyPresets.Minimal;

		try
		{
			return PrettyAssemblyEngine.FormatAssembly(assembly, assemblyOptions, tfc);
		}
		catch (Exception ex)
		{
			sLog.Write(
				LogLevel.Alert,
				"An unhandled exception occurred while formatting. Exception:\n{0}",
				LogWriter.UnwrapException(ex));

			return "<error>";
		}
	}

	/// <summary>
	/// Formats an <see cref="Assembly"/> using the <see cref="PrettyAssemblyPresets.Minimal"/> preset.
	/// </summary>
	/// <param name="assembly">
	/// The assembly to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <returns>
	/// A human-readable description of the assembly.
	/// </returns>
	public static string Format(Assembly? assembly) => Format(assembly, profile: null);

	/// <summary>
	/// Formats an <see cref="AssemblyName"/> identity in CLR canonical form.
	/// </summary>
	/// <param name="name">
	/// The assembly identity.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <param name="profile">
	/// This parameter is ignored, but included for API consistency.
	/// </param>
	/// <remarks>
	/// This method produces a single-line description of the assembly identity only; it does not
	/// include modules, references, or exported types. For more detailed assembly diagnostics, use
	/// <see cref="Format(Assembly?, PrettyOptions?)"/> instead.
	/// </remarks>
	public static string Format(AssemblyName? name, PrettyOptions? profile)
	{
		if (name == null) return "<null>";

		try
		{
			// The engine for AssemblyName formatting currently takes no options.
			return PrettyAssemblyEngine.FormatAssemblyName(name);
		}
		catch (Exception ex)
		{
			sLog.Write(
				LogLevel.Alert,
				"An unhandled exception occurred while formatting. Exception:\n{0}",
				LogWriter.UnwrapException(ex));

			return "<error>";
		}
	}

	/// <summary>
	/// Formats an <see cref="AssemblyName"/> identity in CLR canonical form.
	/// </summary>
	/// <param name="name">
	/// The assembly identity.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <remarks>
	/// This method produces a single-line description of the assembly identity only; it does not
	/// include modules, references, or exported types. For more detailed assembly diagnostics, use
	/// <see cref="Format(Assembly?, PrettyOptions?)"/> instead.
	/// </remarks>
	public static string Format(AssemblyName? name) => Format(name, profile: null);

	/// <summary>
	/// Formats a <see cref="Module"/> using <see cref="PrettyOptions.AssemblyOptions"/> if provided,
	/// otherwise <see cref="PrettyAssemblyPresets.Minimal"/>.
	/// </summary>
	/// <param name="module">
	/// The module to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <param name="profile">
	/// Optional formatting profile to resolve <see cref="PrettyOptions.AssemblyOptions"/>.<br/>
	/// If <see langword="null"/>, <see cref="PrettyAssemblyPresets.Minimal"/> is used.
	/// </param>
	/// <returns>
	/// A concise one-line module description.
	/// </returns>
	public static string Format(Module? module, PrettyOptions? profile)
	{
		if (module == null) return "<null>";
		PrettyAssemblyOptions assemblyOptions = profile?.AssemblyOptions ?? PrettyAssemblyPresets.Minimal;

		try
		{
			// The engine for Module formatting currently ignores the options,
			// but we pass them for consistency and future-proofing.
			return PrettyAssemblyEngine.FormatModule(module, assemblyOptions);
		}
		catch (Exception ex)
		{
			sLog.Write(
				LogLevel.Alert,
				"An unhandled exception occurred while formatting. Exception:\n{0}",
				LogWriter.UnwrapException(ex));

			return "<error>";
		}
	}

	/// <summary>
	/// Formats a <see cref="Module"/> using <see cref="PrettyAssemblyPresets.Minimal"/>.
	/// </summary>
	/// <param name="module">
	/// The module to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <returns>
	/// A concise one-line module description.
	/// </returns>
	public static string Format(Module? module) => Format(module, profile: null);

	// ───────────────────────── Exception ─────────────────────────

	/// <summary>
	/// Formats an <see cref="Exception"/> using global <see cref="PrettyOptions"/> (newline/indent/culture) and exception-specific options.
	/// </summary>
	/// <param name="exception">
	/// The exception to format; if <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <param name="profile">
	/// Optional formatting profile controlling text formatting (newline/indent/culture) and providing an optional
	/// <see cref="PrettyOptions.ExceptionOptions"/> preset.<br/>
	/// If <see langword="null"/>, <see cref="PrettyExceptionPresets.Standard"/> is used.
	/// </param>
	/// <returns>
	/// A formatted exception string suitable for deterministic snapshot testing and log output.
	/// </returns>
	public static string Format(Exception? exception, PrettyOptions? profile)
	{
		if (exception == null) return "<null>";
		TextFormatContext tfc = TextFormatContext.From(profile);
		PrettyExceptionOptions exceptionOptions = profile?.ExceptionOptions ?? PrettyExceptionPresets.Standard;

		try
		{
			return PrettyExceptionEngine.Format(exception, exceptionOptions, tfc);
		}
		catch (Exception ex)
		{
			sLog.Write(
				LogLevel.Alert,
				"An unhandled exception occurred while formatting. Exception:\n{0}",
				LogWriter.UnwrapException(ex));

			return "<error>";
		}
	}

	/// <summary>
	/// Formats an <see cref="Exception"/> using the <see cref="PrettyExceptionPresets.Standard"/> preset.
	/// </summary>
	/// <param name="exception">
	/// The exception to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <returns>
	/// A formatted exception string suitable for log output.
	/// </returns>
	public static string Format(Exception? exception) => Format(exception, profile: null);

	// ───────────────────────── Object ─────────────────────────

	/// <summary>
	/// Formats an arbitrary object using the most appropriate engine based on its runtime type.
	/// </summary>
	/// <remarks>
	/// This is the primary "smart" formatting façade. It inspects <paramref name="obj"/> and
	/// dispatches to the correct specialized engine based on its type. This method also applies
	/// global text settings (Indent, NewLine, Culture) from the profile.
	/// </remarks>
	/// <param name="obj">
	/// The object to format.<br/>
	/// If <see langword="null"/>, the method returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <param name="profile">
	/// The global formatting profile containing text settings and specialized options for all engines.<br/>
	/// If <see langword="null"/>, standard presets for each engine are used (e.g., <see cref="PrettyExceptionPresets.Standard"/> for Exceptions).
	/// </param>
	/// <returns>
	/// A formatted string representation of <paramref name="obj"/>, generated by the appropriate specialized engine.
	/// </returns>
	public static string Format(object? obj, PrettyOptions? profile)
	{
		if (obj == null) return "<null>";

		switch (obj)
		{
			case Exception exception:       return Format(exception, profile);
			case Type type:                 return Format(type, profile);
			case ParameterInfo parameter:   return Format(parameter, profile);
			case MemberInfo member:         return Format(member, profile); // MemberInfo is base for PropertyInfo, MethodInfo, etc.
			case Assembly assembly:         return Format(assembly, profile);
			case Module module:             return Format(module, profile);
			case AssemblyName assemblyName: return Format(assemblyName, profile);
			case Type[] types:              return Format(types, profile);
			case IEnumerable<Type> seq:     return Format(seq, profile);

			default:
				TextFormatContext tf = TextFormatContext.From(profile);
				PrettyObjectOptions objectOptions = profile?.ObjectOptions ?? PrettyObjectPresets.Standard;
				return PrettyObjectEngine.Format(obj, objectOptions, tf);
		}
	}

	/// <summary>
	/// Formats an arbitrary object using the <see cref="PrettyObjectPresets.Standard"/> preset.
	/// </summary>
	/// <param name="obj">
	/// The object to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <returns>
	/// A human-readable string representation of the object.
	/// </returns>
	public static string Format(object? obj) => Format(obj, profile: null);
}
