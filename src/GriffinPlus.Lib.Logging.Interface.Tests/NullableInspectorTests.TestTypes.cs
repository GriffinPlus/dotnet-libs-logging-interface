///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

// ReSharper disable once RedundantNullableDirective
#nullable enable

namespace GriffinPlus.Lib.Logging.Tests.PrettyFormatter;

public sealed partial class NullableInspectorTests
{
	/// <summary>
	/// Provides types compiled with <c>#nullable enable</c> to verify correct handling of
	/// explicit nullability metadata by the <see cref="NullabilityInspector"/>.
	/// </summary>
	/// <remarks>
	/// Each nested class exposes members with varying nullability to ensure that the inspector correctly
	/// distinguishes between <see cref="Nullability.NonNullable"/> and <see cref="Nullability.Nullable"/>.
	/// </remarks>
	internal static class EnabledTypes
	{
		/// <summary>
		/// Contains properties of different nullability kinds.
		/// </summary>
		public sealed class ForProperties
		{
			/// <summary>
			/// A non-nullable reference type property (<c>string</c>).
			/// </summary>
			public string NonNullableRef { get; set; } = string.Empty;

			/// <summary>
			/// A nullable reference type property (<c>string?</c>).
			/// </summary>
			public string? NullableRef { get; set; }

			/// <summary>
			/// A non-nullable value type property (<c>int</c>).
			/// </summary>
			public int NonNullableValue { get; set; }

			/// <summary>
			/// A nullable value type property (<c>int?</c>).
			/// </summary>
			public int? NullableValue { get; set; }
		}

		/// <summary>
		/// Contains fields of different nullability kinds.
		/// </summary>
		public sealed class ForFields
		{
			/// <summary>
			/// A non-nullable reference type field (<c>string</c>).
			/// </summary>
			public string NonNullableRef = string.Empty;

			/// <summary>
			/// A nullable reference type field (<c>string?</c>).
			/// </summary>
			public string? NullableRef;

			/// <summary>
			/// A non-nullable value type field (<c>int</c>).
			/// </summary>
			public int NonNullableValue;

			/// <summary>
			/// A nullable value type field (<c>int?</c>).
			/// </summary>
			public int? NullableValue;
		}

		/// <summary>
		/// Contains methods exposing nullability in parameters and return types.
		/// </summary>
		public sealed class ForMethods
		{
			/// <summary>
			/// Returns a non-nullable reference type.
			/// </summary>
			public string ReturnNonNullable() => string.Empty;

			/// <summary>
			/// Returns a nullable reference type.
			/// </summary>
			public string? ReturnNullable() => null;

			/// <summary>
			/// Accepts a non-nullable reference type parameter.
			/// </summary>
			public void ParamNonNullable(string s) { }

			/// <summary>
			/// Accepts a nullable reference type parameter.
			/// </summary>
			public void ParamNullable(string? s) { }
		}

		/// <summary>
		/// Contains events exposing nullability in events.
		/// </summary>
		public sealed class ForEvents
		{
			/// <summary>
			/// Backing delegate initialized to an empty handler to guarantee non-null at runtime.
			/// </summary>
			private EventHandler mNonNullableEvent = delegate { };

			/// <summary>
			/// An explicit event using a non-nullable <see cref="EventHandler"/> type.
			/// The backing delegate is initialized to a no-op, so it's safe to invoke without null checks.
			/// </summary>
			public event EventHandler NonNullableEvent
			{
				add => mNonNullableEvent = (EventHandler)Delegate.Combine(mNonNullableEvent, value);
				remove => mNonNullableEvent = (EventHandler)Delegate.Remove(mNonNullableEvent, value)!;
			}

			/// <summary>
			/// A standard event with a nullable <see cref="EventHandler"/> delegate.
			/// </summary>
			public event EventHandler? NullableEvent;
		}
	}

	// ReSharper disable once RedundantNullableDirective
#nullable disable

	/// <summary>
	/// Provides types compiled with <c>#nullable disable</c> to simulate legacy or oblivious contexts
	/// where the compiler does not emit nullability metadata.
	/// </summary>
	/// <remarks>
	/// The <see cref="NullabilityInspector"/> should return <see cref="Nullability.Oblivious"/> or
	/// <see cref="Nullability.Unknown"/> for these members, depending on runtime capabilities.
	/// </remarks>
	internal static class DisabledTypes
	{
		/// <summary>
		/// Contains reference type properties compiled without nullability annotations.
		/// </summary>
		public sealed class ForProperties
		{
			/// <summary>
			/// A property declared in an oblivious context (<c>string</c>).
			/// </summary>
			public string RefProp { get; set; } // oblivious
		}

		/// <summary>
		/// Contains reference type fields compiled without nullability annotations.
		/// </summary>
		public sealed class ForFields
		{
			/// <summary>
			/// A field declared in an oblivious context (<c>string</c>).
			/// </summary>
			public string RefField; // oblivious
		}

		/// <summary>
		/// Contains methods compiled without nullability annotations.
		/// </summary>
		public sealed class ForMethods
		{
			/// <summary>
			/// Returns a reference type in an oblivious context.
			/// </summary>
			public string ReturnRef() => null; // oblivious

			/// <summary>
			/// Accepts a reference type parameter in an oblivious context.
			/// </summary>
			public void ParamRef(string s) { } // oblivious
		}
	}
}

// ReSharper disable once UnusedNullableDirective
#nullable restore
