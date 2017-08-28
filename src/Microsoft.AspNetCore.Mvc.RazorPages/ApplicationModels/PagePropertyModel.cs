// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Represents a property in a <see cref="PageApplicationModel"/>.
    /// </summary>
    [DebuggerDisplay("PagePropertyModel: Name={PropertyName}")]
    public class PagePropertyModel : ICommonModel, IBindingModel
    {
        /// <summary>
        /// Creates a new instance of <see cref="PagePropertyModel"/>.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> for the underlying property.</param>
        /// <param name="attributes">Any attributes which are annotated on the property.</param>
        public PagePropertyModel(
            PropertyInfo propertyInfo,
            IReadOnlyList<object> attributes)
        {
            PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
            Properties = new Dictionary<object, object>();
            Attributes = new List<object>(attributes) ?? throw new ArgumentNullException(nameof(attributes));
        }

        /// <summary>
        /// Creates a new instance of <see cref="PagePropertyModel"/> from a given <see cref="PagePropertyModel"/>.
        /// </summary>
        /// <param name="other">The <see cref="PagePropertyModel"/> which needs to be copied.</param>
        public PagePropertyModel(PagePropertyModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Page = other.Page;
            Attributes = new List<object>(other.Attributes);
            BindingInfo = BindingInfo == null ? null : new BindingInfo(other.BindingInfo);
            PropertyInfo = other.PropertyInfo;
            PropertyName = other.PropertyName;
            Properties = new Dictionary<object, object>(other.Properties);
        }

        /// <summary>
        /// Gets or sets the <see cref="PageApplicationModel"/> this <see cref="PagePropertyModel"/> is associated with.
        /// </summary>
        public PageApplicationModel Page { get; set; }

        /// <summary>
        /// Gets any attributes which are annotated on the property.
        /// </summary>
        public IReadOnlyList<object> Attributes { get; }

        /// <inheritdoc />
        public IDictionary<object, object> Properties { get; }

        /// <summary>
        /// Gets or sets the <see cref="BindingInfo"/> associated with this model.
        /// </summary>
        public BindingInfo BindingInfo { get; set; }

        /// <summary>
        /// Gets the underlying <see cref="PropertyInfo"/>.
        /// </summary>
        public PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// Gets or sets the name of the property represented by this model.
        /// </summary>
        public string PropertyName { get; set; }

        MemberInfo ICommonModel.MemberInfo => PropertyInfo;

        string ICommonModel.Name => PropertyName;
    }
}