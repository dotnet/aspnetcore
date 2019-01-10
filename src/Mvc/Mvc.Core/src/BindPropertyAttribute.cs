// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An attribute that can specify a model name or type of <see cref="IModelBinder"/> to use for binding the
    /// associated property.
    /// </summary>
    /// <remarks>
    /// Similar to <see cref="ModelBinderAttribute"/>. Unlike that attribute, <see cref="BindPropertyAttribute"/>
    /// applies only to properties and adds an <see cref="IRequestPredicateProvider"/> implementation that by default
    /// indicates the property should not be bound for HTTP GET requests (see also <see cref="SupportsGet"/>).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class BindPropertyAttribute : Attribute, IModelNameProvider, IBinderTypeProviderMetadata, IRequestPredicateProvider
    {
        private static readonly BindingSource[] BindingSources = new[]
        {
            BindingSource.Body,
            BindingSource.Custom,
            BindingSource.Form,
            BindingSource.FormFile,
            BindingSource.Header,
            BindingSource.ModelBinding,
            BindingSource.Path,
            BindingSource.Query,
            BindingSource.Services,
            BindingSource.Special,
        };

        private static readonly Func<ActionContext, bool> _supportsAllRequests = (c) => true;

        private static readonly Func<ActionContext, bool> _supportsNonGetRequests = IsNonGetRequest;

        private BindingSource _bindingSource;

        /// <summary>
        /// Initializes a new instance of <see cref="BindPropertyAttribute"/>.
        /// </summary>
        /// <remarks>
        /// If setting <see cref="BinderType"/> to an <see cref="IModelBinder"/> implementation that does not use values
        /// from form data, route values or the query string, instead use the
        /// <see cref="BindPropertyAttribute(BindingSourceKey)"/> constructor to set <see cref="BindingSource"/>.
        /// </remarks>
        public BindPropertyAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BindPropertyAttribute"/>.
        /// </summary>
        /// <param name="bindingSource">
        /// The <see cref="BindingSourceKey"/> that indicates the value of the
        /// <see cref="BindingSource"/> property.
        /// </param>
        public BindPropertyAttribute(BindingSourceKey bindingSource)
        {
            var sourcesIndex = (int)bindingSource;
            if (!Enum.IsDefined(typeof(BindingSourceKey), bindingSource))
            {
                throw new InvalidEnumArgumentException(nameof(bindingSource), sourcesIndex, typeof(BindingSourceKey));
            }

            _bindingSource = BindingSources[sourcesIndex];
        }

        /// <summary>
        /// Gets or sets an indication the associated property should be bound in HTTP GET requests. If
        /// <see langword="true"/>, the property should be bound in all requests. Otherwise, the property should not be
        /// bound in HTTP GET requests.
        /// </summary>
        /// <value>Defaults to <see langword="false"/>.</value>
        public bool SupportsGet { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Use the <see cref="BindPropertyAttribute(BindingSourceKey)"/> constructor to set
        /// <see cref="BindingSource"/> if the specified <see cref="IModelBinder"/> implementation does not use values
        /// from form data, route values or the query string.
        /// </remarks>
        public Type BinderType { get; set; }

        /// <inheritdoc />
        /// <value>
        /// If <see cref="BinderType"/> is <see langword="null"/>, defaults to <see langword="null"/>. Otherwise,
        /// defaults to <see cref="BindingSource.ModelBinding"/>. May be overridden using the
        /// <see cref="BindPropertyAttribute(BindingSourceKey)"/> constructor or in a subclass.
        /// </value>
        public virtual BindingSource BindingSource
        {
            get
            {
                if (_bindingSource == null && BinderType != null)
                {
                    return BindingSource.ModelBinding;
                }

                return _bindingSource;
            }
            protected set => _bindingSource = value;
        }

        /// <inheritdoc />
        public string Name { get; set; }

        Func<ActionContext, bool> IRequestPredicateProvider.RequestPredicate
            => SupportsGet ? _supportsAllRequests : _supportsNonGetRequests;

        private static bool IsNonGetRequest(ActionContext context)
        {
            return !string.Equals(context.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
        }
    }
}
