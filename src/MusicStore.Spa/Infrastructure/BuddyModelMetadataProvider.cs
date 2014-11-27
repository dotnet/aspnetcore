using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace MusicStore.Spa.Infrastructure
{
    public class BuddyModelMetadataProvider : DataAnnotationsModelMetadataProvider
    {
        protected override CachedDataAnnotationsModelMetadata CreateMetadataPrototype(IEnumerable<object> attributes, Type containerType, Type modelType, string propertyName)
        {
            var realTypeMetadata = base.CreateMetadataPrototype(attributes, containerType, modelType, propertyName);
            var buddyType = BuddyTypeAttribute.GetBuddyType(modelType);

            if (buddyType != null)
            {
                var buddyMetadata = base.CreateMetadataPrototype(attributes, containerType, buddyType, propertyName);
                foreach (var realProperty in realTypeMetadata.Properties)
                {
                    var buddyProperty = buddyMetadata.Properties.SingleOrDefault(bp => string.Equals(bp.PropertyName, realProperty.PropertyName, StringComparison.Ordinal));
                    if (buddyProperty != null)
                    {
                        // TODO: Only overwrite if the real type doesn't explicitly set it
                        realProperty.IsReadOnly = buddyProperty.IsReadOnly;
                        realProperty.IsRequired = buddyProperty.IsRequired;
                        realProperty.DisplayName = buddyProperty.DisplayName;
                        realProperty.DisplayFormatString = buddyProperty.DisplayFormatString;
                        realProperty.SimpleDisplayText = buddyProperty.SimpleDisplayText;
                        realProperty.DataTypeName = buddyProperty.DataTypeName;
                        realProperty.Description = buddyProperty.Description;
                        realProperty.EditFormatString = buddyProperty.EditFormatString;
                        realProperty.NullDisplayText = buddyProperty.NullDisplayText;
                        realProperty.ShowForDisplay = buddyProperty.ShowForDisplay;
                        realProperty.ShowForEdit = buddyProperty.ShowForEdit;
                        realProperty.TemplateHint = buddyProperty.TemplateHint;
                    }
                }
            }

            return realTypeMetadata;
        }
    }
}