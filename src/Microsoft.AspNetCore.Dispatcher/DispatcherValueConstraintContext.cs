// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// A context object for <see cref="IDispatcherValueConstraint"/>.
    /// </summary>
    public class DispatcherValueConstraintContext
    {
        private HttpContext _httpContext;
        private DispatcherValueCollection _values;
        private ConstraintPurpose _purpose;
        private string _key;

        /// <summary>
        /// Creates a new <see cref="DispatcherValueConstraintContext"/>.
        /// </summary>
        public DispatcherValueConstraintContext()
        {
        }

        /// <summary>
        /// Creates a new <see cref="DispatcherValueConstraintContext"/> for the current request.
        /// </summary>
        /// <param name="httpContext">The <see cref="Http.HttpContext"/> associated with the current request.</param>
        /// <param name="values">The <see cref="DispatcherValueCollection"/> for the current operation.</param>
        /// <param name="purpose">The purpose for invoking the constraint.</param>
        public DispatcherValueConstraintContext(HttpContext httpContext, DispatcherValueCollection values, ConstraintPurpose purpose)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            HttpContext = httpContext;
            Values = values;
            Purpose = purpose;
        }

        /// <summary>
        /// Gets or sets the <see cref="Http.HttpContext"/> associated with the current request.
        /// </summary>
        public HttpContext HttpContext
        {
            get => _httpContext;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _httpContext = value;
            }
        }

        /// <summary>
        /// Gets or sets the key associated with the current <see cref="IDispatcherValueConstraint"/>.
        /// </summary>
        public string Key
        {
            get => _key;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _key = value;
            }
        }

        /// <summary>
        /// Gets or sets the purpose of executing the <see cref="IDispatcherValueConstraint"/>.
        /// </summary>
        public ConstraintPurpose Purpose
        {
            get => _purpose;
            set => _purpose = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="DispatcherValueCollection"/> associated with the current operation.
        /// </summary>
        public DispatcherValueCollection Values
        {
            get => _values;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _values = value;
            }
        }
    }
}
