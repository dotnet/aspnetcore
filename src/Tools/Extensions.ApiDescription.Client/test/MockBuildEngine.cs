// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;

// Inspired by https://github.com/microsoft/msbuild/blob/master/src/Utilities.UnitTests/MockEngine.cs
namespace Microsoft.Extensions.ApiDescription.Client
{
    internal sealed class MockBuildEngine : IBuildEngine3
    {
        private readonly StringBuilder _log = new StringBuilder();

        public bool IsRunningMultipleNodes => false;

        public bool ContinueOnError => false;

        public string ProjectFileOfTaskNode => string.Empty;

        public int LineNumberOfTaskNode => 0;

        public int ColumnNumberOfTaskNode => 0;

        internal MessageImportance MinimumMessageImportance { get; set; } = MessageImportance.Low;

        internal int Messages { set; get; }

        internal int Warnings { set; get; }

        internal int Errors { set; get; }

        internal string Log => _log.ToString();

        public bool BuildProjectFile(
            string projectFileName,
            string[] targetNames,
            IDictionary globalProperties,
            IDictionary targetOutputs) => false;

        public bool BuildProjectFile(
            string projectFileName,
            string[] targetNames,
            IDictionary globalProperties,
            IDictionary targetOutputs,
            string toolsVersion) => false;

        public bool BuildProjectFilesInParallel(
            string[] projectFileNames,
            string[] targetNames,
            IDictionary[] globalProperties,
            IDictionary[] targetOutputsPerProject,
            string[] toolsVersion,
            bool useResultsCache,
            bool unloadProjectsOnCompletion) => false;

        public BuildEngineResult BuildProjectFilesInParallel(
            string[] projectFileNames,
            string[] targetNames,
            IDictionary[] globalProperties,
            IList<string>[] undefineProperties,
            string[] toolsVersion,
            bool includeTargetOutputs) => new BuildEngineResult(false, null);

        public void LogErrorEvent(BuildErrorEventArgs eventArgs)
        {
            _log.AppendLine(eventArgs.Message);
            Errors++;
        }

        public void LogWarningEvent(BuildWarningEventArgs eventArgs)
        {
            _log.AppendLine(eventArgs.Message);
            Warnings++;
        }

        public void LogCustomEvent(CustomBuildEventArgs eventArgs)
        {
            _log.AppendLine(eventArgs.Message);
        }

        public void LogMessageEvent(BuildMessageEventArgs eventArgs)
        {
            // Only record the message if it is above the minimum importance. MessageImportance enum has higher values
            // for lower importance.
            if (eventArgs.Importance <= MinimumMessageImportance)
            {
                _log.AppendLine(eventArgs.Message);
                Messages++;
            }
        }

        public void Reacquire()
        {
        }

        public void Yield()
        {
        }
    }
}
