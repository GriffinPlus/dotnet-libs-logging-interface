///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// Interface of a sorted collection of tags (must be implemented thread-safe).
	/// 
	/// Valid tags may consist of the following characters only:
	/// - alphanumeric characters: [a-z], [A-Z], [0-9]
	/// - extra characters: [_ . , : ; + - #]
	/// - brackets: (), [], {}, &lt;&gt;
	///
	/// Asterisk(*) and quotation mark (?) are not supported as these characters are used to implement pattern matching with wildcards.
	/// Caret(^) and dollar sign ($) are not supported as these characters are used to implement the detection of regex strings.
	/// </summary>
	public interface ITagSet : IReadOnlyList<string>, IEquatable<ITagSet> { }

}
