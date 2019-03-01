// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// Creates a <see cref="CompiledPageActionDescriptor"/> from a <see cref="PageActionDescriptor"/>.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public abstract class PageLoader : IPageLoader
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public abstract Task<CompiledPageActionDescriptor> LoadAsync(PageActionDescriptor actionDescriptor);

        CompiledPageActionDescriptor IPageLoader.Load(PageActionDescriptor actionDescriptor)
            => LoadAsync(actionDescriptor).GetAwaiter().GetResult();
    }
}
