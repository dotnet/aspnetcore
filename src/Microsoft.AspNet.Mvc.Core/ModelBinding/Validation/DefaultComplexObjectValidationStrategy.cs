// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// The default implementation of <see cref="IValidationStrategy"/> for a complex object.
    /// </summary>
    public class DefaultComplexObjectValidationStrategy : IValidationStrategy
    {
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

            public ValidationEntry Current
            {
                get
                {
                    return _entry;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

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
                var model = property.PropertyGetter(_model);

                _entry = new ValidationEntry(property, key, model);

                return true;
            }

            public void Dispose()
            {
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
