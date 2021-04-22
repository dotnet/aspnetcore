// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// Creates a <see cref="CompiledPageActionDescriptor"/> from a <see cref="PageActionDescriptor"/>.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public abstract class PageLoader : IPageLoader
#pragma warning restore CS0618 // Type or member is obsolete
    {
        /// <summary>
        /// Produces a <see cref="CompiledPageActionDescriptor"/> given a <see cref="PageActionDescriptor"/>.
        /// </summary>
        /// <param name="actionDescriptor">The <see cref="PageActionDescriptor"/>.</param>
        /// <returns>A <see cref="Task"/> that on completion returns a <see cref="CompiledPageActionDescriptor"/>.</returns>
        [Obsolete("This overload is obsolete and no longer called by the framework. Use the overload that includes an EndpointMetadataCollection.")]
        public abstract Task<CompiledPageActionDescriptor> LoadAsync(PageActionDescriptor actionDescriptor);

        /// <summary>
        /// Produces a <see cref="CompiledPageActionDescriptor"/> given a <see cref="PageActionDescriptor"/>.
        /// </summary>
        /// <param name="actionDescriptor">The <see cref="PageActionDescriptor"/>.</param>
        /// <param name="endpointMetadata">The <see cref="EndpointMetadataCollection"/>.</param>
        /// <returns>A <see cref="Task"/> that on completion returns a <see cref="CompiledPageActionDescriptor"/>.</returns>
        public virtual Task<CompiledPageActionDescriptor> LoadAsync(PageActionDescriptor actionDescriptor, EndpointMetadataCollection endpointMetadata)
            => throw new NotSupportedException();

        CompiledPageActionDescriptor IPageLoader.Load(PageActionDescriptor actionDescriptor)
            => LoadAsync(actionDescriptor, EndpointMetadataCollection.Empty).GetAwaiter().GetResult();
    }
}
