using System;
using System.Reflection;
using System.Text;

// ReSharper disable UseIndexFromEndExpression

namespace GriffinPlus.Lib.Logging;

static partial class PrettyMemberEngine
{
	/// <summary>
	/// Gets the C# accessibility keyword for a method/constructor.
	/// </summary>
	/// <param name="methodInfo">The method or constructor.</param>
	/// <returns>
	/// The accessibility keyword (e.g., <c>public</c>, <c>protected</c>, <c>internal</c>, …).
	/// </returns>
	private static string GetAccessibility(MethodBase methodInfo) => GetAccessibility(
		methodInfo.IsPublic,
		methodInfo.IsFamily,
		methodInfo.IsAssembly,
		methodInfo.IsFamilyOrAssembly,
		methodInfo.IsFamilyAndAssembly);

	/// <summary>
	/// Gets the C# accessibility keyword for a field.
	/// </summary>
	/// <param name="fieldInfo">The field.</param>
	/// <returns>
	/// The accessibility keyword (e.g., <c>public</c>, <c>protected</c>, <c>internal</c>, …).
	/// </returns>
	private static string GetAccessibility(FieldInfo fieldInfo) => GetAccessibility(
		fieldInfo.IsPublic,
		fieldInfo.IsFamily,
		fieldInfo.IsAssembly,
		fieldInfo.IsFamilyOrAssembly,
		fieldInfo.IsFamilyAndAssembly);

	/// <summary>
	/// Determines the accessibility level of a member based on its visibility flags.
	/// </summary>
	/// <param name="isPublic">Indicates whether the member is 'public'.</param>
	/// <param name="isFamily">Indicates whether the member is 'protected'.</param>
	/// <param name="isAssembly">Indicates whether the member is 'internal'.</param>
	/// <param name="isFamilyOrAssembly">Indicates whether the member is 'protected internal'.</param>
	/// <param name="isFamilyAndAssembly">Indicates whether the member is 'private protected'.</param>
	/// <returns>
	/// A string representing the accessibility level of the member. Possible values are  <see langword="public"/>,
	/// <see langword="protected"/>, <see langword="internal"/>,  <see langword="protected internal"/>,
	/// <see langword="private protected"/>, or <see langword="private"/>.
	/// </returns>
	private static string GetAccessibility(
		bool isPublic,
		bool isFamily,
		bool isAssembly,
		bool isFamilyOrAssembly,
		bool isFamilyAndAssembly)
	{
		return isPublic              ? "public"
		       : isFamily            ? "protected"
		       : isAssembly          ? "internal"
		       : isFamilyOrAssembly  ? "protected internal"
		       : isFamilyAndAssembly ? "private protected"
		                               : "private";
	}

	/// <summary>
	/// Appends the appropriate member modifiers (e.g., <see langword="static"/>, <see langword="abstract"/>",
	/// <see langword="virtual"/>, <see langword="override"/>, <see langword="sealed"/>, <see langword="extern"/>) to
	/// the specified <see cref="StringBuilder"/> based on the characteristics of the provided <see cref="MethodBase"/>.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="StringBuilder"/> to which the member modifiers will be appended.<br/>
	/// This parameter cannot be <see langword="null"/>.
	/// </param>
	/// <param name="method">
	/// The <see cref="MethodBase"/> representing the method whose modifiers are to be determined.<br/>
	/// This parameter cannot be <see langword="null"/>.
	/// </param>
	/// <remarks>
	/// The method analyzes the provided <see cref="MethodBase"/> to determine its modifiers, such as whether it is static,
	/// abstract, virtual, or an override. It also checks for specific implementation flags, such as
	/// <see cref="MethodImplAttributes.InternalCall"/> or <see cref="MethodImplAttributes.Unmanaged"/>, to append the "extern"
	/// modifier.
	/// </remarks>
	private static void AppendMemberModifiers(StringBuilder builder, MethodBase method)
	{
		if (method.IsStatic) builder.Append("static ");
		if (method.IsAbstract) builder.Append("abstract ");
		else if (method.IsVirtual)
		{
			bool isOverride = method is MethodInfo methodInfo && methodInfo.GetBaseDefinition() != methodInfo;
			if (isOverride)
			{
				if (method.IsFinal) builder.Append("sealed ");
				builder.Append("override ");
			}
			else
			{
				builder.Append("virtual ");
			}
		}

		// ReSharper disable BitwiseOperatorOnEnumWithoutFlags
		if ((method.MethodImplementationFlags & MethodImplAttributes.InternalCall) != 0 ||
		    (method.MethodImplementationFlags & MethodImplAttributes.Unmanaged) != 0)
			builder.Append("extern ");
		// ReSharper restore BitwiseOperatorOnEnumWithoutFlags
	}

