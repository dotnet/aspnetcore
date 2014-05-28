// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CachedDataAnnotationsModelMetadata : CachedModelMetadata<CachedDataAnnotationsMetadataAttributes>
    {
        public CachedDataAnnotationsModelMetadata(CachedDataAnnotationsModelMetadata prototype, 
                                                  Func<object> modelAccessor)
            : base(prototype, modelAccessor)
        {
        }

        public CachedDataAnnotationsModelMetadata(DataAnnotationsModelMetadataProvider provider, 
                                                  Type containerType, 
                                                  Type modelType, 
                                                  string propertyName, 
                                                  IEnumerable<Attribute> attributes)
            : base(provider, 
                   containerType, 
                   modelType, 
                   propertyName, 
                   new CachedDataAnnotationsMetadataAttributes(attributes))
        {
        }

        protected override bool ComputeConvertEmptyStringToNull()
        {
            return PrototypeCache.DisplayFormat != null
                       ? PrototypeCache.DisplayFormat.ConvertEmptyStringToNull
                       : base.ComputeConvertEmptyStringToNull();
        }

        protected override string ComputeNullDisplayText()
        {
            return PrototypeCache.DisplayFormat != null
                       ? PrototypeCache.DisplayFormat.NullDisplayText
                       : base.ComputeNullDisplayText();
        }

        protected override string ComputeDescription()
        {
            return PrototypeCache.Display != null
                       ? PrototypeCache.Display.GetDescription()
                       : base.ComputeDescription();
        }

        protected override bool ComputeIsReadOnly()
        {
            if (PrototypeCache.Editable != null)
            {
                return !PrototypeCache.Editable.AllowEdit;
            }

            return base.ComputeIsReadOnly();
        }

        protected override bool ComputeIsRequired()
        {
            return (PrototypeCache.Required != null) || base.ComputeIsRequired();
        }

        public override string GetDisplayName()
        {
            // DisplayAttribute doesn't require you to set a name, so this could be null. 
            if (PrototypeCache.Display != null)
            {
                var name = PrototypeCache.Display.GetName();
                if (name != null)
                {
                    return name;
                }
            }

            // If DisplayAttribute does not specify a name, we'll fall back to the property name.
            return base.GetDisplayName();
        }
    }
}
