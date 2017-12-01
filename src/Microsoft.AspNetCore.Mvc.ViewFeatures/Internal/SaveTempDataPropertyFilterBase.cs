// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public abstract class SaveTempDataPropertyFilterBase : ISaveTempDataCallback
    {
        protected const string Prefix = "TempDataProperty-";

        protected readonly ITempDataDictionaryFactory _factory;

        /// <summary>
        /// Describes the temp data properties which exist on <see cref="Subject"/>
        /// </summary>
        public IList<TempDataProperty> Properties { get; set; }

        /// <summary>
        /// The <see cref="object"/> which has the temp data properties.
        /// </summary>
        public object Subject { get; set; }

        /// <summary>
        /// Tracks the values which originally existed in temp data.
        /// </summary>
        public IDictionary<PropertyInfo, object> OriginalValues { get; } = new Dictionary<PropertyInfo, object>();

        public SaveTempDataPropertyFilterBase(ITempDataDictionaryFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Puts the modified values of <see cref="Subject"/> into <paramref name="tempData"/>.
        /// </summary>
        /// <param name="tempData">The <see cref="ITempDataDictionary"/> to be updated.</param>
        public void OnTempDataSaving(ITempDataDictionary tempData)
        {
            if (Subject != null && Properties != null)
            {
                for (var i = 0; i < Properties.Count; i++)
                {
                    var property = Properties[i];
                    OriginalValues.TryGetValue(property.PropertyInfo, out var originalValue);

                    var newValue = property.GetValue(Subject);
                    if (newValue != null && !newValue.Equals(originalValue))
                    {
                        tempData[property.TempDataKey] = newValue;
                    }
                }
            }
        }

        public static IList<TempDataProperty> GetTempDataProperties(Type type)
        {
            List<TempDataProperty> results = null;

            var propertyHelpers = PropertyHelper.GetVisibleProperties(type: type);

            for (var i = 0; i < propertyHelpers.Length; i++)
            {
                var propertyHelper = propertyHelpers[i];
                if (propertyHelper.Property.IsDefined(typeof(TempDataAttribute)))
                {
                    ValidateProperty(propertyHelper);
                    if (results == null)
                    {
                        results = new List<TempDataProperty>();
                    }

                    results.Add(new TempDataProperty(
                        Prefix + propertyHelper.Name,
                        propertyHelper.Property,
                        propertyHelper.GetValue,
                        propertyHelper.SetValue));
                }
            }

            return results;
        }

        private static void ValidateProperty(PropertyHelper propertyHelper)
        {
            var property = propertyHelper.Property;
            if (!(property.SetMethod != null &&
                property.SetMethod.IsPublic &&
                property.GetMethod != null &&
                property.GetMethod.IsPublic))
            {
                throw new InvalidOperationException(
                    Resources.FormatTempDataProperties_PublicGetterSetter(property.DeclaringType.FullName, property.Name, nameof(TempDataAttribute)));
            }

            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (!(propertyType.GetTypeInfo().IsPrimitive || propertyType == typeof(string)))
            {
                throw new InvalidOperationException(
                    Resources.FormatTempDataProperties_PrimitiveTypeOrString(property.DeclaringType.FullName, property.Name, nameof(TempDataAttribute)));
            }
        }

        /// <summary>
        /// Sets the values of the properties of <paramref name="subject"/> from <paramref name="tempData"/>.
        /// </summary>
        /// <param name="tempData">The <see cref="ITempDataDictionary"/> with the data to set on <paramref name="subject"/>.</param>
        /// <param name="subject">The <see cref="object"/> which will have it's properties set.</param>
        protected void SetPropertyVaules(ITempDataDictionary tempData, object subject)
        {
            if (Properties == null)
            {
                return;
            }

            for (var i = 0; i < Properties.Count; i++)
            {
                var property = Properties[i];
                var value = tempData[Prefix + property.PropertyInfo.Name];

                OriginalValues[property.PropertyInfo] = value;

                var propertyTypeInfo = property.PropertyInfo.PropertyType.GetTypeInfo();

                var isReferenceTypeOrNullable = !propertyTypeInfo.IsValueType || Nullable.GetUnderlyingType(property.GetType()) != null;
                if (value != null || isReferenceTypeOrNullable)
                {
                    property.SetValue(subject, value);
                }
            }
        }
    }
}
