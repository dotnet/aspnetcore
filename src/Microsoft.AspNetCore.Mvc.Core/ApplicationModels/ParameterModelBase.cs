// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A model type for reading and manipulation properties and parameters.
    /// <para>
    /// Derived instances of this type represent properties and parameters for controllers, and Razor Pages.
    /// </para>
    /// </summary>
    public abstract class ParameterModelBase : IBindingModel
    {
        protected ParameterModelBase(
            Type parameterType,
            IReadOnlyList<object> attributes)
        {
            ParameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
            Attributes = new List<object>(attributes ?? throw new ArgumentNullException(nameof(attributes)));

            Properties = new Dictionary<object, object>();
        }

        protected ParameterModelBase(ParameterModelBase other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            ParameterType = other.ParameterType;
            Attributes = new List<object>(other.Attributes);
            BindingInfo = other.BindingInfo == null ? null : new BindingInfo(other.BindingInfo);
            Name = other.Name;
            Properties = new Dictionary<object, object>(other.Properties);
        }

        public IReadOnlyList<object> Attributes { get; }

        public IDictionary<object, object> Properties { get; }

        public Type ParameterType { get; }

        public string Name { get; protected set;  }

        public BindingInfo BindingInfo { get; set; }
    }
}