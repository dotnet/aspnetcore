// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Chunks;

namespace Microsoft.AspNetCore.Mvc.Razor.Directives
{
    /// <summary>
    /// Contains helper methods for dealing with Chunks
    /// </summary>
    public static class ChunkHelper
    {
        /// <summary>
        /// Token that is replaced by the model name in <c>@inherits</c> and <c>@inject</c>
        /// chunks as part of <see cref="ChunkInheritanceUtility"/>.
        /// </summary>
        public static readonly string TModelToken = "TModel";
        private static readonly string TModelReplaceToken = $"<{TModelToken}>";

        /// <summary>
        /// Returns the <see cref="ModelChunk"/> used to determine the model name for the page generated
        /// using the specified <paramref name="chunkTree"/>
        /// </summary>
        /// <param name="chunkTree">The <see cref="ChunkTree"/> to scan for <see cref="ModelChunk"/>s in.</param>
        /// <returns>The last <see cref="ModelChunk"/> in the <see cref="ChunkTree"/> if found, <c>null</c> otherwise.
        /// </returns>
        public static ModelChunk GetModelChunk(ChunkTree chunkTree)
        {
            if (chunkTree == null)
            {
                throw new ArgumentNullException(nameof(chunkTree));
            }

            // If there's more than 1 model chunk there will be a Razor error BUT we want intellisense to show up on
            // the current model chunk that the user is typing.
            return chunkTree
                .Children
                .OfType<ModelChunk>()
                .LastOrDefault();
        }

        /// <summary>
        /// Returns the type name of the Model specified via a <see cref="ModelChunk"/> in the
        /// <paramref name="chunkTree"/> if specified or the default model type.
        /// </summary>
        /// <param name="chunkTree">The <see cref="ChunkTree"/> to scan for <see cref="ModelChunk"/>s in.</param>
        /// <param name="defaultModelName">The <see cref="Type"/> name of the default model.</param>
        /// <returns>The model type name for the generated page.</returns>
        public static string GetModelTypeName(
            ChunkTree chunkTree,
            string defaultModelName)
        {
            if (chunkTree == null)
            {
                throw new ArgumentNullException(nameof(chunkTree));
            }

            if (defaultModelName == null)
            {
                throw new ArgumentNullException(nameof(defaultModelName));
            }

            var modelChunk = GetModelChunk(chunkTree);
            return modelChunk != null ? modelChunk.ModelType : defaultModelName;
        }

        /// <summary>
        /// Returns a string with the &lt;TModel&gt; token replaced with the value specified in
        /// <paramref name="modelName"/>.
        /// </summary>
        /// <param name="value">The string to replace the token in.</param>
        /// <param name="modelName">The model name to replace with.</param>
        /// <returns>A string with the token replaced.</returns>
        public static string ReplaceTModel(
            string value,
            string modelName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            return value.Replace(TModelReplaceToken, modelName);
        }
    }
}