// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets up default options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcDataAnnotationsMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        public MvcDataAnnotationsMvcOptionsSetup()
            : base(ConfigureMvc)
        {
        }

        public static void ConfigureMvc(MvcOptions options)
        {
            options.ModelMetadataDetailsProviders.Add(new DataAnnotationsMetadataProvider());
            options.ModelValidatorProviders.Add(new DataAnnotationsModelValidatorProvider());
        }
    }
}