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
    /// A type which is used to represent a property in a <see cref="ControllerModel"/>.
    /// </summary>
    [DebuggerDisplay("PropertyModel: Name={PropertyName}")]
    public class PropertyModel : ParameterModelBase, ICommonModel, IBindingModel
    {
        /// <summary>
        /// Creates a new instance of <see cref="PropertyModel"/>.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> for the underlying property.</param>
        /// <param name="attributes">Any attributes which are annotated on the property.</param>
        public PropertyModel(
            PropertyInfo propertyInfo,
            IReadOnlyList<object> attributes)
            : base(propertyInfo?.PropertyType, attributes)
        {
            PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        }

        /// <summary>
        /// Creates a new instance of <see cref="PropertyModel"/> from a given <see cref="PropertyModel"/>.
        /// </summary>
        /// <param name="other">The <see cref="PropertyModel"/> which needs to be copied.</param>
        public PropertyModel(PropertyModel other)
            : base(other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Controller = other.Controller;
            BindingInfo = BindingInfo == null ? null : new BindingInfo(other.BindingInfo);
            PropertyInfo = other.PropertyInfo;
        }

        /// <summary>
        /// Gets or sets the <see cref="ControllerModel"/> this <see cref="PropertyModel"/> is associated with.
        /// </summary>
        public ControllerModel Controller { get; set; }

        MemberInfo ICommonModel.MemberInfo => PropertyInfo;

        public new IDictionary<object, object> Properties => base.Properties;

        public new IReadOnlyList<object> Attributes => base.Attributes;

        public PropertyInfo PropertyInfo { get; }

        public string PropertyName
        {
            get => Name;
            set => Name = value;
        }
    }
}