	/// <summary>
	/// Builds the modifier string for fields (e.g., <c>static</c>, <c>readonly</c>, <c>const</c>).
	/// </summary>
	/// <param name="f">The field.</param>
	/// <returns>
	/// A space-terminated modifier string (or empty if none).
	/// </returns>
	private static string GetMemberModifiers(FieldInfo f)
	{
		var builder = new StringBuilder();
		if (f.IsStatic) builder.Append("static ");
		if (f.IsInitOnly) builder.Append("readonly ");
		if (f.IsLiteral) builder.Append("const ");
		return builder.ToString();
	}

	/// <summary>
	/// Determines whether a method returns a task-like type (<c>Task</c>, <c>Task&lt;T&gt;</c>,
	/// <c>ValueTask</c>, <c>ValueTask&lt;T&gt;</c>) without directly referencing ValueTask types,
	/// so the code compiles on frameworks where ValueTask is unavailable.
	/// </summary>
	/// <param name="methodInfo">The method to inspect.</param>
	/// <returns><see langword="true"/> if the return type is task-like; otherwise <see langword="false"/>.</returns>
	private static bool ReturnsTaskLike(MethodInfo methodInfo)
	{
		Type returnType = methodInfo.ReturnType;

		// Non-generic cases
		string? fullName = returnType.FullName; // may be null for open generics / dynamic types
		if (string.Equals(fullName, "System.Threading.Tasks.Task", StringComparison.Ordinal) ||
		    string.Equals(fullName, "System.Threading.Tasks.ValueTask", StringComparison.Ordinal))
		{
			return true;
		}

		// Generic cases: Task<T>, ValueTask<T>
		if (returnType.IsGenericType)
		{
			Type genericTypeDefinition = returnType.GetGenericTypeDefinition();
			string? genericTypeDefinitionName = genericTypeDefinition.FullName;
			if (string.Equals(genericTypeDefinitionName, "System.Threading.Tasks.Task`1", StringComparison.Ordinal) ||
			    string.Equals(genericTypeDefinitionName, "System.Threading.Tasks.ValueTask`1", StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Normalizes whitespace in a string by collapsing multiple spaces into one,
	/// and removing leading/trailing spaces.
	/// </summary>
	/// <param name="s">The input string.</param>
	/// <returns>The normalized string.</returns>
	/// <remarks>
	/// This implementation avoids <see cref="string.Split(char[])"/> and <see cref="string.Join(string,string[])"/>
	/// to reduce allocations, using a state-machine-like loop over the string's characters.
	/// </remarks>
	private static string NormalizeSpaces(string s)
	{
		if (string.IsNullOrEmpty(s)) return s;

		// --- Fast path ---

		// Check if the string is already normalized (no leading/trailing/multiple spaces).
		bool needsNormalization = false;
		if (s[0] == ' ' || s[s.Length - 1] == ' ')
		{
			needsNormalization = true;
		}
		else
		{
			// Check for double spaces
			for (int i = 1; i < s.Length; i++)
			{
				if (s[i] == ' ' && s[i - 1] == ' ')
				{
					needsNormalization = true;
					break;
				}
			}
		}

		if (!needsNormalization) return s;

		// --- End fast path ---

		// The fast path above handles already-normalized strings.
		// For strings requiring normalization, the builder is initialized lazily
		// only when the first non-whitespace character is encountered.
		StringBuilder? builder = null;

		// 'inWhitespace' acts as a state flag.
		// We start 'true' to automatically trim any leading spaces.
		bool inWhitespace = true;

		// ReSharper disable once ForCanBeConvertedToForeach
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];
			if (c == ' ')
			{
				// If we encounter a space, just set the flag and continue.
				// This effectively collapses multiple spaces.
				inWhitespace = true;
			}
			else
			{
				// We found a non-space character.
				// Initialize the builder on the first non-space character found.
				builder ??= new StringBuilder(s.Length);

				// If the *previous* state was whitespace AND this is *not*
				// the very first character being added (builder.Length > 0),
				// then append a single space as a separator.
				if (inWhitespace && builder.Length > 0)
				{
					builder.Append(' ');
				}

				// Append the actual character.
				builder.Append(c);

				// We are no longer in a whitespace block.
				inWhitespace = false;
			}
		}

		// If the builder was never initialized, the string contained
		// only whitespace (or was empty). Return empty string.
		if (builder == null) return string.Empty;

		// Return the built string. Trailing spaces are automatically
		// omitted because the loop ends without appending them.
		return builder.ToString();
	}
}
