///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

using GriffinPlus.Lib.Logging;

// ReSharper disable CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
// ReSharper disable NotResolvedInText
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
// ReSharper disable ReplaceSubstringWithRangeIndexer
// ReSharper disable UnusedMember.Local
// ReSharper disable UseUtf8StringLiteral

#pragma warning disable CA1845 // Use span-based 'AsSpan' method
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace PrettyFormatterDemo;

// --------------------------------------------------------------------------------------------------------------------
// --- Demo Data Structures ---
// --------------------------------------------------------------------------------------------------------------------

[Serializable]
public class Person
{
	// Properties
	public  int                        Id          { get; set; } = 0;
	public  string                     FirstName   { get; set; } = "";
	public  string?                    MiddleName  { get; set; } = null; // Nullable Reference Type
	public  string                     LastName    { get; set; } = "";
	private int                        Age         { get; set; } = 42; // Private property
	public  Address                    HomeAddress { get; set; } = new();
	public  List<Order>                Orders      { get; set; } = [];
	public  Dictionary<string, string> Tags        { get; set; } = [];

	// Fields
	public readonly string ReadOnlyField = "Immutable";

	// Constants
	private const string Secret = "Code"; // Private const field

	// Methods
	public void PlaceOrder(int orderId, decimal amount, string? description)
	{
		/* ... */
	}

	public string? GetOptionalTag(string key)
	{
		return Tags.TryGetValue(key, out string? value) ? value : null;
	}

	public override string ToString() => $"Person {Id}: {FirstName} {LastName}"; // Basic ToString
}

[Serializable]
public struct Address : IEquatable<Address>
{
	public string Street  { get; set; }
	public string City    { get; set; }
	public string ZipCode { get; set; }

	public readonly override string ToString() => $"{Street}, {ZipCode} {City}";

	public readonly bool Equals(Address other)
	{
		return string.Equals(Street, other.Street, StringComparison.Ordinal) &&
		       string.Equals(City, other.City, StringComparison.Ordinal) &&
		       string.Equals(ZipCode, other.ZipCode, StringComparison.Ordinal);
	}

	public readonly override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj is Address other && Equals(other);
	}

	public readonly override int GetHashCode()
	{
		unchecked // allow overflow
		{
			int hash = 17;
			hash = hash * 23 + (Street?.GetHashCode() ?? 0);
			hash = hash * 23 + (City?.GetHashCode() ?? 0);
			hash = hash * 23 + (ZipCode?.GetHashCode() ?? 0);
			return hash;
		}
	}

	public static bool operator ==(Address left, Address right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Address left, Address right)
	{
		return !(left == right);
	}
}

[Serializable]
public record Order(int OrderId, DateTime OrderDate, decimal TotalAmount);

// --------------------------------------------------------------------------------------------------------------------

public static class Program
{
	// --- Main Demo Logic ---
	public static void Main(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8; // For correct ellipsis display

		(string Name, PrettyOptions Preset)[] presets =
		[
			(Name: "Compact", Preset: PrettyPresets.Compact),
			(Name: "Standard", Preset: PrettyPresets.Standard),
			(Name: "Verbose", Preset: PrettyPresets.Verbose)
		];

		List<(string Name, object? Obj)> objectsToFormat = CreateDemoObjects();

		foreach ((string Name, PrettyOptions Preset) p in presets)
		{
			Console.WriteLine($"// ===== PRESET: {p.Name} ===== //");
			Console.WriteLine($"// Options:\n{p.Preset}"); // Show resolved options
			Console.WriteLine(new string('-', 80));

			foreach ((string Name, object? Obj) item in objectsToFormat)
			{
				Console.WriteLine($"--- Formatting:  {item.Name}");
				Console.WriteLine($"--- Description: {GetObjectDescription(item.Obj)}");
				Console.WriteLine($"--- Type:        {item.Obj?.GetType().FullName ?? "<not available>"}");
				Console.WriteLine("--- Output ↓↓↓");
				string formattedOutput;
				try
				{
					// Use the main Format overload which dispatches correctly
					formattedOutput = PrettyFormatter.Format(item.Obj, p.Preset);
				}
				catch (Exception ex)
				{
					formattedOutput = $"<!!! FORMATTER CRASHED: {ex.GetType().Name} !!!>";
				}
				Console.WriteLine(formattedOutput);
				Console.WriteLine();
			}
			Console.WriteLine();
		}

		Console.WriteLine("Demo Finished. Press Enter to exit.");
		Console.ReadLine();
	}

