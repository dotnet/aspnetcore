// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Razor.Host.Internal
{
    /// <summary>
    /// Contains necessary information for the view component <see cref="AspNetCore.Razor.TagHelpers.TagHelper"/> code generation process.
    /// </summary>
    public class GeneratedViewComponentTagHelperContext
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="GeneratedViewComponentTagHelperContext"/> with default values.  
        /// </summary>
        public GeneratedViewComponentTagHelperContext()
        {
            ContextualizeMethodName = "Contextualize";
            InvokeAsyncMethodName = "InvokeAsync";
            IViewComponentHelperTypeName = "Microsoft.AspNetCore.Mvc.IViewComponentHelper";
            IViewContextAwareTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware";
            ViewContextAttributeTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute";
            ViewContextTypeName = "Microsoft.AspNetCore.Mvc.Rendering.ViewContext";
        }

        /// <summary> 
        /// Name of the Contextualize method called by an instance of the IViewContextAware type.
        /// </summary>
        public string ContextualizeMethodName { get; set; }

        /// <summary>
        /// Name of the InvokeAsync method called by an IViewComponentHelper.
        /// </summary>
        public string InvokeAsyncMethodName { get; set; }

        /// <summary>
        /// Name of the IViewComponentHelper type used to invoke view components.
        /// </summary>
        public string IViewComponentHelperTypeName { get; set; }

        /// <summary>
        /// Name of the IViewContextAware type used to contextualize the view context.
        /// </summary>
        public string IViewContextAwareTypeName { get; set; }

        /// <summary>
        /// Name of the ViewContext type for view execution.
        /// </summary>
        public string ViewContextTypeName { get; set; }

        /// <summary>
        /// Name of the ViewContextAttribute type.
        /// </summary>
        public string ViewContextAttributeTypeName { get; set; }
    }
}