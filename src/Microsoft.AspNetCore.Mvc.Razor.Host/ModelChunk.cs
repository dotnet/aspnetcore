// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Chunks;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// <see cref="Chunk"/> for an <c>@model</c> directive.
    /// </summary>
    public class ModelChunk : Chunk
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ModelChunk"/>.
        /// </summary>
        /// <param name="modelType">The type of the view's model.</param>
        public ModelChunk(string modelType)
        {
            ModelType = modelType;
        }

        /// <summary>
        /// Gets the type of the view's model.
        /// </summary>
        public string ModelType { get; }
    }
}