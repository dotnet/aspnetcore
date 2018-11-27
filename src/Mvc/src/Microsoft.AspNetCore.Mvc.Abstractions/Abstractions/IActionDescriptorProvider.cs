// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Abstractions
{
    public interface IActionDescriptorProvider
    {
        /// <summary>
        /// Gets the order value for determining the order of execution of providers. Providers execute in
        /// ascending numeric value of the <see cref="Order"/> property.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Providers are executed in an ordering determined by an ascending sort of the <see cref="Order"/> property.
        /// A provider with a lower numeric value of <see cref="Order"/> will have its
        /// <see cref="OnProvidersExecuting"/> called before that of a provider with a higher numeric value of
        /// <see cref="Order"/>. The <see cref="OnProvidersExecuted"/> method is called in the reverse ordering after
        /// all calls to <see cref="OnProvidersExecuting"/>. A provider with a lower numeric value of
        /// <see cref="Order"/> will have its <see cref="OnProvidersExecuted"/> method called after that of a provider
        /// with a higher numeric value of <see cref="Order"/>.
        /// </para>
        /// <para>
        /// If two providers have the same numeric value of <see cref="Order"/>, then their relative execution order
        /// is undefined.
        /// </para>
        /// </remarks>
        int Order { get; }

        void OnProvidersExecuting(ActionDescriptorProviderContext context);

        void OnProvidersExecuted(ActionDescriptorProviderContext context);
    }
}
