// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class RouteValueValueProviderFactory : IValueProviderFactory
    {
        public Task<IValueProvider> GetValueProviderAsync([NotNull] ValueProviderFactoryContext context)
        {
            return Task.FromResult<IValueProvider>(new DictionaryBasedValueProvider(
                BindingSource.Path,
                context.RouteValues));
        }
    }
}
