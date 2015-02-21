// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Mvc.Razor.Host;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// Contains helper methods for dealing with Chunks
    /// </summary>
    public static class ChunkHelper
    {
        private const string TModelToken = "<TModel>";

        /// <summary>
        /// Attempts to cast the passed in <see cref="Chunk"/> to type <typeparamref name="TChunk"/> and throws if the
        /// cast fails.
        /// </summary>
        /// <typeparam name="TChunk">The type to cast to.</typeparam>
        /// <param name="chunk">The chunk to cast.</param>
        /// <returns>The <paramref name="Chunk"/> cast to <typeparamref name="TChunk"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="chunk"/> is not an instance of
        /// <typeparamref name="TChunk"/>.</exception>
        public static TChunk EnsureChunk<TChunk>([NotNull] Chunk chunk)
            where TChunk : Chunk
        {
            var chunkOfT = chunk as TChunk;
            if (chunkOfT == null)
            {
                var message = Resources.FormatArgumentMustBeOfType(typeof(TChunk).FullName);
                throw new ArgumentException(message, "chunk");
            }

            return chunkOfT;
        }

        /// <summary>
        /// Returns the <see cref="ModelChunk"/> used to determine the model name for the page generated
        /// using the specified <paramref name="codeTree"/>
        /// </summary>
        /// <param name="codeTree">The <see cref="CodeTree"/> to scan for <see cref="ModelChunk"/>s in.</param>
        /// <returns>The last <see cref="ModelChunk"/> in the <see cref="CodeTree"/> if found, null otherwise.
        /// </returns>
        public static ModelChunk GetModelChunk([NotNull] CodeTree codeTree)
        {
            // If there's more than 1 model chunk there will be a Razor error BUT we want intellisense to show up on
            // the current model chunk that the user is typing.
            return codeTree.Chunks
                           .OfType<ModelChunk>()
                           .LastOrDefault();
        }

        /// <summary>
        /// Returns the type name of the Model specified via a <see cref="ModelChunk"/> in the
        /// <paramref name="codeTree"/> if specified or the default model type.
        /// </summary>
        /// <param name="codeTree">The <see cref="CodeTree"/> to scan for <see cref="ModelChunk"/>s in.</param>
        /// <param name="defaultModelName">The <see cref="Type"/> name of the default model.</param>
        /// <returns>The model type name for the generated page.</returns>
        public static string GetModelTypeName([NotNull] CodeTree codeTree,
                                              [NotNull] string defaultModelName)
        {
            var modelChunk = GetModelChunk(codeTree);
            return modelChunk != null ? modelChunk.ModelType : defaultModelName;
        }

        /// <summary>
        /// Returns a string with the &lt;TModel&gt; token replaced with the value specified in
        /// <paramref name="modelName"/>.
        /// </summary>
        /// <param name="value">The string to replace the token in.</param>
        /// <param name="modelName">The model name to replace with.</param>
        /// <returns>A string with the token replaced.</returns>
        public static string ReplaceTModel([NotNull] string value,
                                           [NotNull] string modelName)
        {
            return value.Replace(TModelToken, modelName);
        }
    }
}