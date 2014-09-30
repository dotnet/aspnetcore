// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    // TODO: Document this class as part of https://github.com/aspnet/Razor/issues/99

    public interface ITagHelperDescriptorResolver
    {
        IEnumerable<TagHelperDescriptor> Resolve(string lookupText);
    }
}