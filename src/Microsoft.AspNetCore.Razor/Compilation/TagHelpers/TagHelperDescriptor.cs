// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Compilation.TagHelpers
{
    /// <summary>
    /// A metadata class describing a tag helper.
    /// </summary>
    public class TagHelperDescriptor
    {
        private string _prefix = string.Empty;
        private string _tagName;
        private string _typeName;
        private string _assemblyName;
        private IDictionary<string, string> _propertyBag;
        private IEnumerable<TagHelperAttributeDescriptor> _attributes =
            Enumerable.Empty<TagHelperAttributeDescriptor>();
        private IEnumerable<TagHelperRequiredAttributeDescriptor> _requiredAttributes = 
            Enumerable.Empty<TagHelperRequiredAttributeDescriptor>();

        /// <summary>
        /// Text used as a required prefix when matching HTML start and end tags in the Razor source to available
        /// tag helpers.
        /// </summary>
        public string Prefix
        {
            get
            {
                return _prefix;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _prefix = value;
            }
        }

        /// <summary>
        /// The tag name that the tag helper should target.
        /// </summary>
        public string TagName
        {
            get
            {
                return _tagName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _tagName = value;
            }
        }

        /// <summary>
        /// The full tag name that is required for the tag helper to target an HTML element.
        /// </summary>
        /// <remarks>This is equivalent to <see cref="Prefix"/> and <see cref="TagName"/> concatenated.</remarks>
        public string FullTagName
        {
            get
            {
                return Prefix + TagName;
            }
        }

        /// <summary>
        /// The full name of the tag helper class.
        /// </summary>
        public string TypeName
        {
            get
            {
                return _typeName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _typeName = value;
            }
        }

        /// <summary>
        /// The name of the assembly containing the tag helper class.
        /// </summary>
        public string AssemblyName
        {
            get
            {
                return _assemblyName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _assemblyName = value;
            }
        }

        /// <summary>
        /// The list of attributes the tag helper expects.
        /// </summary>
        public IEnumerable<TagHelperAttributeDescriptor> Attributes
        {
            get
            {
                return _attributes;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _attributes = value;
            }
        }

        /// <summary>
        /// The list of required attribute names the tag helper expects to target an element.
        /// </summary>
        /// <remarks>
        /// <c>*</c> at the end of an attribute name acts as a prefix match.
        /// </remarks>
        public IEnumerable<TagHelperRequiredAttributeDescriptor> RequiredAttributes
        {
            get
            {
                return _requiredAttributes;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _requiredAttributes = value;
            }
        }

        /// <summary>
        /// Get the names of elements allowed as children.
        /// </summary>
        /// <remarks><c>null</c> indicates all children are allowed.</remarks>
        public IEnumerable<string> AllowedChildren { get; set; }

        /// <summary>
        /// Get the name of the HTML element required as the immediate parent.
        /// </summary>
        /// <remarks><c>null</c> indicates no restriction on parent tag.</remarks>
        public string RequiredParent { get; set; }

        /// <summary>
        /// The expected tag structure.
        /// </summary>
        /// <remarks>
        /// If <see cref="TagStructure.Unspecified"/> and no other tag helpers applying to the same element specify
        /// their <see cref="TagStructure"/> the <see cref="TagStructure.NormalOrSelfClosing"/> behavior is used:
        /// <para>
        /// <code>
        /// &lt;my-tag-helper&gt;&lt;/my-tag-helper&gt;
        /// &lt;!-- OR --&gt;
        /// &lt;my-tag-helper /&gt;
        /// </code>
        /// Otherwise, if another tag helper applying to the same element does specify their behavior, that behavior
        /// is used.
        /// </para>
        /// <para>
        /// If <see cref="TagStructure.WithoutEndTag"/> HTML elements can be written in the following formats:
        /// <code>
        /// &lt;my-tag-helper&gt;
        /// &lt;!-- OR --&gt;
        /// &lt;my-tag-helper /&gt;
        /// </code>
        /// </para>
        /// </remarks>
        public TagStructure TagStructure { get; set; }

        /// <summary>
        /// The <see cref="TagHelperDesignTimeDescriptor"/> that contains design time information about this
        /// tag helper.
        /// </summary>
        public TagHelperDesignTimeDescriptor DesignTimeDescriptor { get; set; }

        /// <summary>
        /// A dictionary containing additional information about the <see cref="TagHelperDescriptor"/>.
        /// </summary>
        public IDictionary<string, string> PropertyBag
        {
            get
            {
                if (_propertyBag == null)
                {
                    _propertyBag = new Dictionary<string, string>();
                }

                return _propertyBag;
            }
        }
    }
}