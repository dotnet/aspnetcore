// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    public abstract class RazorView<TModel> : RazorView
    {
        public TModel Model
        {
            get
            {
                return ViewData == null ? default(TModel) : ViewData.Model;
            }
        }

        [Activate]
        public ViewDataDictionary<TModel> ViewData { get; set; }

        public override Task RenderAsync([NotNull] ViewContext context)
        {
            var viewActivator = context.HttpContext.RequestServices.GetService<IRazorViewActivator>();
            viewActivator.Activate(this, context);

            return base.RenderAsync(context);
        }
    }
}
