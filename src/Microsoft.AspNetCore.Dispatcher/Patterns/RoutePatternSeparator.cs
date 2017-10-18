// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Dispatcher.Patterns
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePatternSeparator : RoutePatternPart
    {
        internal RoutePatternSeparator(string rawText, string content)
        {
            Debug.Assert(!string.IsNullOrEmpty(content));

            RawText = rawText;
            Content = content;

            PartKind = RoutePatternPartKind.Separator;
        }

        public string Content { get; }

        public override RoutePatternPartKind PartKind { get; }

        public override string RawText { get; }

        internal override string DebuggerToString()
        {
            return RawText ?? Content;
        }
    }
}
