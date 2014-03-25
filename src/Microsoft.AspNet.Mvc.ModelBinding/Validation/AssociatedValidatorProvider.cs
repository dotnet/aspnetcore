using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public abstract class AssociatedValidatorProvider : IModelValidatorProvider
    {
        public IEnumerable<IModelValidator> GetValidators([NotNull] ModelMetadata metadata)
        {
            if (metadata.ContainerType != null && !string.IsNullOrEmpty(metadata.PropertyName))
            {
                return GetValidatorsForProperty(metadata);
            }

            return GetValidatorsForType(metadata);
        }

        protected abstract IEnumerable<IModelValidator> GetValidators(ModelMetadata metadata, 
                                                                      IEnumerable<Attribute> attributes);

        private IEnumerable<IModelValidator> GetValidatorsForProperty(ModelMetadata metadata)
        {
            var propertyName = metadata.PropertyName;
            var property = metadata.ContainerType
                                   .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                
            if (property == null)
            {
                throw new ArgumentException(
                    Resources.FormatCommon_PropertyNotFound(
                        metadata.ContainerType.FullName, 
                        metadata.PropertyName),
                    "metadata");
            }

            var attributes = property.GetCustomAttributes();
            return GetValidators(metadata, attributes);
        }

        private IEnumerable<IModelValidator> GetValidatorsForType(ModelMetadata metadata)
        {
            var attributes = metadata.ModelType
                                     .GetTypeInfo()
                                     .GetCustomAttributes();
            return GetValidators(metadata, attributes);
        }
    }
}
