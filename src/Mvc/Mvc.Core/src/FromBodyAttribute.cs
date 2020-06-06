// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using the request body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromBodyAttribute : Attribute, IBindingSourceMetadata, IAllowEmptyInputInBodyModelBinding
    {
        private bool? _allowEmptyInputInBodyModelBinding;

        /// <inheritdoc />
        public BindingSource BindingSource => BindingSource.Body;

        /// <summary>
        /// Gets or sets the flag which decides whether body model binding (for example, on an
        /// action method parameter with <see cref="FromBodyAttribute"/>) should treat empty
        /// input as valid. <see langword="null"/> by default.
        /// </summary>
        /// <remarks>
        /// When configured, takes precedence over <see cref="MvcOptions.AllowEmptyInputInBodyModelBinding"/>.
        /// </remarks>
        public bool AllowEmptyInputInBodyModelBinding
        {
            get => _allowEmptyInputInBodyModelBinding ?? false;
            set => _allowEmptyInputInBodyModelBinding = value;
        }

        bool? IAllowEmptyInputInBodyModelBinding.AllowEmptyInputInBodyModelBinding => _allowEmptyInputInBodyModelBinding;
    }
}
