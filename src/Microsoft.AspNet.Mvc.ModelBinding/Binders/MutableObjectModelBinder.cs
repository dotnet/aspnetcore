using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MutableObjectModelBinder : IModelBinder
    {
        public virtual bool BindModel(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);

            if (!CanBindType(bindingContext.ModelType) ||
                !bindingContext.ValueProvider.ContainsPrefix(bindingContext.ModelName))
            {
                return false;
            }

            EnsureModel(bindingContext);
            IEnumerable<ModelMetadata> propertyMetadatas = GetMetadataForProperties(bindingContext);
            ComplexModelDto dto = CreateAndPopulateDto(bindingContext, propertyMetadatas);

            // post-processing, e.g. property setters and hooking up validation
            ProcessDto(bindingContext, dto);
            // TODO: Validation
            // bindingContext.ValidationNode.ValidateAllProperties = true; // complex models require full validation
            return true;
        }

        protected virtual bool CanUpdateProperty(ModelMetadata propertyMetadata)
        {
            return CanUpdatePropertyInternal(propertyMetadata);
        }

        private static bool CanBindType(Type modelType)
        {
            // Simple types cannot use this binder
            bool isComplexType = !modelType.HasStringConverter();
            if (!isComplexType)
            {
                return false;
            }

            if (modelType == typeof(ComplexModelDto))
            {
                // forbidden type - will cause a stack overflow if we try binding this type
                return false;
            }
            return true;
        }

        internal static bool CanUpdatePropertyInternal(ModelMetadata propertyMetadata)
        {
            return !propertyMetadata.IsReadOnly || CanUpdateReadOnlyProperty(propertyMetadata.ModelType);
        }

        private static bool CanUpdateReadOnlyProperty(Type propertyType)
        {
            // Value types have copy-by-value semantics, which prevents us from updating
            // properties that are marked readonly.
            if (propertyType.GetTypeInfo().IsValueType)
            {
                return false;
            }

            // Arrays are strange beasts since their contents are mutable but their sizes aren't.
            // Therefore we shouldn't even try to update these. Further reading:
            // http://blogs.msdn.com/ericlippert/archive/2008/09/22/arrays-considered-somewhat-harmful.aspx
            if (propertyType.IsArray)
            {
                return false;
            }

            // Special-case known immutable reference types
            if (propertyType == typeof(string))
            {
                return false;
            }

            return true;
        }

        private ComplexModelDto CreateAndPopulateDto(ModelBindingContext bindingContext, IEnumerable<ModelMetadata> propertyMetadatas)
        {
            // create a DTO and call into the DTO binder
            ComplexModelDto originalDto = new ComplexModelDto(bindingContext.ModelMetadata, propertyMetadatas);
            ModelBindingContext dtoBindingContext = new ModelBindingContext(bindingContext)
            {
                ModelMetadata = bindingContext.MetadataProvider.GetMetadataForType(() => originalDto, typeof(ComplexModelDto)),
                ModelName = bindingContext.ModelName
            };

            bindingContext.ModelBinder.BindModel(dtoBindingContext);
            return (ComplexModelDto)dtoBindingContext.Model;
        }

        protected virtual object CreateModel(ModelBindingContext bindingContext)
        {
            // If the Activator throws an exception, we want to propagate it back up the call stack, since the application
            // developer should know that this was an invalid type to try to bind to.
            return Activator.CreateInstance(bindingContext.ModelType);
        }

        //// Called when the property setter null check failed, allows us to add our own error message to ModelState.
        //internal static EventHandler<ModelValidatedEventArgs> CreateNullCheckFailedHandler(ModelMetadata modelMetadata, object incomingValue)
        //{
        //    return (sender, e) =>
        //    {
        //        ModelValidationNode validationNode = (ModelValidationNode)sender;
        //        ModelStateDictionary modelState = e.ActionContext.ModelState;

        //        if (modelState.IsValidField(validationNode.ModelStateKey))
        //        {
        //            string errorMessage = ModelBinderConfig.ValueRequiredErrorMessageProvider(e.ActionContext, modelMetadata, incomingValue);
        //            if (errorMessage != null)
        //            {
        //                modelState.AddModelError(validationNode.ModelStateKey, errorMessage);
        //            }
        //        }
        //    };
        //}

        protected virtual void EnsureModel(ModelBindingContext bindingContext)
        {
            if (bindingContext.Model == null)
            {
                bindingContext.ModelMetadata.Model = CreateModel(bindingContext);
            }
        }

        protected virtual IEnumerable<ModelMetadata> GetMetadataForProperties(ModelBindingContext bindingContext)
        {
            // TODO: Revive required properties. This has a dependency on HttpBindingAttribute and DataAnnotations 
            // keep a set of the required properties so that we can cross-reference bound properties later
            HashSet<string> requiredProperties = new HashSet<string>();
            // Dictionary<string, ModelValidator> requiredValidators;
            HashSet<string> skipProperties = new HashSet<string>();
            // GetRequiredPropertiesCollection(bindingContext, out requiredProperties, out skipProperties);

            return from propertyMetadata in bindingContext.ModelMetadata.Properties
                   let propertyName = propertyMetadata.PropertyName
                   let shouldUpdateProperty = requiredProperties.Contains(propertyName) || !skipProperties.Contains(propertyName)
                   where shouldUpdateProperty && CanUpdateProperty(propertyMetadata)
                   select propertyMetadata;
        }

        private static object GetPropertyDefaultValue(PropertyInfo propertyInfo)
        {
            DefaultValueAttribute attr = propertyInfo.GetCustomAttribute<DefaultValueAttribute>();
            return (attr != null) ? attr.Value : null;
        }

        //internal static void GetRequiredPropertiesCollection(ModelBindingContext bindingContext, out HashSet<string> requiredProperties, out HashSet<string> skipProperties)
        //{
        //    requiredProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //    // requiredValidators = new Dictionary<string, ModelValidator>(StringComparer.OrdinalIgnoreCase);
        //    skipProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        //    // Use attributes on the property before attributes on the type.
        //    Type modelType = bindingContext.ModelType;
        //    ICustomTypeDescriptor modelDescriptor = new AssociatedMetadataTypeTypeDescriptionProvider(modelType)
        //                                                        .GetTypeDescriptor(modelType);

        //    PropertyDescriptorCollection propertyDescriptors = modelDescriptor.GetProperties();

        //    // TODO: Revive HttpBindingBehavior
        //    // HttpBindingBehaviorAttribute typeAttr = modelDescriptor.GetAttributes().OfType<HttpBindingBehaviorAttribute>().SingleOrDefault();

        //    foreach (PropertyDescriptor propertyDescriptor in propertyDescriptors)
        //    {
        //        string propertyName = propertyDescriptor.Name;
        //        ModelMetadata propertyMetadata = bindingContext.PropertyMetadata[propertyName];
        //        // ModelValidator requiredValidator = context.GetValidators(propertyMetadata).Where(v => v.IsRequired).FirstOrDefault();
        //        // requiredValidators[propertyName] = requiredValidator;

        //        HttpBindingBehaviorAttribute propAttr = propertyDescriptor.Attributes.OfType<HttpBindingBehaviorAttribute>().SingleOrDefault();
        //        HttpBindingBehaviorAttribute workingAttr = propAttr ?? typeAttr;
        //        if (workingAttr != null)
        //        {
        //            switch (workingAttr.Behavior)
        //            {
        //                case HttpBindingBehavior.Required:
        //                    requiredProperties.Add(propertyName);
        //                    break;

        //                case HttpBindingBehavior.Never:
        //                    skipProperties.Add(propertyName);
        //                    break;
        //            }
        //        }
        //        else if (requiredValidator != null)
        //        {
        //            requiredProperties.Add(propertyName);
        //        }
        //    }
        //}

        internal void ProcessDto(ModelBindingContext bindingContext, ComplexModelDto dto)
        {
            // TODO: Uncomment this once we revive validation

            //HashSet<string> requiredProperties;
            //// Dictionary<string, ModelValidator> requiredValidators;
            //HashSet<string> skipProperties;
            //GetRequiredPropertiesCollection(context, bindingContext, out requiredProperties, out requiredValidators, out skipProperties);

            //// Eliminate provided properties from requiredProperties; leaving just *missing* required properties.
            //requiredProperties.ExceptWith(dto.Results.Select(r => r.Key.PropertyName));

            //foreach (string missingRequiredProperty in requiredProperties)
            //{
            //    string modelStateKey = ModelBindingHelper.CreatePropertyModelName(
            //        bindingContext.ValidationNode.ModelStateKey, missingRequiredProperty);

            //    // Update Model as SetProperty() would: Place null value where validator will check for non-null. This
            //    // ensures a failure result from a required validator (if any) even for a non-nullable property.
            //    // (Otherwise, propertyMetadata.Model is likely already null.)
            //    ModelMetadata propertyMetadata = bindingContext.PropertyMetadata[missingRequiredProperty];
            //    propertyMetadata.Model = null;

            //    // Execute validator (if any) to get custom error message.
            //    ModelValidator validator = requiredValidators[missingRequiredProperty];
            //    bool addedError = RunValidator(validator, bindingContext, propertyMetadata, modelStateKey);

            //    // Fall back to default message if HttpBindingBehaviorAttribute required this property or validator
            //    // (oddly) succeeded.
            //    if (!addedError)
            //    {
            //        bindingContext.ModelState.AddModelError(modelStateKey,
            //            Error.Format(SRResources.MissingRequiredMember, missingRequiredProperty));
            //    }
            //}

            // for each property that was bound, call the setter, recording exceptions as necessary
            foreach (var entry in dto.Results)
            {
                ModelMetadata propertyMetadata = entry.Key;

                ComplexModelDtoResult dtoResult = entry.Value;
                if (dtoResult != null)
                {
                    SetProperty(bindingContext, propertyMetadata, dtoResult);
                    // TODO: Validation
                    // bindingContext.ValidationNode.ChildNodes.Add(dtoResult.ValidationNode);
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We're recording this exception so that we can act on it later.")]
        protected virtual void SetProperty(ModelBindingContext bindingContext,
                                           ModelMetadata propertyMetadata,
                                           ComplexModelDtoResult dtoResult
                                           /*, ModelValidator requiredValidator*/)
        {
            // TODO: This used TypeDescriptor which is no longer available. Lookups performed using System.ComponentModel were 
            // cached. To maintain parity, we'll need to cache property lookups.
            PropertyInfo property = bindingContext.ModelType
                                                  .GetRuntimeProperties()
                                                  .FirstOrDefault(p => p.Name.Equals(propertyMetadata.PropertyName, StringComparison.OrdinalIgnoreCase));

            if (property == null || !property.CanWrite)
            {
                // nothing to do
                return;
            }

            object value = dtoResult.Model ?? GetPropertyDefaultValue(property);
            propertyMetadata.Model = value;


            //// 'Required' validators need to run first so that we can provide useful error messages if
            //// the property setters throw, e.g. if we're setting entity keys to null. See comments in
            //// DefaultModelBinder.SetProperty() for more information.
            //if (value == null)
            //{
            //    string modelStateKey = dtoResult.ValidationNode.ModelStateKey;
            //    if (bindingContext.ModelState.IsValidField(modelStateKey))
            //    {
            //        RunValidator(requiredValidator, bindingContext, propertyMetadata, modelStateKey);
            //    }
            //}

            if (value != null || property.PropertyType.AllowsNullValue())
            {
                try
                {
                    property.SetValue(bindingContext.Model, value);
                }
                catch (Exception ex)
                {
                    // don't display a duplicate error message if a binding error has already occurred for this field
                    //string modelStateKey = dtoResult.ValidationNode.ModelStateKey;
                    //if (bindingContext.ModelState.IsValidField(modelStateKey))
                    //{
                    bindingContext.ModelState.AddModelError(bindingContext.ModelName, ex);
                    //}
                }
            }
            else
            {
                // trying to set a non-nullable value type to null, need to make sure there's a message
                //string modelStateKey = dtoResult.ValidationNode.ModelStateKey;
                //if (bindingContext.ModelState.IsValidField(modelStateKey))
                //{
                //    dtoResult.ValidationNode.Validated += CreateNullCheckFailedHandler(propertyMetadata, value);
                //}
            }
        }

        //// Returns true if validator execution adds a model error.
        //private static bool RunValidator(ModelValidator validator, ModelBindingContext bindingContext,
        //    ModelMetadata propertyMetadata, string modelStateKey)
        //{
        //    bool addedError = false;
        //    if (validator != null)
        //    {
        //        foreach (ModelValidationResult validationResult in validator.Validate(propertyMetadata, bindingContext.Model))
        //        {
        //            bindingContext.ModelState.AddModelError(modelStateKey, validationResult.Message);
        //            addedError = true;
        //        }
        //    }

        //    return addedError;
        //}
    }
}
