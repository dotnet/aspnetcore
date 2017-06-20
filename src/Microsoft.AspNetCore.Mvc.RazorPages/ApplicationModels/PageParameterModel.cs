// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    [DebuggerDisplay("PageParameterModel: Name={ParameterName}")]
    public class PageParameterModel : ICommonModel, IBindingModel
    {
        public PageParameterModel(
            ParameterInfo parameterInfo,
            IReadOnlyList<object> attributes)
        {
            ParameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            Properties = new Dictionary<object, object>();
            Attributes = new List<object>(attributes);
        }

        public PageParameterModel(PageParameterModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Handler = other.Handler;
            Attributes = new List<object>(other.Attributes);
            BindingInfo = other.BindingInfo == null ? null : new BindingInfo(other.BindingInfo);
            ParameterInfo = other.ParameterInfo;
            ParameterName = other.ParameterName;
            Properties = new Dictionary<object, object>(other.Properties);
        }

        public PageHandlerModel Handler { get; set; }

        public IReadOnlyList<object> Attributes { get; }

        public IDictionary<object, object> Properties { get; }

        MemberInfo ICommonModel.MemberInfo => ParameterInfo.Member;

        string ICommonModel.Name => ParameterName;

        public ParameterInfo ParameterInfo { get; }

        public string ParameterName { get; set; }

        public BindingInfo BindingInfo { get; set; }
    }
}