// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc
{
    public class ParameterBindingInfo
    {
        public ParameterBindingInfo(string prefix, Type parameterType)
        {
            Prefix = prefix;
            ParameterType = parameterType;
        }

        public string Prefix { get; private set; }

        public Type ParameterType { get; private set; }
    }
}
