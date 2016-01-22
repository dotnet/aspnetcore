// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Html;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    public class FormContext
    {
        private readonly Dictionary<string, bool> _renderedFields =
            new Dictionary<string, bool>(StringComparer.Ordinal);
        private Dictionary<string, object> _formData;
        private IList<IHtmlContent> _endOfFormContent;

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

        public bool HasAntiforgeryToken { get; set; }

        public bool HasFormData => _formData != null;

        public bool HasEndOfFormContent => _endOfFormContent != null;

        public IList<IHtmlContent> EndOfFormContent
        {
            get
            {
                if (_endOfFormContent == null)
                {
                    _endOfFormContent = new List<IHtmlContent>();
                }

                return _endOfFormContent;
            }
        }

        public bool CanRenderAtEndOfForm { get; set; }

        public bool RenderedField(string fieldName)
        {
            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            bool result;
            _renderedFields.TryGetValue(fieldName, out result);

            return result;
        }

        public void RenderedField(string fieldName, bool value)
        {
            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            _renderedFields[fieldName] = value;
        }
    }
}
