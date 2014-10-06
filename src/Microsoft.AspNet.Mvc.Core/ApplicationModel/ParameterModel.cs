// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ApplicationModel
{
    public class ParameterModel
    {
        public ParameterModel(ParameterInfo parameterInfo)
        {
            ParameterInfo = parameterInfo;

            Attributes = new List<object>();
        }

        public ActionModel Action { get; set; }

        public List<object> Attributes { get; private set; }

        public bool IsOptional { get; set; }

        public ParameterInfo ParameterInfo { get; private set; }

        public string ParameterName { get; set; }
    }
}