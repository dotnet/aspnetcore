// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
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
        private static readonly Func<ActionContext, bool> _supportsAllRequests = (c) => true;
        private static readonly Func<ActionContext, bool> _supportsNonGetRequests = IsNonGetRequest;

        private BindingSource _bindingSource;
        private Type _binderType;

        /// <summary>
        /// Gets or sets an indication the associated property should be bound in HTTP GET requests. If
        /// <see langword="true"/>, the property should be bound in all requests. Otherwise, the property should not be
        /// bound in HTTP GET requests.
        /// </summary>
        /// <value>Defaults to <see langword="false"/>.</value>
        public bool SupportsGet { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Subclass this attribute and set <see cref="BindingSource"/> if <see cref="BindingSource.Custom"/> is not
        /// correct for the specified (non-<see langword="null"/>) <see cref="IModelBinder"/> implementation.
        /// </remarks>
        public Type BinderType
        {
            get => _binderType;
            set
            {
                if (value != null && !typeof(IModelBinder).IsAssignableFrom(value))
                {
                    throw new ArgumentException(
                        Resources.FormatBinderType_MustBeIModelBinder(
                            value.FullName,
                            typeof(IModelBinder).FullName),
                        nameof(value));
                }

                _binderType = value;
            }
        }

        /// <inheritdoc />
        /// <value>
        /// If <see cref="BinderType"/> is <see langword="null"/>, defaults to <see langword="null"/>. Otherwise,
        /// defaults to <see cref="BindingSource.Custom"/>. May be overridden in a subclass.
        /// </value>
        public virtual BindingSource BindingSource
        {
            get
            {
                if (_bindingSource == null && BinderType != null)
                {
                    return BindingSource.Custom;
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
            return !HttpMethods.IsGet(context.HttpContext.Request.Method);
        }
    }
}
