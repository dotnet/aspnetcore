using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace MusicStore.Spa.Infrastructure
{
    public class BuddyValidatorProvider : DataAnnotationsModelValidatorProvider
    {
        protected override IEnumerable<IModelValidator> GetValidators(ModelMetadata metadata, IEnumerable<object> attributes)
        {
            var buddyType = BuddyTypeAttribute.GetBuddyType(metadata.ContainerType ?? metadata.ModelType);

            if (buddyType != null)
            {
                var buddyProperty = buddyType.GetTypeInfo().GetDeclaredProperty(metadata.PropertyName);
                if (buddyProperty != null)
                {
                    var buddyTypeAttributes = buddyProperty.GetCustomAttributes();
                    // TODO: De-dupe?
                    attributes = attributes.Concat(buddyTypeAttributes);
                    return base.GetValidators(metadata, attributes);
                }
            }

            return Enumerable.Empty<IModelValidator>();
        }
    }
}