// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IValueProviderFactory
    {
        /// <summary>
        /// Gets a <see cref="IValueProvider"/> with values from the current request.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that when completed will yield a <see cref="IValueProvider"/> instance or <c>null</c>.
        /// </returns>
        Task<IValueProvider> GetValueProviderAsync(ActionContext context);
    }
}
