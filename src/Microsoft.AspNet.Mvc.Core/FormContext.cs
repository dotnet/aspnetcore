// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class FormContext
    {
        private readonly Dictionary<string, bool> _renderedFields =
            new Dictionary<string, bool>(StringComparer.Ordinal);
        private Dictionary<string, object> _formData;

        /// <summary>
        /// Property bag for any information you wish to associate with a &lt;form/&gt; in an
        /// <see cref="Rendering.IHtmlHelper"/> implementation or extension method.
        /// </summary>
        public IDictionary<string, object> FormData
        {
            get
            {
                if (_formData == null)
                {
                    _formData = new Dictionary<string, object>(StringComparer.Ordinal);
                }

                return _formData;
            }
        }

        public bool RenderedField([NotNull] string fieldName)
        {
            bool result;
            _renderedFields.TryGetValue(fieldName, out result);

            return result;
        }

        public void RenderedField([NotNull] string fieldName, bool value)
        {
            _renderedFields[fieldName] = value;
        }
    }
}
