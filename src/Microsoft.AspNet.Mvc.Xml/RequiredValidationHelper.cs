// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Xml
{
    /// <summary>
    /// Validates types having value type properties decorated with <see cref="RequiredAttribute"/>
    /// but no <see cref="DataMemberAttribute"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonInputFormatter"/> supports <see cref="RequiredAttribute"/> where as the xml formatters
    /// do not. Since a user's aplication can have both Json and Xml formatters, a request could be validated
    /// when posted as Json but not Xml. So to prevent end users from having a false sense of security when posting
    /// as Xml, we add errors to model-state to at least let the users know that there is a problem with their models.
    /// </remarks>
    public class DataAnnotationRequiredAttributeValidation
    {
        // Since formatters are 'typically' registered as single instance, concurrent dictionary is used
        // here to avoid duplicate errors being added for a type.
        private ConcurrentDictionary<Type, Dictionary<Type, List<string>>> _cachedValidationErrors
            = new ConcurrentDictionary<Type, Dictionary<Type, List<string>>>();

        public void Validate([NotNull] Type modelType, [NotNull] ModelStateDictionary modelStateDictionary)
        {
            var visitedTypes = new HashSet<Type>();

            // Every node maintains a dictionary of Type => Errors. 
            // It's a dictionary as we want to avoid adding duplicate error messages.
            // Example:
            // In the following case, from the perspective of type 'Store', we should not see duplicate
            // errors related to type 'Address'
            // public class Store
            // {
            //    [Required]
            //    public int Id { get; set; }
            //    public Address Address { get; set; }
            // }
            // public class Employee
            // {
            //    [Required]
            //    public int Id { get; set; }
            //    public Address Address { get; set; }
            // }
            // public class Address
            // {
            //    [Required]
            //    public string Line1 { get; set; }
            //    [Required]
            //    public int Zipcode { get; set; }
            //    [Required]
            //    public string State { get; set; }
            // }
            var rootNodeValidationErrors = new Dictionary<Type, List<string>>();

            Validate(modelType, visitedTypes, rootNodeValidationErrors);

            foreach (var validationError in rootNodeValidationErrors)
            {
                foreach (var validationErrorMessage in validationError.Value)
                {
                    // Add error message to model state as exception to avoid
                    // disclosing details to end user as SerializableError sanitizes the
                    // model state errors having exceptions with a generic message when sending
                    // it to the client.
                    modelStateDictionary.TryAddModelError(
                        validationError.Key.FullName,
                        new InvalidOperationException(validationErrorMessage));
                }
            }
        }

        private void Validate(
            Type modelType,
            HashSet<Type> visitedTypes,
            Dictionary<Type, List<string>> errors)
        {
            // We don't need to code special handling for KeyValuePair (for example, when the model type 
            // is Dictionary<,> which implements IEnumerable<KeyValuePair<TKey, TValue>>) as the model 
            // type here would be KeyValuePair<TKey, TValue> where Key and Value are public properties
            // which would also be probed for Required attribute validation.
            if (modelType.IsGenericType())
            {
                var enumerableOfT = modelType.ExtractGenericInterface(typeof(IEnumerable<>));
                if (enumerableOfT != null)
                {
                    modelType = enumerableOfT.GetGenericArguments()[0];
                }
            }

            if (ExcludeTypeFromValidation(modelType))
            {
                return;
            }

            // Avoid infinite loop in case of self-referencing properties
            if (!visitedTypes.Add(modelType))
            {
                return;
            }

            Dictionary<Type, List<string>> cachedErrors;
            if (_cachedValidationErrors.TryGetValue(modelType, out cachedErrors))
            {
                foreach (var validationError in cachedErrors)
                {
                    errors.Add(validationError.Key, validationError.Value);
                }

                return;
            }

            foreach (var propertyHelper in PropertyHelper.GetProperties(modelType))
            {
                var propertyInfo = propertyHelper.Property;
                var propertyType = propertyInfo.PropertyType;

                // Since DefaultObjectValidator can handle Required attribute validation for reference types,
                // we only consider value types here.
                if (propertyType.IsValueType() && !propertyType.IsNullableValueType())
                {
                    var validationError = GetValidationError(propertyInfo);
                    if (validationError != null)
                    {
                        List<string> errorMessages;
                        if (!errors.TryGetValue(validationError.ModelType, out errorMessages))
                        {
                            errorMessages = new List<string>();
                            errors.Add(validationError.ModelType, errorMessages);
                        }

                        errorMessages.Add(Resources.FormatRequiredProperty_MustHaveDataMemberRequired(
                            typeof(DataContractSerializer).FullName,
                            typeof(RequiredAttribute).FullName,
                            typeof(DataMemberAttribute).FullName,
                            nameof(DataMemberAttribute.IsRequired),
                            bool.TrueString,
                            validationError.PropertyName, 
                            validationError.ModelType.FullName));
                    }
                    
                    // if the type is not primitve, then it could be a struct in which case
                    // we need to probe its properties for validation
                    if (propertyType.GetTypeInfo().IsPrimitive)
                    {
                        continue;
                    }
                }

                var childNodeErrors = new Dictionary<Type, List<string>>();
                Validate(propertyType, visitedTypes, childNodeErrors);

                // Avoid adding duplicate errors at current node.
                foreach (var modelTypeKey in childNodeErrors.Keys)
                {
                    if (!errors.ContainsKey(modelTypeKey))
                    {
                        errors.Add(modelTypeKey, childNodeErrors[modelTypeKey]);
                    }
                }
            }

            _cachedValidationErrors.TryAdd(modelType, errors);

            visitedTypes.Remove(modelType);
        }

        private ValidationError GetValidationError(PropertyInfo propertyInfo)
        {
            var required = propertyInfo.GetCustomAttribute(typeof(RequiredAttribute), inherit: true);
            if (required == null)
            {
                return null;
            }

            var dataMemberRequired = (DataMemberAttribute)propertyInfo.GetCustomAttribute(
                typeof(DataMemberAttribute),
                inherit: true);

            if (dataMemberRequired != null && dataMemberRequired.IsRequired)
            {
                return null;
            }

            return new ValidationError()
            {
                ModelType = propertyInfo.DeclaringType,
                PropertyName = propertyInfo.Name
            };
        }

        private bool ExcludeTypeFromValidation(Type modelType)
        {
            return TypeHelper.IsSimpleType(modelType);
        }

        private class ValidationError
        {
            public Type ModelType { get; set; }

            public string PropertyName { get; set; }
        }
    }
}
