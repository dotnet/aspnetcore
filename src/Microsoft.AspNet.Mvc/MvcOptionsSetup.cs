// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.OptionsModel;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets up default options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        public MvcOptionsSetup()
            : base(ConfigureMvc)
        {
            Order = DefaultOrder.DefaultFrameworkSortOrder + 1;
        }

        public static void ConfigureMvc(MvcOptions options)
        {
            options.ModelMetadataDetailsProviders.Add(new DataAnnotationsMetadataProvider());
            options.ModelMetadataDetailsProviders.Add(new DataMemberRequiredBindingMetadataProvider());

            options.ModelValidatorProviders.Add(new DataAnnotationsModelValidatorProvider());

            options.ValidationExcludeFilters.Add(typeof(XObject));
            options.ValidationExcludeFilters.Add(typeof(JToken));
            options.ValidationExcludeFilters.Add(typeFullName: "System.Xml.XmlNode");
        }
    }
}