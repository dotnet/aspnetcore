// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Repl.Commanding
{
    public class CommandInputProcessingIssue
    {
        public CommandInputProcessingIssueKind Kind { get; }

        public string Text { get; }

        public CommandInputProcessingIssue(CommandInputProcessingIssueKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }
    }
}
