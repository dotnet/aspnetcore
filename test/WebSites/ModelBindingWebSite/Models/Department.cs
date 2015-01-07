// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace ModelBindingWebSite
{
    public class Department
    {
        // A single property marked with a binder metadata attribute makes it a binder metadata poco.
        [FromTest]
        public string Name { get; set; }
    }
}