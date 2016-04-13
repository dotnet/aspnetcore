// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies that the class or method that this attribute is applied validates the anti-forgery token.
    /// If the anti-forgery token is not available, or if the token is invalid, the validation will fail
    /// and the action method will not execute.
    /// </summary>
    /// <remarks>
    /// This attribute helps defend against cross-site request forgery. It won't prevent other forgery or tampering
    /// attacks.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ValidateAntiForgeryTokenAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        /// <inheritdoc />
        public int Order { get; set; }

        /// <inheritdoc />
        public bool IsReusable => true;

        /// <inheritdoc />
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<ValidateAntiforgeryTokenAuthorizationFilter>();
        }
    }
}