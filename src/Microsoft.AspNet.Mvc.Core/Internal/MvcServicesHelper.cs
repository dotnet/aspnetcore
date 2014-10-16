// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Internal
{
    /// <summary>
    /// Helper class which contains MvcServices related helpers.
    /// </summary>
    public static class MvcServicesHelper
    {
        /// <summary>
        /// Throws InvalidOperationException when MvcMarkerService is not present
        /// in the list of services.
        /// </summary>
        /// <param name="services">The list of services.</param>
        /// <param name="serviceType">The type of service which needs to be searched for.</param>
        public static void ThrowIfMvcNotRegistered(IServiceProvider services)
        {
            if (services.GetService(typeof(MvcMarkerService)) == null)
            {
                throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                    "IServiceCollection.AddMvc()",
                    "IApplicationBuilder.UseServices(...)",
                    "IApplicationBuilder.UseMvc(...)"));
            }
        }
    }
}