// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
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
