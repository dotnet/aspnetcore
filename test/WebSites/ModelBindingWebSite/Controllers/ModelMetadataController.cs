// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using ModelBindingWebSite.Models;
using ModelBindingWebSite.ViewModels;

namespace ModelBindingWebSite.Controllers
{
    public class ModelMetadataController
    {
        [HttpGet(template: "/AdditionalValues")]
        public IDictionary<object, object> GetAdditionalValues([FromServices] IModelMetadataProvider provider)
        {
            var metadata = provider.GetMetadataForType(typeof(LargeModelWithValidation));

            return metadata.AdditionalValues;
        }

        [HttpGet(template: "/GroupNames")]
        public IDictionary<string, string> GetGroupNames([FromServices] IModelMetadataProvider provider)
        {
            var groupNames = new Dictionary<string, string>();
            var metadata = provider.GetMetadataForType(typeof(VehicleViewModel));
            foreach (var property in metadata.Properties)
            {
                groupNames.Add(property.PropertyName, property.GetGroupName());
            }

            return groupNames;
        }
    }
}