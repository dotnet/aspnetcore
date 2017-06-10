// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    [DebuggerDisplay("ParameterModel: Name={ParameterName}")]
    public class ParameterModel : ICommonModel, IBindingModel
    {
        public ParameterModel(
            ParameterInfo parameterInfo,
            IReadOnlyList<object> attributes)
        {
            if (parameterInfo == null)
            {
                throw new ArgumentNullException(nameof(parameterInfo));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            ParameterInfo = parameterInfo;
            Properties = new Dictionary<object, object>();
            Attributes = new List<object>(attributes);
        }

        public ParameterModel(ParameterModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Action = other.Action;
            Attributes = new List<object>(other.Attributes);
            BindingInfo = other.BindingInfo == null ? null : new BindingInfo(other.BindingInfo);
            ParameterInfo = other.ParameterInfo;
            ParameterName = other.ParameterName;
            Properties = new Dictionary<object, object>(other.Properties);
        }

        public ActionModel Action { get; set; }

        public IReadOnlyList<object> Attributes { get; }

        public IDictionary<object, object> Properties { get; }

        MemberInfo ICommonModel.MemberInfo => ParameterInfo.Member;

        string ICommonModel.Name => ParameterName;

        public ParameterInfo ParameterInfo { get; }

        public string ParameterName { get; set; }

        public BindingInfo BindingInfo { get; set; }
    }
}