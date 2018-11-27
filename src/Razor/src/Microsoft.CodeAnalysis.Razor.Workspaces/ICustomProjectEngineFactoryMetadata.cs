// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor
{
    public interface ICustomProjectEngineFactoryMetadata
    {
        string ConfigurationName { get; }

        bool SupportsSerialization { get; }
    }
}
