// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Repl.ConsoleHandling
{
    public interface IWritable
    {
        void Write(char c);

        void Write(string s);

        void WriteLine();

        void WriteLine(string s);

        bool IsCaretVisible { get; set; }
    }
}
