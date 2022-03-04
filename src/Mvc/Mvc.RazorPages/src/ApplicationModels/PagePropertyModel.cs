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
    public class PagePropertyModel : ParameterModelBase, ICommonModel
    {
        /// <summary>
        /// Creates a new instance of <see cref="PagePropertyModel"/>.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> for the underlying property.</param>
        /// <param name="attributes">Any attributes which are annotated on the property.</param>
        public PagePropertyModel(
            PropertyInfo propertyInfo,
            IReadOnlyList<object> attributes)
            : base(propertyInfo?.PropertyType, attributes)
        {
            PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        }

        /// <summary>
        /// Creates a new instance of <see cref="PagePropertyModel"/> from a given <see cref="PagePropertyModel"/>.
        /// </summary>
        /// <param name="other">The <see cref="PagePropertyModel"/> which needs to be copied.</param>
        public PagePropertyModel(PagePropertyModel other)
            : base(other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Page = other.Page;
            BindingInfo = BindingInfo == null ? null : new BindingInfo(other.BindingInfo);
            PropertyInfo = other.PropertyInfo;
        }

        /// <summary>
        /// Gets or sets the <see cref="PageApplicationModel"/> this <see cref="PagePropertyModel"/> is associated with.
        /// </summary>
        public PageApplicationModel Page { get; set; }

        MemberInfo ICommonModel.MemberInfo => PropertyInfo;

        public PropertyInfo PropertyInfo { get; }

        public string PropertyName
        {
            get => Name;
            set => Name = value;
        }
    }
}