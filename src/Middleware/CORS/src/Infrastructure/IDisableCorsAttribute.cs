// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    /// <summary>
    /// An interface which can be used to identify a type which provides metdata to disable cors for a resource.
    /// </summary>
    public interface IDisableCorsAttribute : ICorsMetadata
    {
    }
}
