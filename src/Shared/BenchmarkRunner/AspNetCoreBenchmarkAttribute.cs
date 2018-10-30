// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    internal class AspNetCoreBenchmarkAttribute : Attribute, IConfigSource
    {
        public static bool UseValidationConfig { get; set; }

        public Type ConfigType { get; }
        public Type ValidationConfigType { get; }

        public AspNetCoreBenchmarkAttribute() : this(typeof(DefaultCoreConfig))
        {
        }

        public AspNetCoreBenchmarkAttribute(Type configType) : this(configType, typeof(DefaultCoreValidationConfig))
        {
        }

        public AspNetCoreBenchmarkAttribute(Type configType, Type validationConfigType)
        {
            ConfigType = configType;
            ValidationConfigType = validationConfigType;
        }

        public IConfig Config => (IConfig) Activator.CreateInstance(UseValidationConfig ? ValidationConfigType : ConfigType, Array.Empty<object>());
    }
}
