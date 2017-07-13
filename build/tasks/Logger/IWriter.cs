// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace RepoTasks
{
    internal interface IWriter
    {
        WriteHandler WriteHandler { get; }

        void OnBuildFinished(BuildFinishedEventArgs e);
        void OnBuildStarted(BuildStartedEventArgs e);
    }
}
