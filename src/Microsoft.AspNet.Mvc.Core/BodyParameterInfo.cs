// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc
{
    public class BodyParameterInfo
    {
        public BodyParameterInfo(Type parameterType)
        {
            ParameterType = parameterType;
        }

        public Type ParameterType { get; private set; }
    }
}

