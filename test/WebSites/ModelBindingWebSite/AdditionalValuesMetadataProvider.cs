// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite
{
    public class AdditionalValuesMetadataProvider : DataAnnotationsModelMetadataProvider
    {
        public static readonly string GroupNameKey = "__GroupName";
        private static Guid _guid = new Guid("7d6d0de2-8d59-49ac-99cc-881423b75a76");

        protected override CachedDataAnnotationsModelMetadata CreateMetadataFromPrototype(
            CachedDataAnnotationsModelMetadata prototype,
            Func<object> modelAccessor)
        {
            var metadata = base.CreateMetadataFromPrototype(prototype, modelAccessor);
            foreach (var keyValuePair in prototype.AdditionalValues)
            {
                metadata.AdditionalValues.Add(keyValuePair);
            }

            return metadata;
        }

        protected override CachedDataAnnotationsModelMetadata CreateMetadataPrototype(
            IEnumerable<object> attributes,
            Type containerType,
            Type modelType,
            string propertyName)
        {
            var metadata = base.CreateMetadataPrototype(attributes, containerType, modelType, propertyName);
            if (modelType == typeof(LargeModelWithValidation))
            {
                metadata.AdditionalValues.Add("key1", _guid);
                metadata.AdditionalValues.Add("key2", "value2");
            }

            var displayAttribute = attributes.OfType<DisplayAttribute>().FirstOrDefault();
            var groupName = displayAttribute?.GroupName;
            if (!string.IsNullOrEmpty(groupName))
            {
                metadata.AdditionalValues[GroupNameKey] = groupName;
            }

            return metadata;
        }
    }
}