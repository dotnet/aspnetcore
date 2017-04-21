// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class BindPropertyAttribute : Attribute, IModelNameProvider, IBinderTypeProviderMetadata
    {
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
            protected set
            {
                _bindingSource = value;
            }
        }

        /// <inheritdoc />
        public string Name { get; set; }
    }
}
