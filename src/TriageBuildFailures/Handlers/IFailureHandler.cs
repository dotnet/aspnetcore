// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.Handlers
{
    public interface IFailureHandler
    {
        Task<bool> CanHandleFailure(ICIBuild build);

        Task HandleFailure(ICIBuild build);
    }
}
