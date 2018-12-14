// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    internal abstract class SaveTempDataPropertyFilterBase : ISaveTempDataCallback
    {
        protected readonly ITempDataDictionaryFactory _tempDataFactory;

        public SaveTempDataPropertyFilterBase(ITempDataDictionaryFactory tempDataFactory)
        {
            _tempDataFactory = tempDataFactory;
        }

        /// <summary>
        /// Describes the temp data properties which exist on <see cref="Subject"/>
        /// </summary>
        public IReadOnlyList<LifecycleProperty> Properties { get; set; }

        /// <summary>
        /// The <see cref="object"/> which has the temp data properties.
        /// </summary>
        public object Subject { get; set; }

        /// <summary>
        /// Tracks the values which originally existed in temp data.
        /// </summary>
        public IDictionary<PropertyInfo, object> OriginalValues { get; } = new Dictionary<PropertyInfo, object>();

        /// <summary>
        /// Puts the modified values of <see cref="Subject"/> into <paramref name="tempData"/>.
        /// </summary>
        /// <param name="tempData">The <see cref="ITempDataDictionary"/> to be updated.</param>
        public void OnTempDataSaving(ITempDataDictionary tempData)
        {
            if (Subject == null)
            {
                return;
            }

            for (var i = 0; i < Properties.Count; i++)
            {
                var property = Properties[i];
                OriginalValues.TryGetValue(property.PropertyInfo, out var originalValue);

                var newValue = property.GetValue(Subject);
                if (newValue != null && !newValue.Equals(originalValue))
                {
                    tempData[property.Key] = newValue;
                }
            }
        }

        /// <summary>
        /// Sets the values of the properties of <see cref="Subject"/> from <paramref name="tempData"/>.
        /// </summary>
        /// <param name="tempData">The <see cref="ITempDataDictionary"/>.</param>
        protected void SetPropertyValues(ITempDataDictionary tempData)
        {
            if (Properties == null)
            {
                return;
            }

            Debug.Assert(Subject != null, "Subject must be set before this method is invoked.");

            for (var i = 0; i < Properties.Count; i++)
            {
                var property = Properties[i];
                var value = tempData[property.Key];

                OriginalValues[property.PropertyInfo] = value;
                property.SetValue(Subject, value);
            }
        }

        public static IReadOnlyList<LifecycleProperty> GetTempDataProperties(Type type, MvcViewOptions viewOptions)
        {
            List<LifecycleProperty> results = null;

            var propertyHelpers = PropertyHelper.GetVisibleProperties(type: type);
            for (var i = 0; i < propertyHelpers.Length; i++)
            {
                var propertyHelper = propertyHelpers[i];
                var property = propertyHelper.Property;
                var tempDataAttribute = property.GetCustomAttribute<TempDataAttribute>();
                if (tempDataAttribute != null)
                {
                    ValidateProperty(propertyHelper.Property);
                    if (results == null)
                    {
                        results = new List<LifecycleProperty>();
                    }

                    var key = tempDataAttribute.Key;
                    if (string.IsNullOrEmpty(key))
                    {
                        key = property.Name;
                    }

                    results.Add(new LifecycleProperty(property, key));
                }
            }

            return results;
        }

        private static void ValidateProperty(PropertyInfo property)
        {
            if (!(property.SetMethod != null &&
                property.SetMethod.IsPublic &&
                property.GetMethod != null &&
                property.GetMethod.IsPublic))
            {
                throw new InvalidOperationException(
                    Resources.FormatTempDataProperties_PublicGetterSetter(property.DeclaringType.FullName, property.Name, nameof(TempDataAttribute)));
            }

            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (!TempDataSerializer.CanSerializeType(propertyType, out var errorMessage))
            {
                var messageWithPropertyInfo = Resources.FormatTempDataProperties_InvalidType(
                    property.DeclaringType.FullName,
                    property.Name,
                    nameof(TempDataAttribute));

                throw new InvalidOperationException($"{messageWithPropertyInfo} {errorMessage}");
            }
        }
    }
}
