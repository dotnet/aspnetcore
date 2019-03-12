// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An attribute that can specify a model name or type of <see cref="IModelBinder"/> to use for binding.
    /// </summary>
    [AttributeUsage(

        // Support method parameters in actions.
        AttributeTargets.Parameter |

        // Support properties on model DTOs.
        AttributeTargets.Property |

        // Support model types.
        AttributeTargets.Class |
        AttributeTargets.Enum |
        AttributeTargets.Struct,

        AllowMultiple = false,
        Inherited = true)]
    public class ModelBinderAttribute : Attribute, IModelNameProvider, IBinderTypeProviderMetadata
    {
        private BindingSource _bindingSource;
        private Type _binderType;

        /// <summary>
        /// Initializes a new instance of <see cref="ModelBinderAttribute"/>.
        /// </summary>
        public ModelBinderAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ModelBinderAttribute"/>.
        /// </summary>
        /// <param name="binderType">A <see cref="Type"/> which implements <see cref="IModelBinder"/>.</param>
        /// <remarks>
        /// Subclass this attribute and set <see cref="BindingSource"/> if <see cref="BindingSource.Custom"/> is not
        /// correct for the specified <paramref name="binderType"/>.
        /// </remarks>
        public ModelBinderAttribute(Type binderType)
        {
            if (binderType == null)
            {
                throw new ArgumentNullException(nameof(binderType));
            }

            BinderType = binderType;
        }

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
            protected set
            {
                _bindingSource = value;
            }
        }

        /// <inheritdoc />
        public string Name { get; set; }
    }
}
