// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Web.Extensions.Head
{
    internal readonly struct TagElement
    {
        public string Type => "tag";

        public string TagName { get; }

        public IReadOnlyDictionary<string, object>? Attributes { get; }

        public TagElement(string tagName, IReadOnlyDictionary<string, object>? attributes)
        {
            TagName = tagName;
            Attributes = attributes;
        }
    }
}
