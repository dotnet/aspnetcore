// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
#pragma warning disable CS0618 // Type or member is obsolete
    public abstract class PageLoaderBase : IPageLoader
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public abstract ValueTask<CompiledPageActionDescriptor> LoadAsync(PageActionDescriptor actionDescriptor);

        CompiledPageActionDescriptor IPageLoader.Load(PageActionDescriptor actionDescriptor)
        {
            return LoadAsync(actionDescriptor).GetAwaiter().GetResult();
        }
    }
}
