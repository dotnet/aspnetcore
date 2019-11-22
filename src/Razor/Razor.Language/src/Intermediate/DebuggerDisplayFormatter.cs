// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    internal class DebuggerDisplayFormatter : IntermediateNodeFormatterBase
    {
        public DebuggerDisplayFormatter()
        {
            Writer = new StringWriter();
            ContentMode = FormatterContentMode.PreferContent;
        }

        public override string ToString()
        {
            return Writer.ToString();
        }
    }
}
