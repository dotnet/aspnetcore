// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class EditResult
    {
        public EditResult(PartialParseResultInternal result, SpanBuilder editedSpan)
        {
            Result = result;
            EditedSpan = editedSpan;
        }

        public PartialParseResultInternal Result { get; set; }
        public SpanBuilder EditedSpan { get; set; }
    }
}
