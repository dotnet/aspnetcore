// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Represents an entity which can provide model name as metadata.
    /// </summary>
    public interface IModelNameProvider
    {
        /// <summary>
        /// Model name.
        /// </summary>
        string Name { get; }
    }
}
