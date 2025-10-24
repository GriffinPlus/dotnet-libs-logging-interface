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
		PrettyTypeOptions typeOptions = profile?.TypeOptions ?? PrettyTypePresets.Full;
		return PrettyTypeEngine.Format(type, typeOptions);
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
	public static string Format(Type? type)
	{
		if (type == null) return "<null>";
		return PrettyTypeEngine.Format(type, PrettyTypePresets.Full);
	}

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
		PrettyTypeOptions typeOptions = profile?.TypeOptions ?? PrettyTypePresets.Full;
		return FormatTypeInternal(types, typeOptions);
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
	public static string Format(Type[]? types)
	{
		PrettyTypeOptions typeOptions = PrettyTypePresets.Full;
		return FormatTypeInternal(types, typeOptions);
	}

	/// <summary>
	/// Internal helper creating a bracketed, comma-separated representation of the supplied type array.
	/// </summary>
	/// <param name="types">
	/// The collection of types to format. May be <see langword="null"/> to explicitly represent a missing value.
	/// </param>
	/// <param name="typeOptions">
	/// Resolved, non-null type formatting options (namespace usage, etc.) already chosen by the caller.
	/// </param>
	/// <returns>
	/// <list type="bullet">
	/// <item><description><c>"&lt;null&gt;"</c> if <paramref name="types"/> is <see langword="null"/>.</description></item>
	/// <item><description><c>"[]"</c> if the array is empty.</description></item>
	/// <item><description>A string like <c>"[System.Int32, string]"</c> otherwise.</description></item>
	/// </list>
	/// </returns>
	/// <remarks>
	/// This method assumes <paramref name="typeOptions"/> has already been resolved (no <see langword="null"/>). It performs
	/// no allocations beyond the output <see cref="StringBuilder"/> sized heuristically to reduce reallocation for small arrays.
	/// </remarks>
	private static string FormatTypeInternal(Type[]? types, PrettyTypeOptions typeOptions)
	{
		if (types == null) return "<null>";
		if (types.Length == 0) return "[]";
		var builder = new StringBuilder(types.Length * 8);
		builder.Append('[');
		for (int i = 0; i < types.Length; i++)
		{
			if (i > 0) builder.Append(", ");
			builder.Append(PrettyTypeEngine.Format(types[i], typeOptions));
		}
		builder.Append(']');
		return builder.ToString();
	}

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
	/// <remarks>
	/// This overload materializes the sequence at most once and delegates to <see cref="Format(Type[], PrettyOptions?)"/>.
	/// </remarks>
	public static string Format(IEnumerable<Type>? types, PrettyOptions? profile)
	{
		if (types == null) return "<null>";

		// Avoid multiple enumeration and reuse the array overload.
		if (types is Type[] array) return Format(array, profile);

		PrettyTypeOptions options = profile?.TypeOptions ?? PrettyTypePresets.Full;
		using IEnumerator<Type> enumerator = types.GetEnumerator();

		// Empty enumeration
		if (!enumerator.MoveNext())
			return "[]";

		var builder = new StringBuilder(64);
		builder.Append('[');

		// First element (already moved)
		builder.Append(PrettyTypeEngine.Format(enumerator.Current, options));

		// Remaining elements
		while (enumerator.MoveNext())
		{
			builder.Append(", ");
			builder.Append(PrettyTypeEngine.Format(enumerator.Current, options));
		}

		builder.Append(']');
		return builder.ToString();
	}

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
		PrettyMemberOptions memberOptions = profile?.MemberOptions ?? PrettyMemberPresets.Standard;
		return PrettyMemberEngine.Format(member, memberOptions);
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
	public static string Format(MemberInfo? member)
	{
		return PrettyMemberEngine.Format(member, PrettyMemberPresets.Standard);
	}

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
		PrettyMemberOptions memberOptions = profile?.MemberOptions ?? PrettyMemberPresets.Standard;
		return PrettyMemberEngine.Format(parameter, memberOptions, owner);
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
		TextFormatContext tf = TextFormatContext.From(profile);
		PrettyAssemblyOptions assemblyOptions = profile?.AssemblyOptions ?? PrettyAssemblyPresets.Minimal;
		return PrettyAssemblyEngine.FormatAssembly(assembly, assemblyOptions, tf);
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
	public static string Format(AssemblyName? name)
	{
		if (name == null) return "<null>";
		return PrettyAssemblyEngine.FormatAssemblyName(name);
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
	public static string Format(Module? module)
	{
		if (module == null) return "<null>";
		return PrettyAssemblyEngine.FormatModule(module, PrettyAssemblyPresets.Minimal);
	}

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

		TextFormatContext tf = TextFormatContext.From(profile);
		PrettyExceptionOptions exceptionOptions = profile?.ExceptionOptions ?? PrettyExceptionPresets.Standard;

		return PrettyExceptionEngine.Format(exception, exceptionOptions, tf);
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
	public static string Format(Exception? exception) => Format(exception, PrettyPresets.Standard);

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
			case MemberInfo member:         return Format(member, profile);
			case ParameterInfo parameter:   return Format(parameter, profile);
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
	public static string Format(object? obj) => Format(obj, PrettyPresets.Standard);
}
