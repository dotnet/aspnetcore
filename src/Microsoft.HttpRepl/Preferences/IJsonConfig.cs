// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Repl.ConsoleHandling;

namespace Microsoft.HttpRepl.Preferences
{
    public interface IJsonConfig
    {
        int IndentSize { get; }

        AllowedColors DefaultColor { get; }

        AllowedColors ArrayBraceColor { get; }

        AllowedColors ObjectBraceColor { get; }

        AllowedColors CommaColor { get; }

        AllowedColors NameColor { get; }

        AllowedColors NameSeparatorColor { get; }

        AllowedColors BoolColor { get; }

        AllowedColors NumericColor { get; }

        AllowedColors StringColor { get; }

        AllowedColors NullColor { get; }
    }
}
