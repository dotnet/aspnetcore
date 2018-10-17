// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TriageBuildFailures.Abstractions
{
    public interface ICITestOccurrence
    {
        BuildStatus Status { get; }
        string Name { get; }
        string BuildId { get; }
        string TestId { get; }
    }
}
