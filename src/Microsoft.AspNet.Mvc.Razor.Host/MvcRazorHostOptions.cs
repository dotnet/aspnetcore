// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents configuration options for the Razor Host
    /// </summary>
    public class MvcRazorHostOptions
    {
        public MvcRazorHostOptions()
        {
            DefaultModel = "dynamic";
            ActivateAttributeName = "Microsoft.AspNet.Mvc.ActivateAttribute";
            DefaultInjectedProperties = new List<InjectDescriptor>()
            {
                new InjectDescriptor("Microsoft.AspNet.Mvc.Rendering.IHtmlHelper<TModel>", "Html"),
                new InjectDescriptor("Microsoft.AspNet.Mvc.IViewComponentHelper", "Component"),
            };
        }

        /// <summary>
        /// Gets or sets the model that is used by default for generated views
        /// when no model is explicily specified. Defaults to dynamic.
        /// </summary>
        public string DefaultModel { get; set; }

        /// <summary>
        /// Gets or sets the attribue that is used to decorate properties that are injected and need to
        /// be activated.
        /// </summary>
        public string ActivateAttributeName { get; set; }

        /// <summary>
        /// Gets the list of properties that are injected by default.
        /// </summary>
        public IList<InjectDescriptor> DefaultInjectedProperties { get; private set; }
    }
}