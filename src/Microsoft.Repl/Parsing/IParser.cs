// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Repl.Parsing
{
    public interface IParser
    {
        ICoreParseResult Parse(string commandText, int caretPosition);
    }

    public interface IParser<out TParseResult> : IParser
    {
        new TParseResult Parse(string commandText, int caretPosition);
    }
}
