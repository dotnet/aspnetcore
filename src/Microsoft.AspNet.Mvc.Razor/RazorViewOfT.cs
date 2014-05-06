// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
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

        public ViewDataDictionary<TModel> ViewData { get; private set; }

        public IHtmlHelper<TModel> Html { get; set; }

        public override Task RenderAsync([NotNull] ViewContext context)
        {
            ViewData = context.ViewData as ViewDataDictionary<TModel>;
            if (ViewData == null)
            {
                if (context.ViewData != null)
                {
                    ViewData = new ViewDataDictionary<TModel>(context.ViewData);
                }
                else
                {
                    var metadataProvider = context.HttpContext.RequestServices.GetService<IModelMetadataProvider>();
                    ViewData = new ViewDataDictionary<TModel>(metadataProvider);
                }

                // Have new ViewDataDictionary; make sure it's visible everywhere.
                context.ViewData = ViewData;
            }

            InitHelpers(context);

            return base.RenderAsync(context);
        }

        private void InitHelpers(ViewContext context)
        {
            Html = context.HttpContext.RequestServices.GetService<IHtmlHelper<TModel>>();

            var contextable = Html as ICanHasViewContext;
            if (contextable != null)
            {
                contextable.Contextualize(context);
            }
        }
    }
}
