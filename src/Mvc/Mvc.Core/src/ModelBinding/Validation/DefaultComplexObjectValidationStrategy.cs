// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// The default implementation of <see cref="IValidationStrategy"/> for a complex object.
    /// </summary>
    internal class DefaultComplexObjectValidationStrategy : IValidationStrategy
    {
        private static readonly bool IsMono = Type.GetType("Mono.Runtime") != null;

        /// <summary>
        /// Gets an instance of <see cref="DefaultComplexObjectValidationStrategy"/>.
        /// </summary>
        public static readonly IValidationStrategy Instance = new DefaultComplexObjectValidationStrategy();

        private DefaultComplexObjectValidationStrategy()
        {
        }

        /// <inheritdoc />
        public IEnumerator<ValidationEntry> GetChildren(
            ModelMetadata metadata,
            string key,
            object model)
        {
            return new Enumerator(metadata.Properties, key, model);
        }

        private class Enumerator : IEnumerator<ValidationEntry>
        {
            private readonly string _key;
            private readonly object _model;
            private readonly ModelPropertyCollection _properties;

            private ValidationEntry _entry;
            private int _index;

            public Enumerator(
                ModelPropertyCollection properties,
                string key,
                object model)
            {
                _properties = properties;
                _key = key;
                _model = model;

                _index = -1;
            }

            public ValidationEntry Current => _entry;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _index++;
                if (_index >= _properties.Count)
                {
                    return false;
                }

                var property = _properties[_index];
                var propertyName = property.BinderModelName ?? property.PropertyName;
                var key = ModelNames.CreatePropertyModelName(_key, propertyName);

                if (_model == null)
                {
                    // Performance: Never create a delegate when container is null.
                    _entry = new ValidationEntry(property, key, model: null);
                }
                else if (IsMono)
                {
                    _entry = new ValidationEntry(property, key, () => GetModelOnMono(_model, property.PropertyName));
                }
                else
                {
                    _entry = new ValidationEntry(property, key, () => GetModel(_model, property));
                }

                return true;
            }

            public void Dispose()
            {
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            private static object GetModel(object container, ModelMetadata property)
            {
                return property.PropertyGetter(container);
            }

            // Our property accessors don't work on Mono 4.0.4 - see https://github.com/aspnet/External/issues/44
            // This is a workaround for what the PropertyGetter does in the background.
            private static object GetModelOnMono(object container, string propertyName)
            {
                var propertyInfo = container.GetType().GetRuntimeProperty(propertyName);
                try
                {
                    return propertyInfo.GetValue(container);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
        }
    }
}
