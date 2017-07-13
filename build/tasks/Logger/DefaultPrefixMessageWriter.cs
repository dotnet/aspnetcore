// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace RepoTasks
{
    internal class DefaultPrefixMessageWriter : IWriter
    {
        private readonly string _flowId;

        public DefaultPrefixMessageWriter(WriteHandler write, string flowId)
        {
            _flowId = flowId;
            var prefix = $"{_flowId,-22}| ";
            WriteHandler = msg => write(prefix + msg);
        }

        public WriteHandler WriteHandler { get; }

        public void OnBuildStarted(BuildStartedEventArgs e)
        {
            WriteHandler(e.Message + Environment.NewLine);
        }

        public void OnBuildFinished(BuildFinishedEventArgs e)
        {
        }
    }
}
