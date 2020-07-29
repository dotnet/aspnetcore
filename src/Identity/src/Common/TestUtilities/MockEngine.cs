using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.DotNet.Arcade.Sdk.Tests.Utilities
{
    internal class MockEngine : IBuildEngine5
    {
        private readonly ITestOutputHelper _output;

        public MockEngine()
        {
        }

        public MockEngine(ITestOutputHelper output)
        {
            _output = output;
        }

        public ICollection<BuildMessageEventArgs> Messages { get; } = new List<BuildMessageEventArgs>();
        public ICollection<BuildWarningEventArgs> Warnings { get; } = new List<BuildWarningEventArgs>();
        public ICollection<BuildErrorEventArgs> Errors { get; } = new List<BuildErrorEventArgs>();

        public bool IsRunningMultipleNodes => false;

        public bool ContinueOnError { get; set; }

        public int LineNumberOfTaskNode => 0;

        public int ColumnNumberOfTaskNode => 0;

        public string ProjectFileOfTaskNode => "<test>";

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            _output?.WriteLine($"{e.Importance} : {e.Message}");
            Messages.Add(e);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            _output?.WriteLine($"warning {e.Code}: {e.Message}");
            Warnings.Add(e);
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            _output?.WriteLine($"error {e.Code}: {e.Message}");
            Errors.Add(e);
            if (!ContinueOnError)
            {
                throw new XunitException("Task error: " + e.Message);
            }
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            _output?.WriteLine(e.Message ?? string.Empty);
        }

        public void LogTelemetry(string eventName, IDictionary<string, string> properties)
        {
            if (_output != null)
            {
                _output?.WriteLine($"telemetry {eventName}: {properties.Aggregate(string.Empty, (sum, piece) => $"{sum}, {piece.Key} = {piece.Value}")}");
            }
        }

        #region NotImplemented

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs, string toolsVersion)
        {
            throw new NotImplementedException();
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public BuildEngineResult BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IList<string>[] removeGlobalProperties, string[] toolsVersion, bool returnTargetOutputs)
        {
            throw new NotImplementedException();
        }

        public bool BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IDictionary[] targetOutputsPerProject, string[] toolsVersion, bool useResultsCache, bool unloadProjectsOnCompletion)
        {
            throw new NotImplementedException();
        }

        public object GetRegisteredTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            throw new NotImplementedException();
        }

        public void Reacquire()
        {
            throw new NotImplementedException();
        }

        public void RegisterTaskObject(object key, object obj, RegisteredTaskObjectLifetime lifetime, bool allowEarlyCollection)
        {
            throw new NotImplementedException();
        }

        public object UnregisterTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            throw new NotImplementedException();
        }

        public void Yield()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

