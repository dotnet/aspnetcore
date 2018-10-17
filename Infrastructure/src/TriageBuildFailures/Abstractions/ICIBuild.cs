// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace TriageBuildFailures.Abstractions
{
    public interface ICIBuild
    {
        string Id { get; }
        Type CIType { get; }
        string BuildTypeID { get; }
        string BuildName { get; }
        BuildStatus Status { get; }
        string Branch { get; }
        DateTimeOffset StartDate { get; }
        Uri WebURL { get; }

        CIConfigBase GetCIConfig(Config config);
    }
}
