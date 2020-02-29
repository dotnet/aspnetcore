// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    internal class ParameterizedJobConfigAttribute: AspNetCoreBenchmarkAttribute
    {
        public ParameterizedJobConfigAttribute(Type configType) : base(configType)
        {
        }
    }
}
