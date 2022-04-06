///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Xunit;

// ReSharper disable RedundantExplicitArrayCreation

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// Unit tests targeting the <see cref="LogLevelBitMask"/> class.
	/// </summary>
	public class LogLevelBitMaskTests
	{
		#region Zeros

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.Zeros"/> property.
		/// </summary>
		[Fact]
		public void Zeros()
		{
			var mask = LogLevelBitMask.Zeros;
			Assert.Equal(0, mask.Size);
			Assert.False(mask.PaddingValue);
			Assert.Empty(mask.AsArray());
		}

		#endregion

		#region Ones

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.Ones"/> property.
		/// </summary>
		[Fact]
		public void Ones()
		{
			var mask = LogLevelBitMask.Ones;
			Assert.Equal(0, mask.Size);
			Assert.True(mask.PaddingValue);
			Assert.Empty(mask.AsArray());
		}

		#endregion

		#region Construction

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask(int,bool,bool)"/> constructor.
		/// </summary>
		/// <param name="size">Size of the bitmask to create.</param>
		/// <param name="initialBitValue">Initial value of mask bits.</param>
		/// <param name="paddingValue">Value to pad the mask with when offsetting with larger masks.</param>
		[Theory]

		// zero-length LogLevelBitMask (bit value is only defined by padding value)
		[InlineData(0, false, false)]
		[InlineData(0, false, true)]
		[InlineData(0, true, false)]
		[InlineData(0, true, true)]
		// small bit mask spanning a single uint32 value only, rounded up to 32 bit
		[InlineData(1, false, false)]
		[InlineData(1, false, true)]
		[InlineData(1, true, false)]
		[InlineData(1, true, true)]
		// small bit mask spanning a single uint32 value only, rounded up to 32 bit
		[InlineData(31, false, false)]
		[InlineData(31, false, true)]
		[InlineData(31, true, false)]
		[InlineData(31, true, true)]
		// exact size (single uint32 internally), no rounding
		[InlineData(32, false, false)]
		[InlineData(32, false, true)]
		[InlineData(32, true, false)]
		[InlineData(32, true, true)]
		// large bit mask spanning multiple uint32 values, rounded up to 128 bit
		[InlineData(97, false, false)]
		[InlineData(97, false, true)]
		[InlineData(97, true, false)]
		[InlineData(97, true, true)]
		// large bit mask spanning multiple uint32 values, rounded up to 128 bit
		[InlineData(127, false, false)]
		[InlineData(127, false, true)]
		[InlineData(127, true, false)]
		[InlineData(127, true, true)]
		// large bit mask spanning multiple uint32 values, no rounding
		[InlineData(128, false, false)]
		[InlineData(128, false, true)]
		[InlineData(128, true, false)]
		[InlineData(128, true, true)]
		public void Create(int size, bool initialBitValue, bool paddingValue)
		{
			var mask = new LogLevelBitMask(size, initialBitValue, paddingValue);

			// check the actual size of the mask in bits
			int effectiveSize = (size + 31) & ~31;
			Assert.Equal(effectiveSize, mask.Size);

			// check padding value
			Assert.Equal(paddingValue, mask.PaddingValue);

			// check underlying buffer
			uint[] maskArray = mask.AsArray();
			uint[] expectedMaskArray = new uint[effectiveSize / 32];
			for (int i = 0; i < expectedMaskArray.Length; i++)
			{
				expectedMaskArray[i] = initialBitValue ? ~0u : 0u;
			}

			Assert.Equal(expectedMaskArray, maskArray);
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask(int,bool,bool)"/> constructor passing an invalid size.
		/// </summary>
		/// <param name="size">Size of the bitmask to create.</param>
		/// <param name="initialBitValue">Initial value of mask bits.</param>
		/// <param name="paddingValue">Value to pad the mask with when offsetting with larger masks.</param>
		[Theory]
		[InlineData(-1, false, false)]
		public void Create_InvalidSize(int size, bool initialBitValue, bool paddingValue)
		{
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new LogLevelBitMask(size, initialBitValue, paddingValue));
			Assert.Equal("size", exception.ParamName);
		}

		#endregion

		#region bool operator ==(LogLevelBitMask mask1, LogLevelBitMask mask2)

		/// <summary>
		/// Test data for testing the equality of masks.
		/// </summary>
		public static IEnumerable<object[]> EqualityTestData
		{
			get
			{
				// two null references are considered equal
				yield return new object[]
				{
					false, // padding
					null,  // mask 1
					null,  // mask 2
					true   // equal
				};

				// any mask compared to a null reference is always considered different
				yield return new object[]
				{
					false,                              // padding
					"00000000000000000000000000000000", // mask 1
					null,                               // mask 2
					false                               // equal
				};

				// any mask compared to a null reference is always considered different
				yield return new object[]
				{
					false,                              // padding
					null,                               // mask 1
					"00000000000000000000000000000000", // mask 2
					false                               // equal
				};

				// same length and content
				yield return new object[]
				{
					false,                              // padding
					"00000000000000000000000000000000", // mask 1
					"00000000000000000000000000000000", // mask 2
					true                                // equal
				};

				// same length and content
				yield return new object[]
				{
					false,                              // padding
					"11111111111111111111111111111111", // mask 1
					"11111111111111111111111111111111", // mask 2
					true                                // equal
				};

				// same length and content
				yield return new object[]
				{
					false,                              // padding
					"01010101010101010101010101010101", // mask 1
					"01010101010101010101010101010101", // mask 2
					true                                // equal
				};

				// same length, different content
				yield return new object[]
				{
					false,                              // padding
					"00000000000000000000000000000000", // mask 1
					"10000000000000000000000000000000", // mask 2
					false                               // equal
				};

				// same length, different content
				yield return new object[]
				{
					false,                              // padding
					"00000000000000000000000000000000", // mask 1
					"00000000000000000000000000000001", // mask 2
					false                               // equal
				};

				// same length, different content
				yield return new object[]
				{
					false,                              // padding
					"10000000000000000000000000000000", // mask 1
					"00000000000000000000000000000000", // mask 2
					false                               // equal
				};

				// same length, different content
				yield return new object[]
				{
					false,                              // padding
					"00000000000000000000000000000001", // mask 1
					"00000000000000000000000000000000", // mask 2
					false                               // equal
				};

				// different length, equal overlapping content, padding is same as real data
				yield return new object[]
				{
					false,                                                              // padding
					"0000000000000000000000000000000000000000000000000000000000000000", // mask 1
					"00000000000000000000000000000000",                                 // mask 2 (expanded with padding 0)
					true                                                                // equal
				};

				// different length, equal overlapping content, padding is same as real data
				yield return new object[]
				{
					false,                                                              // padding
					"00000000000000000000000000000000",                                 // mask 1
					"0000000000000000000000000000000000000000000000000000000000000000", // mask 2 (expanded with padding 0)
					true                                                                // equal
				};

				// different length, equal overlapping content, padding is different from real data
				yield return new object[]
				{
					true,                                                               // padding
					"0000000000000000000000000000000000000000000000000000000000000000", // mask 1
					"00000000000000000000000000000000",                                 // mask 2 (expanded with padding 1)
					false                                                               // equal
				};

				// different length, equal overlapping content, padding is different from real data
				yield return new object[]
				{
					true,                                                               // padding
					"00000000000000000000000000000000",                                 // mask 1
					"0000000000000000000000000000000000000000000000000000000000000000", // mask 2 (expanded with padding 1)
					false                                                               // equal
				};

				// different length, different overlapping content, padding is same as real data
				yield return new object[]
				{
					false,                                                              // padding
					"1000000000000000000000000000000000000000000000000000000000000000", // mask 1
					"00000000000000000000000000000000",                                 // mask 2 (expanded with padding 0)
					false                                                               // equal
				};

				// different length, different overlapping content, padding is same as real data
				yield return new object[]
				{
					false,                                                              // padding
					"10000000000000000000000000000000",                                 // mask 1 (expanded with padding 0)
					"0000000000000000000000000000000000000000000000000000000000000000", // mask 2
					false                                                               // equal
				};
			}
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.op_Equality"/> operator.
		/// </summary>
		/// <param name="padding">Value to pad the mask with when offsetting with larger masks.</param>
		/// <param name="mask1">Mask 1 to compare.</param>
		/// <param name="mask2">Mask 1 to compare.</param>
		/// <param name="expected">
		/// <c>true</c> if both masks are expected to be equal; otherwise <c>false</c>.
		/// </param>
		[Theory]
		[MemberData(nameof(EqualityTestData))]
		public void Operator_Equality(
			bool   padding,
			string mask1,
			string mask2,
			bool   expected)
		{
			var bitmask1 = MaskFromString(mask1, false, padding);
			var bitmask2 = MaskFromString(mask2, false, padding);
			Assert.Equal(expected, bitmask1 == bitmask2);
		}

		#endregion

		#region bool operator !=(LogLevelBitMask mask1, LogLevelBitMask mask2)

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.op_Inequality"/> operator.
		/// </summary>
		/// <param name="padding">Value to pad the mask with when offsetting with larger masks.</param>
		/// <param name="mask1">Mask 1 to compare.</param>
		/// <param name="mask2">Mask 1 to compare.</param>
		/// <param name="expected">
		/// <c>true</c> if both masks are expected to be equal; otherwise <c>false</c>.
		/// </param>
		[Theory]
		[MemberData(nameof(EqualityTestData))]
		public void Operator_Inequality(
			bool   padding,
			string mask1,
			string mask2,
			bool   expected)
		{
			var bitmask1 = MaskFromString(mask1, false, padding);
			var bitmask2 = MaskFromString(mask2, false, padding);
			Assert.Equal(!expected, bitmask1 != bitmask2);
		}

		#endregion

		#region LogLevelBitMask operator ~(LogLevelBitMask mask)

		/// <summary>
		/// Test data for testing the ones complement of the mask.
		/// </summary>
		public static IEnumerable<object[]> OnesComplementOperatorTestData
		{
			get
			{
				yield return new object[]
				{
					"00000000000000000000000000000000",
					"11111111111111111111111111111111"
				};

				yield return new object[]
				{
					"11111111111111111111111111111111",
					"00000000000000000000000000000000"
				};

				yield return new object[]
				{
					"10000000000000000000000000000001",
					"01111111111111111111111111111110"
				};

				yield return new object[]
				{
					"01010101010101010101010101010101",
					"10101010101010101010101010101010"
				};

				yield return new object[]
				{
					"0101010101010101010101010101010101010101010101010101010101010101",
					"1010101010101010101010101010101010101010101010101010101010101010"
				};
			}
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.op_OnesComplement"/> operator.
		/// </summary>
		/// <param name="mask">Mask to test with.</param>
		/// <param name="expected">The expected mask.</param>
		[Theory]
		[MemberData(nameof(OnesComplementOperatorTestData))]
		public void Operator_OnesComplement(
			string mask,
			string expected)
		{
			// create the bitmask to test with
			var bitmask = MaskFromString(mask, false, false);

			// calculate the complement of the bitmask
			var complement = ~bitmask;

			// check resulting bit mask
			string bitmaskAsString = "";
			for (int i = 0; i < complement.Size; i++) bitmaskAsString += complement.IsBitSet(i) ? "1" : "0";
			Assert.Equal(expected, bitmaskAsString);
		}

		#endregion

		#region LogLevelBitMask operator |(LogLevelBitMask mask1, LogLevelBitMask mask2)

		/// <summary>
		/// Test data for testing the bitwise OR of two masks.
		/// </summary>
		public static IEnumerable<object[]> Operator_BitwiseOrTestData
		{
			get
			{
				yield return new object[]
				{
					false,                              // padding
					"00000000000000000000000000000000", // mask 1
					"00000000000000000000000000000000", // mask 2
					"00000000000000000000000000000000"  // resulting mask
				};

				yield return new object[]
				{
					false,                              // padding
					"10000000000000000000000000000000", // mask 1
					"00000000000000000000000000000000", // mask 2
					"10000000000000000000000000000000"  // resulting mask
				};

				yield return new object[]
				{
					false,                              // padding
					"00000000000000000000000000000000", // mask 1
					"10000000000000000000000000000000", // mask 2
					"10000000000000000000000000000000"  // resulting mask
				};

				yield return new object[]
				{
					false,                              // padding
					"11110000000000000000000000001111", // mask 1
					"10000000111100000000111100000000", // mask 2
					"11110000111100000000111100001111"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					false,                                                              // padding
					"1111111111111111000000000000000010101010101010101010101010101010", // mask 1
					"00000000000000001111111111111111",                                 // mask 2 (expanded with padding 0)
					"1111111111111111111111111111111110101010101010101010101010101010"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					false,                                                              // padding
					"00000000000000001111111111111111",                                 // mask 1 (expanded with padding 0)
					"1111111111111111000000000000000010101010101010101010101010101010", // mask 2
					"1111111111111111111111111111111110101010101010101010101010101010"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					true,                                                               // padding
					"1111111111110000000000000000000010101010101010101010101010101010", // mask 1 
					"00000000000000000000111111111111",                                 // mask 2 (expanded with padding 1)
					"1111111111110000000011111111111111111111111111111111111111111111"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					true,                                                               // padding
					"00000000000000000000111111111111",                                 // mask 1 (expanded with padding 1)
					"1111111111110000000000000000000010101010101010101010101010101010", // mask 2
					"1111111111110000000011111111111111111111111111111111111111111111"  // resulting mask
				};
			}
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.op_BitwiseOr"/> operator.
		/// </summary>
		/// <param name="padding">
		/// <c>true</c> to pad shorter masks with '1';
		/// <c>false</c> to pad them with '0'.
		/// </param>
		/// <param name="mask1">Mask 1 to test with.</param>
		/// <param name="mask2">Mask 2 to test with.</param>
		/// <param name="expected">The expected mask.</param>
		[Theory]
		[MemberData(nameof(Operator_BitwiseOrTestData))]
		public void Operator_BitwiseOr(
			bool   padding,
			string mask1,
			string mask2,
			string expected)
		{
			// prepare masks
			var bitmask1 = MaskFromString(mask1, false, padding);
			var bitmask2 = MaskFromString(mask2, false, padding);

			// calculate the bitwise OR of the masks
			var resultingBitmask = bitmask1 | bitmask2;

			// check the resulting mask
			string resultingBitmaskAsString = "";
			for (int i = 0; i < resultingBitmask.Size; i++) resultingBitmaskAsString += resultingBitmask.IsBitSet(i) ? "1" : "0";
			Assert.Equal(expected, resultingBitmaskAsString);
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.op_BitwiseOr"/> operator passing <c>null</c>.
		/// The operator should throw an <see cref="ArgumentNullException"/> in this case.
		/// </summary>
		[Fact]
		[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
		public void Operator_BitwiseOr_ArgumentNull()
		{
			LogLevelBitMask bitmask1 = new LogLevelBitMask(32, false, false);
			LogLevelBitMask bitmask2 = null;
			var exception1 = Assert.Throws<ArgumentNullException>(() => bitmask1 | bitmask2);
			Assert.Equal("mask2", exception1.ParamName);
			var exception2 = Assert.Throws<ArgumentNullException>(() => bitmask2 | bitmask1);
			Assert.Equal("mask1", exception2.ParamName);
		}

		#endregion

		#region LogLevelBitMask operator &(LogLevelBitMask mask1, LogLevelBitMask mask2)

		/// <summary>
		/// Test data for testing the bitwise AND of two masks.
		/// </summary>
		public static IEnumerable<object[]> Operator_BitwiseAndTestData
		{
			get
			{
				yield return new object[]
				{
					false,                              // padding
					"00000000000000000000000000000000", // mask 1
					"00000000000000000000000000000000", // mask 2
					"00000000000000000000000000000000"  // resulting mask
				};

				yield return new object[]
				{
					false,                              // padding
					"11111111111111111111111111111111", // mask 1
					"11111111111111111111111111111111", // mask 2
					"11111111111111111111111111111111"  // resulting mask
				};

				yield return new object[]
				{
					false,                              // padding
					"10101010101010101010101010101010", // mask 1
					"01010101010101010101010101010101", // mask 2
					"00000000000000000000000000000000"  // resulting mask
				};

				yield return new object[]
				{
					false,                              // padding
					"10101010101010101010101010101010", // mask 1
					"10101010101010101010101010101010", // mask 2
					"10101010101010101010101010101010"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					false,                                                              // padding
					"1111000011110000111100001111000001010101010101010101010101010101", // mask 1
					"11111111111111111111111111111111",                                 // mask 2 (expanded with padding 0)
					"1111000011110000111100001111000000000000000000000000000000000000"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					false,                                                              // padding
					"11111111111111111111111111111111",                                 // mask 1 (expanded with padding 0)
					"1111000011110000111100001111000001010101010101010101010101010101", // mask 2
					"1111000011110000111100001111000000000000000000000000000000000000"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					true,                                                               // padding
					"1111000011110000111100001111000001010101010101010101010101010101", // mask 1
					"11111111111111111111111111111111",                                 // mask 2 (expanded with padding 1)
					"1111000011110000111100001111000001010101010101010101010101010101"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					true,                                                               // padding
					"11111111111111111111111111111111",                                 // mask 1 (expanded with padding 1)
					"1111000011110000111100001111000001010101010101010101010101010101", // mask 2
					"1111000011110000111100001111000001010101010101010101010101010101"  // resulting mask
				};
			}
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.op_BitwiseAnd"/> operator.
		/// </summary>
		/// <param name="padding">
		/// <c>true</c> to pad shorter masks with '1';
		/// <c>false</c> to pad them with '0'.
		/// </param>
		/// <param name="mask1">Mask 1 to test with.</param>
		/// <param name="mask2">Mask 2 to test with.</param>
		/// <param name="expected">The expected mask.</param>
		[Theory]
		[MemberData(nameof(Operator_BitwiseAndTestData))]
		public void Operator_BitwiseAnd(
			bool   padding,
			string mask1,
			string mask2,
			string expected)
		{
			// prepare masks
			var bitmask1 = MaskFromString(mask1, false, padding);
			var bitmask2 = MaskFromString(mask2, false, padding);

			// calculate the bitwise AND of the masks
			var resultingBitmask = bitmask1 & bitmask2;

			// check the resulting mask
			string resultingBitmaskAsString = "";
			for (int i = 0; i < resultingBitmask.Size; i++) resultingBitmaskAsString += resultingBitmask.IsBitSet(i) ? "1" : "0";
			Assert.Equal(expected, resultingBitmaskAsString);
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.op_BitwiseAnd"/> operator passing <c>null</c>.
		/// The operator should throw an <see cref="ArgumentNullException"/> in this case.
		/// </summary>
		[Fact]
		[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
		public void Operator_BitwiseAnd_ArgumentNull()
		{
			LogLevelBitMask bitmask1 = new LogLevelBitMask(32, false, false);
			LogLevelBitMask bitmask2 = null;
			var exception1 = Assert.Throws<ArgumentNullException>(() => bitmask1 & bitmask2);
			Assert.Equal("mask2", exception1.ParamName);
			var exception2 = Assert.Throws<ArgumentNullException>(() => bitmask2 & bitmask1);
			Assert.Equal("mask1", exception2.ParamName);
		}

		#endregion

		#region LogLevelBitMask operator ^(LogLevelBitMask mask1, LogLevelBitMask mask2)

		/// <summary>
		/// Test data for testing the bitwise XOR of two masks.
		/// </summary>
		public static IEnumerable<object[]> Operator_BitwiseXorTestData
		{
			get
			{
				yield return new object[]
				{
					false,                              // padding
					"00000000000000000000000000000000", // mask 1
					"00000000000000000000000000000000", // mask 2
					"00000000000000000000000000000000"  // resulting mask
				};

				yield return new object[]
				{
					false,                              // padding
					"11111111111111111111111111111111", // mask 1
					"11111111111111111111111111111111", // mask 2
					"00000000000000000000000000000000"  // resulting mask
				};

				yield return new object[]
				{
					false,                              // padding
					"10101010101010101010101010101010", // mask 1
					"01010101010101010101010101010101", // mask 2
					"11111111111111111111111111111111"  // resulting mask
				};

				yield return new object[]
				{
					false,                              // padding
					"10101010101010101010101010101010", // mask 1
					"10101010101010101010101010101010", // mask 2
					"00000000000000000000000000000000"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					false,                                                              // padding
					"1111000011110000111100001111000001010101010101010101010101010101", // mask 1
					"11111111111111111111111111111111",                                 // mask 2 (expanded with padding 0)
					"0000111100001111000011110000111101010101010101010101010101010101"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					false,                                                              // padding
					"11111111111111111111111111111111",                                 // mask 1 (expanded with padding 0)
					"1111000011110000111100001111000001010101010101010101010101010101", // mask 2
					"0000111100001111000011110000111101010101010101010101010101010101"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					true,                                                               // padding
					"1111000011110000111100001111000001010101010101010101010101010101", // mask 1
					"11111111111111111111111111111111",                                 // mask 2 (expanded with padding 1)
					"0000111100001111000011110000111110101010101010101010101010101010"  // resulting mask
				};

				// masks of different size, larger mask should be returned
				yield return new object[]
				{
					true,                                                               // padding
					"11111111111111111111111111111111",                                 // mask 1 (expanded with padding 1)
					"1111000011110000111100001111000001010101010101010101010101010101", // mask 2
					"0000111100001111000011110000111110101010101010101010101010101010"  // resulting mask
				};
			}
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.op_ExclusiveOr "/> operator.
		/// </summary>
		/// <param name="padding">
		/// <c>true</c> to pad shorter masks with '1';
		/// <c>false</c> to pad them with '0'.
		/// </param>
		/// <param name="mask1">Mask 1 to test with.</param>
		/// <param name="mask2">Mask 2 to test with.</param>
		/// <param name="expected">The expected mask.</param>
		[Theory]
		[MemberData(nameof(Operator_BitwiseXorTestData))]
		public void Operator_BitwiseXor(
			bool   padding,
			string mask1,
			string mask2,
			string expected)
		{
			// prepare masks
			var bitmask1 = MaskFromString(mask1, false, padding);
			var bitmask2 = MaskFromString(mask2, false, padding);

			// calculate the bitwise XOR of the masks
			var resultingBitmask = bitmask1 ^ bitmask2;

			// check the resulting mask
			string resultingBitmaskAsString = "";
			for (int i = 0; i < resultingBitmask.Size; i++) resultingBitmaskAsString += resultingBitmask.IsBitSet(i) ? "1" : "0";
			Assert.Equal(expected, resultingBitmaskAsString);
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.op_ExclusiveOr"/> operator passing <c>null</c>.
		/// The operator should throw an <see cref="ArgumentNullException"/> in this case.
		/// </summary>
		[Fact]
		[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
		public void Operator_BitwiseXor_ArgumentNull()
		{
			LogLevelBitMask bitmask1 = new LogLevelBitMask(32, false, false);
			LogLevelBitMask bitmask2 = null;
			var exception1 = Assert.Throws<ArgumentNullException>(() => bitmask1 ^ bitmask2);
			Assert.Equal("mask2", exception1.ParamName);
			var exception2 = Assert.Throws<ArgumentNullException>(() => bitmask2 ^ bitmask1);
			Assert.Equal("mask1", exception2.ParamName);
		}

		#endregion

		#region void SetBit(int bit)

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.SetBit"/> method.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="bit">Bit to set.</param>
		[Theory]
		// small bit mask
		[InlineData(32, 0)]
		[InlineData(32, 15)]
		[InlineData(32, 31)]
		// large bit mask
		[InlineData(128, 0)]
		[InlineData(128, 15)]
		[InlineData(128, 31)]
		public void SetBit(int size, int bit)
		{
			var mask = new LogLevelBitMask(size, false, false);

			// check the actual size of the mask in bits
			int effectiveSize = (size + 31) & ~31;
			Assert.Equal(effectiveSize, mask.Size);

			// clear bit
			mask.SetBit(bit);

			// check underlying buffer
			uint[] maskArray = mask.AsArray();
			int setBitArrayIndex = bit / 32;
			int setBitIndex = bit % 32;
			uint[] expectedMaskArray = new uint[effectiveSize / 32];
			for (int i = 0; i < expectedMaskArray.Length; i++)
			{
				if (i == setBitArrayIndex)
				{
					expectedMaskArray[i] = 0u | (1u << setBitIndex);
				}
				else
				{
					expectedMaskArray[i] = 0u;
				}
			}

			Assert.Equal(expectedMaskArray, maskArray);
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.SetBit"/> method passing a bit that is out of range.
		/// The method should throw an <see cref="ArgumentOutOfRangeException"/> in this case.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="bit">Bit to set.</param>
		[Theory]
		// small bit mask
		[InlineData(32, -1)]
		[InlineData(32, 32)]
		// large bit mask
		[InlineData(128, -1)]
		[InlineData(128, 128)]
		public void SetBit_BitOutOfRange(int size, int bit)
		{
			var mask = new LogLevelBitMask(size, false, false);
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => mask.SetBit(bit));
			Assert.Equal("bit", exception.ParamName);
		}

		#endregion

		#region void ClearBit(int bit)

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.ClearBit"/> method.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="bit">Bit to clear.</param>
		[Theory]
		// small bit mask
		[InlineData(32, 0)]
		[InlineData(32, 15)]
		[InlineData(32, 31)]
		// large bit mask
		[InlineData(128, 0)]
		[InlineData(128, 15)]
		[InlineData(128, 31)]
		public void ClearBit(int size, int bit)
		{
			var mask = new LogLevelBitMask(size, true, false);

			// check the actual size of the mask in bits
			int effectiveSize = (size + 31) & ~31;
			Assert.Equal(effectiveSize, mask.Size);

			// clear bit
			mask.ClearBit(bit);

			// check underlying buffer
			uint[] maskArray = mask.AsArray();
			int clearedBitArrayIndex = bit / 32;
			int clearedBitIndex = bit % 32;
			uint[] expectedMaskArray = new uint[effectiveSize / 32];
			for (int i = 0; i < expectedMaskArray.Length; i++)
			{
				if (i == clearedBitArrayIndex)
				{
					expectedMaskArray[i] = ~0u & ~(1u << clearedBitIndex);
				}
				else
				{
					expectedMaskArray[i] = ~0u;
				}
			}

			Assert.Equal(expectedMaskArray, maskArray);
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.ClearBit"/> method passing a bit that is out of range.
		/// The method should throw an <see cref="ArgumentOutOfRangeException"/> in this case.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="bit">Bit to clear.</param>
		[Theory]
		// small bit mask
		[InlineData(32, -1)]
		[InlineData(32, 32)]
		// large bit mask
		[InlineData(128, -1)]
		[InlineData(128, 128)]
		public void ClearBit_BitOutOfRange(int size, int bit)
		{
			var mask = new LogLevelBitMask(size, false, false);
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => mask.ClearBit(bit));
			Assert.Equal("bit", exception.ParamName);
		}

		#endregion

		#region void SetBits(int bit, int count)

		/// <summary>
		/// Tests the <see cref="SetBits"/> method.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="fromBit">Bit to start setting at.</param>
		/// <param name="count">Number of bits to set.</param>
		/// <param name="expectedMaskArray">The expected mask array.</param>
		[Theory]
		// small bit mask
		[InlineData(32, 0, 0, new uint[] { 0x00000000u })]
		[InlineData(32, 0, 1, new uint[] { 0x00000001u })]
		[InlineData(32, 0, 2, new uint[] { 0x00000003u })]
		[InlineData(32, 30, 0, new uint[] { 0x00000000u })]
		[InlineData(32, 30, 1, new uint[] { 0x40000000u })]
		[InlineData(32, 30, 2, new uint[] { 0xC0000000u })]
		[InlineData(32, 1, 30, new uint[] { 0x7FFFFFFEu })] // all bits except the first and the last one
		// large bit mask
		[InlineData(128, 0, 0, new uint[] { 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u })]
		[InlineData(128, 0, 1, new uint[] { 0x00000001u, 0x00000000u, 0x00000000u, 0x00000000u })]
		[InlineData(128, 0, 2, new uint[] { 0x00000003u, 0x00000000u, 0x00000000u, 0x00000000u })]
		[InlineData(128, 30, 0, new uint[] { 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u })]
		[InlineData(128, 30, 1, new uint[] { 0x40000000u, 0x00000000u, 0x00000000u, 0x00000000u })]
		[InlineData(128, 30, 2, new uint[] { 0xC0000000u, 0x00000000u, 0x00000000u, 0x00000000u })]
		[InlineData(128, 30, 3, new uint[] { 0xC0000000u, 0x00000001u, 0x00000000u, 0x00000000u })] // spans sections
		[InlineData(128, 30, 4, new uint[] { 0xC0000000u, 0x00000003u, 0x00000000u, 0x00000000u })] // spans sections
		[InlineData(128, 95, 0, new uint[] { 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u })]
		[InlineData(128, 95, 1, new uint[] { 0x00000000u, 0x00000000u, 0x80000000u, 0x00000000u })]
		[InlineData(128, 95, 2, new uint[] { 0x00000000u, 0x00000000u, 0x80000000u, 0x00000001u })] // spans sections
		[InlineData(128, 126, 0, new uint[] { 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u })]
		[InlineData(128, 126, 1, new uint[] { 0x00000000u, 0x00000000u, 0x00000000u, 0x40000000u })]
		[InlineData(128, 126, 2, new uint[] { 0x00000000u, 0x00000000u, 0x00000000u, 0xC0000000u })]
		[InlineData(128, 1, 126, new uint[] { 0xFFFFFFFEu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x7FFFFFFFu })] // all bits except the first and the last one
		public void SetBits(
			int    size,
			int    fromBit,
			int    count,
			uint[] expectedMaskArray)
		{
			var mask = new LogLevelBitMask(size, false, false);

			// check the actual size of the mask in bits
			int effectiveSize = (size + 31) & ~31;
			Assert.Equal(effectiveSize, mask.Size);

			// clear bit
			mask.SetBits(fromBit, count);

			// check underlying buffer
			uint[] maskArray = mask.AsArray();
			Assert.Equal(expectedMaskArray, maskArray);
		}

		/// <summary>
		/// Tests the <see cref="SetBits"/> method passing argument that are out of bounds.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="fromBit">Bit to start setting at.</param>
		/// <param name="count">Number of bits to set.</param>
		/// <param name="expectedParameterName">Name of the parameter that is out of bounds.</param>
		[Theory]
		// small bit mask
		[InlineData(32, -1, 0, "bit")]   // bit < 0
		[InlineData(32, 0, -1, "count")] // count < 0
		[InlineData(32, 0, 33, "count")] // end bit (and therefore count) is out of bounds
		[InlineData(32, 32, 1, "bit")]   // start bit is out of bounds
		// large bit mask
		[InlineData(128, -1, 0, "bit")]    // bit < 0
		[InlineData(128, 0, -1, "count")]  // count < 0
		[InlineData(128, 0, 129, "count")] // end bit (and therefore count) is out of bounds
		[InlineData(128, 128, 1, "bit")]   // start bit is out of bounds
		public void SetBits_OutOfBounds(
			int    size,
			int    fromBit,
			int    count,
			string expectedParameterName)
		{
			var mask = new LogLevelBitMask(size, true, false);
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => mask.SetBits(fromBit, count));
			Assert.Equal(expectedParameterName, exception.ParamName);
		}

		#endregion

		#region void ClearBits(int bit, int count)

		/// <summary>
		/// Tests the <see cref="ClearBits"/> method.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="fromBit">Bit to start clearing at.</param>
		/// <param name="count">Number of bits to clear.</param>
		/// <param name="expectedMaskArray">The expected mask array.</param>
		[Theory]
		// small bit mask
		[InlineData(32, 0, 0, new uint[] { 0xFFFFFFFFu })]
		[InlineData(32, 0, 1, new uint[] { 0xFFFFFFFEu })]
		[InlineData(32, 0, 2, new uint[] { 0xFFFFFFFCu })]
		[InlineData(32, 30, 0, new uint[] { 0xFFFFFFFFu })]
		[InlineData(32, 30, 1, new uint[] { 0xBFFFFFFFu })]
		[InlineData(32, 30, 2, new uint[] { 0x3FFFFFFFu })]
		[InlineData(32, 1, 30, new uint[] { 0x80000001u })] // all bits except the first and the last one
		// large bit mask
		[InlineData(128, 0, 0, new uint[] { 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu })]
		[InlineData(128, 0, 1, new uint[] { 0xFFFFFFFEu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu })]
		[InlineData(128, 0, 2, new uint[] { 0xFFFFFFFCu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu })]
		[InlineData(128, 30, 0, new uint[] { 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu })]
		[InlineData(128, 30, 1, new uint[] { 0xBFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu })]
		[InlineData(128, 30, 2, new uint[] { 0x3FFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu })]
		[InlineData(128, 30, 3, new uint[] { 0x3FFFFFFFu, 0xFFFFFFFEu, 0xFFFFFFFFu, 0xFFFFFFFFu })] // spans sections
		[InlineData(128, 30, 4, new uint[] { 0x3FFFFFFFu, 0xFFFFFFFCu, 0xFFFFFFFFu, 0xFFFFFFFFu })] // spans sections
		[InlineData(128, 95, 0, new uint[] { 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu })]
		[InlineData(128, 95, 1, new uint[] { 0xFFFFFFFFu, 0xFFFFFFFFu, 0x7FFFFFFFu, 0xFFFFFFFFu })]
		[InlineData(128, 95, 2, new uint[] { 0xFFFFFFFFu, 0xFFFFFFFFu, 0x7FFFFFFFu, 0xFFFFFFFEu })] // spans sections
		[InlineData(128, 126, 0, new uint[] { 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu })]
		[InlineData(128, 126, 1, new uint[] { 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xBFFFFFFFu })]
		[InlineData(128, 126, 2, new uint[] { 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x3FFFFFFFu })]
		[InlineData(128, 1, 126, new uint[] { 0x00000001u, 0x00000000u, 0x00000000u, 0x80000000u })] // all bits except the first and the last one
		public void ClearBits(
			int    size,
			int    fromBit,
			int    count,
			uint[] expectedMaskArray)
		{
			var mask = new LogLevelBitMask(size, true, false);

			// check the actual size of the mask in bits
			int effectiveSize = (size + 31) & ~31;
			Assert.Equal(effectiveSize, mask.Size);

			// clear bit
			mask.ClearBits(fromBit, count);

			// check underlying buffer
			uint[] maskArray = mask.AsArray();
			Assert.Equal(expectedMaskArray, maskArray);
		}

		/// <summary>
		/// Tests the <see cref="ClearBits"/> method passing argument that are out of bounds.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="fromBit">Bit to start clearing at.</param>
		/// <param name="count">Number of bits to clear.</param>
		/// <param name="expectedParameterName">Name of the parameter that is out of bounds.</param>
		[Theory]
		// small bit mask
		[InlineData(32, -1, 0, "bit")]   // bit < 0
		[InlineData(32, 0, -1, "count")] // count < 0
		[InlineData(32, 0, 33, "count")] // end bit (and therefore count) is out of bounds
		[InlineData(32, 32, 1, "bit")]   // start bit is out of bounds
		// large bit mask
		[InlineData(128, -1, 0, "bit")]    // bit < 0
		[InlineData(128, 0, -1, "count")]  // count < 0
		[InlineData(128, 0, 129, "count")] // end bit (and therefore count) is out of bounds
		[InlineData(128, 128, 1, "bit")]   // start bit is out of bounds
		public void ClearBits_OutOfBounds(
			int    size,
			int    fromBit,
			int    count,
			string expectedParameterName)
		{
			var mask = new LogLevelBitMask(size, true, false);
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => mask.ClearBits(fromBit, count));
			Assert.Equal(expectedParameterName, exception.ParamName);
		}

		#endregion

		#region bool IsBitSet(int bit)

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.IsBitSet"/> method.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="bit">Bit to test.</param>
		[Theory]
		// small bit mask
		[InlineData(32, 0)]
		[InlineData(32, 1)]
		[InlineData(32, 30)]
		[InlineData(32, 31)]
		// large bit mask
		[InlineData(128, 0)]
		[InlineData(128, 1)]
		[InlineData(128, 126)]
		[InlineData(128, 127)]
		public void IsBitSet(int size, int bit)
		{
			var mask = new LogLevelBitMask(size, false, true);
			Assert.False(mask.IsBitSet(bit));
			mask.SetBit(bit);
			Assert.True(mask.IsBitSet(bit));
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.IsBitSet"/> method with a bit that is out of bounds.
		/// The method should throw an <see cref="ArgumentOutOfRangeException"/> in this case.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="bit">Bit to test.</param>
		[Theory]
		// small bit mask
		[InlineData(32, -1)]
		// large bit mask
		[InlineData(128, -1)]
		public void IsBitSet_OutOfBounds(int size, int bit)
		{
			var mask = new LogLevelBitMask(size, true, false);
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => mask.IsBitSet(bit));
			Assert.Equal("bit", exception.ParamName);
		}

		#endregion

		#region bool IsBitCleared(int bit)

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.IsBitCleared"/> method.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="bit">Bit to test.</param>
		[Theory]
		// small bit mask
		[InlineData(32, 0)]
		[InlineData(32, 1)]
		[InlineData(32, 30)]
		[InlineData(32, 31)]
		// large bit mask
		[InlineData(128, 0)]
		[InlineData(128, 1)]
		[InlineData(128, 126)]
		[InlineData(128, 127)]
		public void IsBitCleared(int size, int bit)
		{
			var mask = new LogLevelBitMask(size, true, false);
			Assert.False(mask.IsBitCleared(bit));
			mask.ClearBit(bit);
			Assert.True(mask.IsBitCleared(bit));
		}

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.IsBitCleared"/> method with a bit that is out of bounds.
		/// The method should throw an <see cref="ArgumentOutOfRangeException"/> in this case.
		/// </summary>
		/// <param name="size">Size of the mask to test with.</param>
		/// <param name="bit">Bit to test.</param>
		[Theory]
		// small bit mask
		[InlineData(32, -1)]
		// large bit mask
		[InlineData(128, -1)]
		public void IsBitCleared_OutOfBounds(int size, int bit)
		{
			var mask = new LogLevelBitMask(size, true, false);
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => mask.IsBitCleared(bit));
			Assert.Equal("bit", exception.ParamName);
		}

		#endregion

		#region int GetHashCode()

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.GetHashCode"/> method.
		/// </summary>
		/// <param name="mask">Mask to test with.</param>
		/// <param name="expected">The expected hash code.</param>
		[Theory]
		[InlineData("00000000000000000000000000000000", 12704)]
		[InlineData("00000000000000000000000000000001", -2147470944)]
		[InlineData("00000000000000000000000000000010", 1073754528)]
		[InlineData("00000000000000000000000000000100", 536883616)]
		[InlineData("00000000000000000000000000001000", 268448160)]
		[InlineData("00000000000000000000000000010000", 134230432)]
		[InlineData("00000000000000000000000000100000", 67121568)]
		[InlineData("00000000000000000000000001000000", 33567136)]
		[InlineData("00000000000000000000000010000000", 16789920)]
		[InlineData("00000000000000000000000100000000", 8401312)]
		[InlineData("00000000000000000000001000000000", 4207008)]
		[InlineData("00000000000000000000010000000000", 2109856)]
		[InlineData("00000000000000000000100000000000", 1061280)]
		[InlineData("00000000000000000001000000000000", 536992)]
		[InlineData("00000000000000000010000000000000", 274848)]
		[InlineData("00000000000000000100000000000000", 143776)]
		[InlineData("00000000000000001000000000000000", 78240)]
		[InlineData("00000000000000010000000000000000", 45472)]
		[InlineData("00000000000000100000000000000000", 29088)]
		[InlineData("00000000000001000000000000000000", 4512)]
		[InlineData("00000000000010000000000000000000", 8608)]
		[InlineData("00000000000100000000000000000000", 14752)]
		[InlineData("00000000001000000000000000000000", 13728)]
		[InlineData("00000000010000000000000000000000", 13216)]
		[InlineData("00000000100000000000000000000000", 12448)]
		[InlineData("00000001000000000000000000000000", 12576)]
		[InlineData("00000010000000000000000000000000", 12768)]
		[InlineData("00000100000000000000000000000000", 12672)]
		[InlineData("00001000000000000000000000000000", 12720)]
		[InlineData("00010000000000000000000000000000", 12712)]
		[InlineData("00100000000000000000000000000000", 12708)]
		[InlineData("01000000000000000000000000000000", 12706)]
		[InlineData("10000000000000000000000000000000", 12705)]
		[InlineData("11111111111111111111111111111111", -12705)]
		[InlineData("0000000000000000000000000000000000000000000000000000000000000000", 10086976)]
		[InlineData("0000000000000000000000000000000000000000000000000000000000000001", -2137396672)]
		[InlineData("0000000000000000000000000000000000000000000000000000000000000010", 1083828800)]
		[InlineData("0000000000000000000000000000000000000000000000000000000000000100", 546957888)]
		[InlineData("0000000000000000000000000000000000000000000000000000000000001000", 278522432)]
		[InlineData("0000000000000000000000000000000000000000000000000000000000010000", 144304704)]
		[InlineData("0000000000000000000000000000000000000000000000000000000000100000", 77195840)]
		[InlineData("0000000000000000000000000000000000000000000000000000000001000000", 43641408)]
		[InlineData("0000000000000000000000000000000000000000000000000000000010000000", 26864192)]
		[InlineData("0000000000000000000000000000000000000000000000000000000100000000", 1698368)]
		[InlineData("0000000000000000000000000000000000000000000000000000001000000000", 14281280)]
		[InlineData("0000000000000000000000000000000000000000000000000000010000000000", 12184128)]
		[InlineData("0000000000000000000000000000000000000000000000000000100000000000", 9038400)]
		[InlineData("0000000000000000000000000000000000000000000000000001000000000000", 9562688)]
		[InlineData("0000000000000000000000000000000000000000000000000010000000000000", 10349120)]
		[InlineData("0000000000000000000000000000000000000000000000000100000000000000", 10218048)]
		[InlineData("0000000000000000000000000000000000000000000000001000000000000000", 10021440)]
		[InlineData("0000000000000000000000000000000000000000000000010000000000000000", 10054208)]
		[InlineData("0000000000000000000000000000000000000000000000100000000000000000", 10070592)]
		[InlineData("0000000000000000000000000000000000000000000001000000000000000000", 10078784)]
		[InlineData("0000000000000000000000000000000000000000000010000000000000000000", 10091072)]
		[InlineData("0000000000000000000000000000000000000000000100000000000000000000", 10084928)]
		[InlineData("0000000000000000000000000000000000000000001000000000000000000000", 10088000)]
		[InlineData("0000000000000000000000000000000000000000010000000000000000000000", 10086464)]
		[InlineData("0000000000000000000000000000000000000000100000000000000000000000", 10087232)]
		[InlineData("0000000000000000000000000000000000000001000000000000000000000000", 10087104)]
		[InlineData("0000000000000000000000000000000000000010000000000000000000000000", 10086912)]
		[InlineData("0000000000000000000000000000000000000100000000000000000000000000", 10087008)]
		[InlineData("0000000000000000000000000000000000001000000000000000000000000000", 10086992)]
		[InlineData("0000000000000000000000000000000000010000000000000000000000000000", 10086984)]
		[InlineData("0000000000000000000000000000000000100000000000000000000000000000", 10086980)]
		[InlineData("0000000000000000000000000000000001000000000000000000000000000000", 10086978)]
		[InlineData("0000000000000000000000000000000010000000000000000000000000000000", 10086977)]
		[InlineData("0000000000000000000000000000000100000000000000000000000000000000", -2137396672)]
		[InlineData("0000000000000000000000000000001000000000000000000000000000000000", 1083828800)]
		[InlineData("0000000000000000000000000000010000000000000000000000000000000000", -1600525760)]
		[InlineData("0000000000000000000000000000100000000000000000000000000000000000", -795219392)]
		[InlineData("0000000000000000000000000001000000000000000000000000000000000000", 1754917440)]
		[InlineData("0000000000000000000000000010000000000000000000000000000000000000", 882502208)]
		[InlineData("0000000000000000000000000100000000000000000000000000000000000000", 446294592)]
		[InlineData("0000000000000000000000001000000000000000000000000000000000000000", -1919292864)]
		[InlineData("0000000000000000000000010000000000000000000000000000000000000000", -954602944)]
		[InlineData("0000000000000000000000100000000000000000000000000000000000000000", 1675225664)]
		[InlineData("0000000000000000000001000000000000000000000000000000000000000000", 842656320)]
		[InlineData("0000000000000000000010000000000000000000000000000000000000000000", 426371648)]
		[InlineData("0000000000000000000100000000000000000000000000000000000000000000", 218229312)]
		[InlineData("0000000000000000001000000000000000000000000000000000000000000000", 114158144)]
		[InlineData("0000000000000000010000000000000000000000000000000000000000000000", 62122560)]
		[InlineData("0000000000000000100000000000000000000000000000000000000000000000", 36104768)]
		[InlineData("0000000000000001000000000000000000000000000000000000000000000000", 23095872)]
		[InlineData("0000000000000010000000000000000000000000000000000000000000000000", 3582528)]
		[InlineData("0000000000000100000000000000000000000000000000000000000000000000", 6834752)]
		[InlineData("0000000000001000000000000000000000000000000000000000000000000000", 11713088)]
		[InlineData("0000000000010000000000000000000000000000000000000000000000000000", 10900032)]
		[InlineData("0000000000100000000000000000000000000000000000000000000000000000", 10493504)]
		[InlineData("0000000001000000000000000000000000000000000000000000000000000000", 9883712)]
		[InlineData("0000000010000000000000000000000000000000000000000000000000000000", 9985344)]
		[InlineData("0000000100000000000000000000000000000000000000000000000000000000", 10137792)]
		[InlineData("0000001000000000000000000000000000000000000000000000000000000000", 10061568)]
		[InlineData("0000010000000000000000000000000000000000000000000000000000000000", 10099680)]
		[InlineData("0000100000000000000000000000000000000000000000000000000000000000", 10093328)]
		[InlineData("0001000000000000000000000000000000000000000000000000000000000000", 10090152)]
		[InlineData("0010000000000000000000000000000000000000000000000000000000000000", 10088564)]
		[InlineData("0100000000000000000000000000000000000000000000000000000000000000", 10087770)]
		[InlineData("1000000000000000000000000000000000000000000000000000000000000000", 10087373)]
		[InlineData("1111111111111111111111111111111111111111111111111111111111111111", 10087372)]
#pragma warning disable xUnit1024 // Test methods cannot have overloads
		public void GetHashCode(string mask, int expected)
#pragma warning restore xUnit1024 // Test methods cannot have overloads
		{
			var bitmask = MaskFromString(mask);
			Assert.Equal(expected, bitmask.GetHashCode());
		}

		#endregion

		#region bool Equals(object obj)

		/// <summary>
		/// Tests the <see cref="LogLevelBitMask.Equals(object)"/> method.
		/// </summary>
		/// <param name="padding">
		/// <c>true</c> to pad shorter masks with '1', <c>false</c> to pad them with '0'.
		/// </param>
		/// <param name="mask1">Mask 1 to compare.</param>
		/// <param name="mask2">Mask 2 to compare.</param>
		/// <param name="expected">
		/// <c>true</c> if the masks are expected to be equal;
		/// otherwise <c>false</c>.
		/// </param>
		[Theory]
		[MemberData(nameof(EqualityTestData))]
#pragma warning disable xUnit1024 // Test methods cannot have overloads
		public void Equals(
			bool   padding,
			string mask1,
			string mask2,
			bool   expected)
#pragma warning restore xUnit1024 // Test methods cannot have overloads
		{
			// mask must not be null as this tests the Equals() method of the instance 
			if (mask1 == null) return;

			var bitmask1 = MaskFromString(mask1, false, padding);
			var bitmask2 = MaskFromString(mask2, false, padding);
			Assert.Equal(expected, bitmask1.Equals(bitmask2));
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Converts a string containing only '0' and '1' characters to the corresponding <see cref="LogLevelBitMask"/>.
		/// </summary>
		/// <param name="mask">The bitmask as a string (may bew <c>null</c> to simply return <c>null</c>).</param>
		/// <param name="set">
		/// <c>false</c> to initialize the bitmask with '0' before setting/clearing bits explicitly.
		/// <c>true</c> to initialize the bitmask with '1' before setting/clearing bits explicitly.
		/// This only makes a difference, if <paramref name="mask"/> is not a multiple of 32.
		/// </param>
		/// <param name="padding">
		/// <c>false</c> to pad the bitmask with '0' when working with it, <c>true</c> to pad the bitmask with '1'.
		/// </param>
		/// <returns>The corresponding <see cref="LogLevelBitMask"/>.</returns>
		private static LogLevelBitMask MaskFromString(string mask, bool set = false, bool padding = false)
		{
			if (mask == null) return null;

			var bitmask = new LogLevelBitMask(mask.Length, set, padding);
			for (int i = 0; i < mask.Length; i++)
			{
				if (mask[i] == '0') bitmask.ClearBit(i);
				else if (mask[i] == '1') bitmask.SetBit(i);
				else throw new ArgumentException("The specified mask contains an invalid character.", nameof(mask));
			}

			return bitmask;
		}

		#endregion
	}

}
