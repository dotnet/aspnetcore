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

        private BindingSource _bindingSource;
        private Type _binderType;

        /// <summary>
        /// Initializes a new instance of <see cref="ModelBinderAttribute"/>.
        /// </summary>
        /// <remarks>
        /// If setting <see cref="BinderType"/> to an <see cref="IModelBinder"/> implementation that does not use values
        /// from form data, route values or the query string, instead use the
        /// <see cref="ModelBinderAttribute(BindingSourceKey)"/> constructor to set <see cref="BindingSource"/>.
        /// </remarks>
        public ModelBinderAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ModelBinderAttribute"/>.
        /// </summary>
        /// <param name="bindingSource">
        /// The <see cref="BindingSourceKey"/> that indicates the value of the <see cref="BindingSource"/> property.
        /// </param>
        public ModelBinderAttribute(BindingSourceKey bindingSource)
        {
            var sourcesIndex = (int)bindingSource;
            if (!Enum.IsDefined(typeof(BindingSourceKey), bindingSource))
            {
                throw new InvalidEnumArgumentException(nameof(bindingSource), sourcesIndex, typeof(BindingSourceKey));
            }

            _bindingSource = BindingSources[sourcesIndex];
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ModelBinderAttribute"/>.
        /// </summary>
        /// <param name="binderType">A <see cref="Type"/> which implements <see cref="IModelBinder"/>.</param>
        /// <remarks>
        /// If the specified <paramref name="binderType" /> does not use values from form data, route values or the
        /// query string, instead use the <see cref="ModelBinderAttribute(BindingSourceKey)"/> constructor to set
        /// <see cref="BindingSource"/> and set the <see cref="BinderType"/> property.
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
        /// If the specified <see cref="IModelBinder"/> implementation does not use values from form data, route values
        /// or the query string, use the <see cref="ModelBinderAttribute(BindingSourceKey)"/> constructor to set
        /// <see cref="BindingSource"/>.
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
        /// defaults to <see cref="BindingSource.ModelBinding"/>. May be overridden using the
        /// <see cref="ModelBinderAttribute(BindingSourceKey)"/> constructor or in a subclass.
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
            protected set
            {
                _bindingSource = value;
            }
        }

        /// <inheritdoc />
        public string Name { get; set; }
    }
}
