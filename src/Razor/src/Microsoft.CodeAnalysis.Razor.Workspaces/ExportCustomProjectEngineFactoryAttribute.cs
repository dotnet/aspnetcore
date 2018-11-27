// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Razor
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ExportCustomProjectEngineFactoryAttribute : ExportAttribute, ICustomProjectEngineFactoryMetadata
    {
        public ExportCustomProjectEngineFactoryAttribute(string configurationName)
            : base(typeof(IProjectEngineFactory))
        {
            if (configurationName == null)
            {
                throw new ArgumentNullException(nameof(configurationName));
            }

            ConfigurationName = configurationName;
        }

        public string ConfigurationName { get; }

        public bool SupportsSerialization { get; set; }
    }
}
