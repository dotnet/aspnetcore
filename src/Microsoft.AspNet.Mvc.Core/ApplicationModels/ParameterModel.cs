// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    [DebuggerDisplay("ParameterModel: Name={ParameterName}")]
    public class ParameterModel
    {
        public ParameterModel([NotNull] ParameterInfo parameterInfo,
                              [NotNull] IReadOnlyList<object> attributes)
        {
            ParameterInfo = parameterInfo;

            Attributes = new List<object>(attributes);
        }

        public ParameterModel([NotNull] ParameterModel other)
        {
            Action = other.Action;
            Attributes = new List<object>(other.Attributes);
            BinderMetadata = other.BinderMetadata;
            ParameterInfo = other.ParameterInfo;
            ParameterName = other.ParameterName;
        }

        public ActionModel Action { get; set; }

        public IReadOnlyList<object> Attributes { get; }

        public IBinderMetadata BinderMetadata { get; set; }

        public ParameterInfo ParameterInfo { get; private set; }

        public string ParameterName { get; set; }
    }
}