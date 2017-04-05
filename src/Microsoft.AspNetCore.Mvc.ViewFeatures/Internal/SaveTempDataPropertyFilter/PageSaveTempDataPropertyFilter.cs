// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class PageSaveTempDataPropertyFilter : SaveTempDataPropertyFilterBase
    {
        public PageSaveTempDataPropertyFilter(ITempDataDictionaryFactory factory)
            : base(factory)
        {
        }

        public PageSaveTempDataPropertyFilterFactory FilterFactory { get; set; }

        public override object Subject {
            get => base.Subject;
            set
            {
                base.Subject = value;
                SetTempDataProperties(value.GetType());
            }
        }
        
        private void SetTempDataProperties(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (FilterFactory == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatPropertyOfTypeCannotBeNull(
                        nameof(FilterFactory),
                        typeof(PageSaveTempDataPropertyFilter).Name));
            }

            TempDataProperties = FilterFactory.GetTempDataProperties(type);
        }

        /// <summary>
        /// Applies values from TempData from <paramref name="httpContext"/> to the
        /// <see cref="SaveTempDataPropertyFilterBase.Subject"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> used to find TempData.</param>
        public void ApplyTempDataChanges(HttpContext httpContext)
        {
            if (Subject == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatPropertyOfTypeCannotBeNull(
                        nameof(Subject),
                        typeof(PageSaveTempDataPropertyFilter).Name));
            }

            var tempData = _factory.GetTempData(httpContext);

            SetPropertyVaules(tempData, Subject);
        }
    }
}
