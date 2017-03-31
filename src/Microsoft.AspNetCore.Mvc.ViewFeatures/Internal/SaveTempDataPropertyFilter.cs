// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class SaveTempDataPropertyFilter : ISaveTempDataCallback, IActionFilter
    {
        private const string Prefix = "TempDataProperty-";
        private readonly ITempDataDictionaryFactory _factory;

        public SaveTempDataPropertyFilter(ITempDataDictionaryFactory factory)
        {
            _factory = factory;
        }

        // Cannot be public as <c>PropertyHelper</c> is an internal shared source type
        internal IList<PropertyHelper> PropertyHelpers { get; set; }

        public object Subject { get; set; }

        public IDictionary<PropertyInfo, object> OriginalValues { get; set; }

        /// <summary>
        /// Puts the modified values of <see cref="Subject"/> into <paramref name="tempData"/>.
        /// </summary>
        /// <param name="tempData">The <see cref="ITempDataDictionary"/> to be updated.</param>
        public void OnTempDataSaving(ITempDataDictionary tempData)
        {
            if (Subject != null && OriginalValues != null)
            {
                foreach (var kvp in OriginalValues)
                {
                    var property = kvp.Key;
                    var originalValue = kvp.Value;

                    var newValue = property.GetValue(Subject);
                    if (newValue != null && !newValue.Equals(originalValue))
                    {
                        tempData[Prefix + property.Name] = newValue;
                    }
                }
            }
        }

        /// <summary>
        /// Applies values from TempData from <paramref name="httpContext"/> to the <see cref="Subject"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> used to find TempData.</param>
        public void ApplyTempDataChanges(HttpContext httpContext)
        {
            if (Subject == null)
            {
                throw new ArgumentNullException(nameof(Subject));
            }

            var tempData = _factory.GetTempData(httpContext);

            if (OriginalValues == null)
            {
                OriginalValues = new Dictionary<PropertyInfo, object>();
            }

            SetPropertyVaules(tempData, Subject);
        }

        /// <inheritdoc />
        public void OnActionExecuting(ActionExecutingContext context)
        {
            Subject = context.Controller;
            var tempData = _factory.GetTempData(context.HttpContext);

            OriginalValues = new Dictionary<PropertyInfo, object>();

            SetPropertyVaules(tempData, Subject);
        }

        /// <inheritdoc />
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        private void SetPropertyVaules(ITempDataDictionary tempData, object subject)
        {
            if (PropertyHelpers == null)
            {
                return;
            }

            for (var i = 0; i < PropertyHelpers.Count; i++)
            {
                var property = PropertyHelpers[i];
                var value = tempData[Prefix + property.Name];

                OriginalValues[property.Property] = value;

                var propertyTypeInfo = property.Property.PropertyType.GetTypeInfo();

                var isReferenceTypeOrNullable = !propertyTypeInfo.IsValueType || Nullable.GetUnderlyingType(property.GetType()) != null;
                if (value != null || isReferenceTypeOrNullable)
                {
                    property.SetValue(subject, value);
                }
            }
        }
    }
}

