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

            if (PrototypeCache.ReadOnly != null)
            {
                return PrototypeCache.ReadOnly.IsReadOnly;
            }

            return base.ComputeIsReadOnly();
        }

        public override string GetDisplayName()
        {
            // DisplayName could be provided by either the DisplayAttribute, or DisplayNameAttribute. If neither of
            // those supply a name, then we fall back to the property name (in base.GetDisplayName()).
            // 
            // DisplayName has lower precedence than Display.Name, for consistency with MVC.

            // DisplayAttribute doesn't require you to set a name, so this could be null. 
            if (PrototypeCache.Display != null)
            {
                string name = PrototypeCache.Display.GetName();
                if (name != null)
                {
                    return name;
                }
            }

            // It's also possible for DisplayNameAttribute to be used without setting a name. If a user does that, then DisplayName will
            // return the empty string - but for consistency with MVC we allow it. We do fallback to the property name in the (unlikely)
            // scenario that the user sets null as the DisplayName, again, for consistency with MVC.
            if (PrototypeCache.DisplayName != null)
            {
                string name = PrototypeCache.DisplayName.DisplayName;
                if (name != null)
                {
                    return name;
                }
            }

            // If neither attribute specifies a name, we'll fall back to the property name.
            return base.GetDisplayName();
        }
    }
}
