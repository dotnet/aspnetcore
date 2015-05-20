// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Used to override an <see cref="ITagHelper"/> property's HTML attribute name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class HtmlAttributeNameAttribute : Attribute
    {
        private string _dictionaryAttributePrefix;

        /// <summary>
        /// Instantiates a new instance of the <see cref="HtmlAttributeNameAttribute"/> class.
        /// </summary>
        /// <param name="name">HTML attribute name for the associated property.</param>
        public HtmlAttributeNameAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// HTML attribute name of the associated property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the prefix used to match HTML attribute names. Matching attributes are added to the
        /// associated property (an <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>).
        /// </summary>
        /// <remarks>
        /// If non-<c>null</c> associated property must be compatible with
        /// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> where <c>TKey</c> is
        /// <see cref="string"/>.
        /// </remarks>
        /// <value>
        /// If associated property is compatible with
        /// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>, default value is <c>Name + "-"</c>.
        /// Otherwise default value is <c>null</c>.
        /// </value>
        public string DictionaryAttributePrefix
        {
            get
            {
                return _dictionaryAttributePrefix;
            }
            set
            {
                _dictionaryAttributePrefix = value;
                DictionaryAttributePrefixSet = true;
            }
        }

        /// <summary>
        /// Gets an indication whether <see cref="DictionaryAttributePrefix"/> has been set. Used to distinguish an
        /// uninitialized <see cref="DictionaryAttributePrefix"/> value from an explicit <c>null</c> setting.
        /// </summary>
        /// <value><c>true</c> if <see cref="DictionaryAttributePrefix"/> was set. <c>false</c> otherwise.</value>
        public bool DictionaryAttributePrefixSet { get; private set; }
    }
}