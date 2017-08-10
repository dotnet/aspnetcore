// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace RepoTasks
{
    public class FlowLogger : ConsoleLogger
    {
        private volatile bool _initialized;

        public FlowLogger()
        {
        }

        public override void Initialize(IEventSource eventSource, int nodeCount)
        {
            PreInit(eventSource);
            base.Initialize(eventSource, nodeCount);
        }

        public override void Initialize(IEventSource eventSource)
        {
            PreInit(eventSource);
            base.Initialize(eventSource);
        }

        private void PreInit(IEventSource eventSource)
        {
            if (_initialized) return;
            _initialized = true;

            var flowId = GetFlowId();
            var prefix = $"{flowId,-22}| ";
            var write = WriteHandler;
            WriteHandler = msg => write(prefix + msg);

            eventSource.BuildStarted += (o, e) =>
            {
                WriteHandler(e.Message + Environment.NewLine);
            };
        }

        private string GetFlowId()
        {
            var parameters = Parameters?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parameters == null || parameters.Length == 0)
            {
                return null;
            }

            const string flowIdParamName = "FlowId=";
            return parameters
                .FirstOrDefault(p => p.StartsWith(flowIdParamName, StringComparison.Ordinal))
                ?.Substring(flowIdParamName.Length);
        }
    }
}
