// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorMvcViewOptionsSetup : ConfigureOptions<MvcViewOptions>
    {
        public MvcRazorMvcViewOptionsSetup()
            : base(ConfigureMvc)
        {
            Order = DefaultOrder.DefaultFrameworkSortOrder;
        }

        public static void ConfigureMvc(MvcViewOptions options)
        {
            options.ViewEngines.Add(typeof(RazorViewEngine));
        }
    }
}