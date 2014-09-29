// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Diagnostics.Views
{
    public class RuntimeInfoPageModel
    {
        public string Version { get; internal set; }

        public IEnumerable<ILibraryInformation> References { get; internal set; }
    }
}