// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public abstract class IntermediateNodeFormatter
    {
        public abstract void WriteChildren(IntermediateNodeCollection children);

        public abstract void WriteContent(string content);

        public abstract void WriteProperty(string key, string value);
    }
}
