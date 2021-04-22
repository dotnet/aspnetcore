// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A type that represents a paramater.
    /// </summary>
    [DebuggerDisplay("ParameterModel: Name={ParameterName}")]
    public class ParameterModel : ParameterModelBase, ICommonModel
    {
        /// <summary>
        /// Initializes a new <see cref="ParameterModel"/>.
        /// </summary>
        /// <param name="parameterInfo">The parameter info.</param>
        /// <param name="attributes">The attributes.</param>
        public ParameterModel(
            ParameterInfo parameterInfo,
            IReadOnlyList<object> attributes)
            : base(parameterInfo.ParameterType, attributes)
        {
            ParameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
        }

        /// <summary>
        /// Initializes a new <see cref="ParameterModel"/>.
        /// </summary>
        /// <param name="other">The parameter model to copy.</param>
        public ParameterModel(ParameterModel other)
            : base(other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Action = other.Action;
            ParameterInfo = other.ParameterInfo;
        }

        /// <summary>
        /// The <see cref="ActionModel"/>.
        /// </summary>
        public ActionModel Action { get; set; } = default!;

        /// <summary>
        /// The properties.
        /// </summary>
        public new IDictionary<object, object> Properties => base.Properties;

        /// <summary>
        /// The attributes.
        /// </summary>
        public new IReadOnlyList<object> Attributes => base.Attributes;

        MemberInfo ICommonModel.MemberInfo => ParameterInfo.Member;

        /// <summary>
        /// The <see cref="ParameterInfo"/>.
        /// </summary>
        public ParameterInfo ParameterInfo { get; }

        /// <summary>
        /// The parameter name.
        /// </summary>
        public string ParameterName
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>
        /// The display name.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var parameterTypeName = TypeNameHelper.GetTypeDisplayName(ParameterInfo.ParameterType, fullName: false);
                return $"{parameterTypeName} {ParameterName}";
            }
        }
    }
}