	// --- Helper to Create Demo Objects ---
	private static List<(string Name, object? Obj)> CreateDemoObjects()
	{
		// Custom Class
		var person = new Person
		{
			Id = 123,
			FirstName = "John",
			MiddleName = null, // Explicitly null NRT
			LastName = "Doe",
			HomeAddress = new Address { Street = "123 Main St", City = "Anytown", ZipCode = "12345" },
			Orders =
			[
				new Order(1, DateTime.UtcNow.AddDays(-10), 49.99m),
				new Order(2, DateTime.UtcNow.AddDays(-5), 105.50m),
				new Order(3, DateTime.UtcNow.AddDays(-1), 12.00m)
			],
			Tags = new Dictionary<string, string> { { "Status", "Active" }, { "Priority", "High" }, { "InternalID", Guid.NewGuid().ToString() } }
		};

		// Simple Exception
		// We throw and catch it to populate the stack trace.
		var simpleException = new InvalidOperationException("Something went wrong.");
		try { throw simpleException; }
		catch
		{
			/* swallow */
		}

		// Complex Exception
		var complexException = new AggregateException(
			"Multiple errors occurred",
			new ArgumentNullException("param1"),
			new FormatException("Invalid input format") { HelpLink = "http://example.com/errors/format" });
		complexException.Data.Add("ErrorCode", 1001);
		complexException.Data.Add("Timestamp", DateTime.UtcNow);
		complexException.Data.Add(new Address { Street = "Exception Ally", ZipCode = "12345", City = "Anytown" }, person); // Complex key/value in Data

		// List<object?>
		var list = new List<object?> { 1, "two", null, 3.14, DateTime.Now };

		// Dictionary
		var dict = new Dictionary<string, object?>
		{
			["A"] = 1,
			["B"] = "two",
			["C"] = null,
			["D"] = new Address { City = "DictCity" }
		};

		// Anonymous Type
		var anonymousType = new { Name = "Anonymous", Value = 123, Nested = new { Prop = true } };

		// ValueTuple
		(int, string?, bool?) tuple = (42, "Tuple", null); // ValueTuple with NRT

		// Reflection objects
		Type typeOfString = typeof(string);
		Type typeOfListOfInt = typeof(List<int>);
		Type typeOfPerson = typeof(Person);
		MethodInfo? placeOrderMethod = typeof(Person).GetMethod(nameof(Person.PlaceOrder));
		PropertyInfo? firstNameProperty = typeof(Person).GetProperty(nameof(Person.FirstName));
		ParameterInfo? amountParam = placeOrderMethod?.GetParameters()[1]; // The 'amount' parameter
		var currentAssembly = Assembly.GetExecutingAssembly();
		AssemblyName assemblyName = currentAssembly.GetName();

		return
		[
			("Null Object", null),
			("Simple Int", 42),
			("Simple String", "Hello, World!"),
			("Long String", new string('A', 150)),                                     // Test truncation
			("String w/ Control", "Line1\nLine2\tTabbed\\Backslash\"Quote\u202EBiDi"), // Test escaping & BiDi
			("Boolean", true),
			("DateTime", DateTime.UtcNow),
			("Guid", Guid.NewGuid()),
			("Nullable Int", (int?)null),
			("Nullable Int Val", (int?)123),
			("Simple Array", new[] { 1, 2, 3, 4, 5, 6, 7 }),
			("Byte Array", Encoding.UTF8.GetBytes("Test Bytes")),
			("2D Array", new[,] { { 1, 2 }, { 3, 4 } }),
			("List<object?>", list),
			("Dictionary", dict),
			("Anonymous Type", anonymousType),
			("ValueTuple", tuple),
			("Custom Class", person), // Includes NRT, nested class, List, Dict
			("Simple Exception", simpleException),
			("Complex Exception", complexException), // Aggregate, Data, HelpLink
			("Type (string)", typeOfString),
			("Type (List<int>)", typeOfListOfInt),
			("Type (Person)", typeOfPerson),
			("MethodInfo", placeOrderMethod),
			("PropertyInfo", firstNameProperty),
			("ParameterInfo", amountParam),
			("Assembly", currentAssembly),
			("AssemblyName", assemblyName)
		];
	}

	// --- Helper to get a simple description ---
	private static string GetObjectDescription(object? obj)
	{
		return obj switch
		{
			null                       => "null",
			string { Length: > 30 } s  => s.Substring(0, 27) + "...",
			string s                   => $"\"{s}\"",
			Type t                     => $"typeof({t.Name})",
			MethodInfo m               => $"Method: {m.Name}",
			PropertyInfo p             => $"Property: {p.Name}",
			ParameterInfo pi           => $"Parameter: {pi.Name}",
			Assembly a                 => $"Assembly: {a.GetName().Name}",
			AssemblyName an            => $"AssemblyName: {an.Name}",
			Exception e                => $"Exception: {e.GetType().Name}",
			IEnumerable and not string => $"Collection ({obj.GetType().Name})",
			var _                      => obj.ToString() ?? "<ToString() returned null>"
		};
	}
}
