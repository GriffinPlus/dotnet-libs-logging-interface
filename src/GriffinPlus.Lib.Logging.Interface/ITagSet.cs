///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Interface of a sorted collection of tags (must be implemented thread-safe).<br/>
/// <br/>
/// Valid tags may consist of the following characters only:<br/>
/// - alphanumeric characters: <c>[a-z]</c>, <c>[A-Z]</c>, <c>[0-9]</c><br/>
/// - extra characters: [<c>_</c>, <c>.</c>, <c>,</c>, <c>:</c>, <c>;</c>, <c>+</c>, <c>-</c>, <c>#</c>]<br/>
/// - brackets: <c>()</c>, <c>[]</c>, <c>{}</c>, <c>&lt;&gt;</c><br/>
/// <br/>
/// Asterisk(<c>*</c>) and quotation mark (<c>?</c>) are not supported as these characters are used to implement
/// pattern matching with wildcards. Caret(<c>^</c>) and dollar sign (<c>$</c>) are not supported as these characters
/// are used to implement the detection of regex strings.
/// </summary>
public interface ITagSet : IReadOnlyList<string>, IEquatable<ITagSet>;
