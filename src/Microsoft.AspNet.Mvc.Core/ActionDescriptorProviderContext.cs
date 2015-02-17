// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class ActionDescriptorProviderContext
    {
        public ActionDescriptorProviderContext()
        {
            Results = new List<ActionDescriptor>();
        }

        public IList<ActionDescriptor> Results { get; }
    }
}
