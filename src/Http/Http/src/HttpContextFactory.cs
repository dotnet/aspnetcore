// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Represents methods used to create an HTTP context object. 
    /// </summary>
    [Obsolete("This is obsolete and will be removed in a future version. Use DefaultHttpContextFactory instead.")]
    public class HttpContextFactory : IHttpContextFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FormOptions _formOptions;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        /// Initializes a new instance of the HttpContext class with options passed in.
        /// </summary>
        /// <param name="formOptions">Options to set when instantianting the HTTP context object.</param>
        public HttpContextFactory(IOptions<FormOptions> formOptions)
            : this(formOptions, serviceScopeFactory: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DefaultHttpContext class with options passed in.
        /// </summary>
        /// <param name="formOptions">Options to set when instantianting the HTTP context object.</param>
        /// <param name="serviceScopeFactory">Factory object used to create the service scope for the HTTP context.</param>
        public HttpContextFactory(IOptions<FormOptions> formOptions, IServiceScopeFactory serviceScopeFactory)
            : this(formOptions, serviceScopeFactory, httpContextAccessor: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DefaultHttpContext class with options passed in.
        /// </summary>
        /// <param name="formOptions">Options to set when instantianting the HTTP context object.</param>
        /// <param name="httpContextAccessor">Object to be used to access the HTTP context instance.</param>
        public HttpContextFactory(IOptions<FormOptions> formOptions, IHttpContextAccessor httpContextAccessor)
            : this(formOptions, serviceScopeFactory: null, httpContextAccessor: httpContextAccessor)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DefaultHttpContext class with options passed in.
        /// </summary>
        /// <param name="formOptions">Options to set when instantianting the HTTP context object.</param>
        /// <param name="serviceScopeFactory">Factory object used to create the service scope for the HTTP context.</param>
        /// <param name="httpContextAccessor">Options to set when instantianting the Default HTTP context object.</param>
        public HttpContextFactory(IOptions<FormOptions> formOptions, IServiceScopeFactory serviceScopeFactory, IHttpContextAccessor httpContextAccessor)
        {
            if (formOptions == null)
            {
                throw new ArgumentNullException(nameof(formOptions));
            }

            if (serviceScopeFactory == null)
            {
                throw new ArgumentNullException(nameof(serviceScopeFactory));
            }

            _formOptions = formOptions.Value;
            _serviceScopeFactory = serviceScopeFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Initializes a new instance of the DefaultHttpContext class with options passed in.
        /// </summary>
        /// <param name="featureCollection">Options to set when instantianting the Default HTTP context object.</param>
        public HttpContext Create(IFeatureCollection featureCollection)
        {
            if (featureCollection == null)
            {
                throw new ArgumentNullException(nameof(featureCollection));
            }

            var httpContext = new DefaultHttpContext(featureCollection);
            if (_httpContextAccessor != null)
            {
                _httpContextAccessor.HttpContext = httpContext;
            }

            httpContext.FormOptions = _formOptions;
            httpContext.ServiceScopeFactory = _serviceScopeFactory;

            return httpContext;
        }

        /// <summary>
        /// Sets the HTTP context object to null for garbage collection. 
        /// </summary>
        /// <param name="httpContext">HTTP context to dispose.</param>
        public void Dispose(HttpContext httpContext)
        {
            if (_httpContextAccessor != null)
            {
                _httpContextAccessor.HttpContext = null;
            }
        }
    }
}
