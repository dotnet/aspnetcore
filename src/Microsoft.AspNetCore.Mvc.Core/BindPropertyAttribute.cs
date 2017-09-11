// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class BindPropertyAttribute : Attribute, IModelNameProvider, IBinderTypeProviderMetadata, IRequestPredicateProvider
    {
        private static readonly Func<ActionContext, bool> _supportsAllRequests = (c) => true;

        private static readonly Func<ActionContext, bool> _supportsNonGetRequests = IsNonGetRequest;

        private BindingSource _bindingSource;

        public bool SupportsGet { get; set; }

        public Type BinderType { get; set; }

        /// <inheritdoc />
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
            return !string.Equals(context.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
        }
    }
}
