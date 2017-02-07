// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class TempDataPropertyProvider
    {
        public static readonly string Prefix = "TempDataProperty-";

        private ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _subjectProperties =
            new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();

        public IDictionary<PropertyInfo, object> LoadAndTrackChanges(object subject, ITempDataDictionary tempData)
        {
            var properties = GetSubjectProperties(subject);
            var result = new Dictionary<PropertyInfo, object>();

            foreach (var property in properties)
            {
                var value = tempData[Prefix + property.Name];

                result[property] = value;

                // TODO: Clarify what behavior should be for null values here
                if (value != null && property.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    property.SetValue(subject, value);
                }
            }

            return result;
        }

        private IEnumerable<PropertyInfo> GetSubjectProperties(object subject)
        {
            return _subjectProperties.GetOrAdd(subject.GetType(), subjectType =>
            {
                var properties = subjectType.GetRuntimeProperties()
                    .Where(pi => pi.GetCustomAttribute<TempDataAttribute>() != null);

                if (properties.Any(pi => !(pi.SetMethod != null && pi.SetMethod.IsPublic && pi.GetMethod != null && pi.GetMethod.IsPublic)))
                {
                    throw new InvalidOperationException("TempData properties must have a public getter and setter.");
                }

                if (properties.Any(pi => !(pi.PropertyType.GetTypeInfo().IsPrimitive || pi.PropertyType == typeof(string))))
                {
                    throw new InvalidOperationException("TempData properties must be declared as primitive types or string only.");
                }

                return properties;
            });
        }
    }
}