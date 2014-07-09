// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets up default options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcOptionsSetup : IOptionsSetup<MvcOptions>
    {
        /// <inheritdoc />
        public int Order
        {
            get { return 1; }
        }

        /// <inheritdoc />
        public void Setup(MvcOptions options)
        {
            // Set up ViewEngines
            options.ViewEngines.Add(typeof(RazorViewEngine));

            // Set up ModelBinding
            options.ModelBinders.Add(new TypeConverterModelBinder());
            options.ModelBinders.Add(new TypeMatchModelBinder());
            options.ModelBinders.Add(typeof(GenericModelBinder));
            options.ModelBinders.Add(new MutableObjectModelBinder());
            options.ModelBinders.Add(new ComplexModelDtoModelBinder());
        }
    }
}