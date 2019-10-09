// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    [DebuggerDisplay("PageParameterModel: Name={ParameterName}")]
    public class PageParameterModel : ParameterModelBase, ICommonModel, IBindingModel
    {
        public PageParameterModel(
            ParameterInfo parameterInfo,
            IReadOnlyList<object> attributes)
            : base(parameterInfo?.ParameterType, attributes)
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
        }

        public PageParameterModel(PageParameterModel other)
            : base(other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Handler = other.Handler;
            ParameterInfo = other.ParameterInfo;
        }

        public PageHandlerModel Handler { get; set; }

        MemberInfo ICommonModel.MemberInfo => ParameterInfo.Member;

        public ParameterInfo ParameterInfo { get; }

        public string ParameterName
        {
            get => Name;
            set => Name = value;
        }
    }
}