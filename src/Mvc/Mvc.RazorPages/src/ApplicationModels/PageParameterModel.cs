// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A model type for reading and manipulation properties and parameters representing a Page Parameter.
    /// </summary>
    [DebuggerDisplay("PageParameterModel: Name={ParameterName}")]
    public class PageParameterModel : ParameterModelBase, ICommonModel, IBindingModel
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="PageParameterModel"/>.
        /// </summary>
        /// <param name="parameterInfo">The parameter info.</param>
        /// <param name="attributes">The attributes.</param>
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

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other">The model to copy.</param>
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

        /// <summary>
        /// The <see cref="PageHandlerModel"/>.
        /// </summary>
        public PageHandlerModel Handler { get; set; }

        MemberInfo ICommonModel.MemberInfo => ParameterInfo.Member;

        /// <summary>
        /// The <see cref="ParameterInfo"/>.
        /// </summary>
        public ParameterInfo ParameterInfo { get; }

        /// <summary>
        /// Gets or sets the parameter name.
        /// </summary>
        public string ParameterName
        {
            get => Name;
            set => Name = value;
        }
    }
}
