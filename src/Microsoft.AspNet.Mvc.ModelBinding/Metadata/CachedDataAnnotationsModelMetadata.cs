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
