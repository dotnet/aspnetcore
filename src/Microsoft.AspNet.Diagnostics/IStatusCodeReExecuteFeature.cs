// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Diagnostics
{
    [AssemblyNeutral]
    public interface IStatusCodeReExecuteFeature
    {
        string OriginalPathBase { get; set; }

        string OriginalPath { get; set; }
    }
}