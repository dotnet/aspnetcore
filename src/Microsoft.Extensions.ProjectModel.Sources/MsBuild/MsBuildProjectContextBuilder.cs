// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.ProjectModel.Internal;
using NuGet.Frameworks;

namespace Microsoft.Extensions.ProjectModel
{
    internal class MsBuildProjectContextBuilder
    {
        private string _configuration;
        private IFileInfo _fileInfo;
        private string[] _buildTargets;
        private Dictionary<string, string> _globalProperties = new Dictionary<string, string>();
        private bool _explicitMsBuild;

        public MsBuildProjectContextBuilder()
        {
            Initialize();
        }

        public MsBuildProjectContextBuilder WithBuildTargets(string[] targets)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            _buildTargets = targets;
            return this;
        }

        public MsBuildProjectContextBuilder WithConfiguration(string configuration)
        {
            _configuration = configuration;
            WithProperty("Configuration", configuration);
            return this;
        }

        public MsBuildProjectContextBuilder AsDesignTimeBuild()
        {
            // don't to expensive things
            WithProperty("DesignTimeBuild", "true");
            WithProperty("_ResolveReferenceDependencies", "true");
            WithProperty("BuildProjectReferences", "false");
            return this;
        }

        // should be needed in most cases, but can be used to override
        public MsBuildProjectContextBuilder UseMsBuild(MsBuildContext context)
        {
            _explicitMsBuild = true;
            SetMsBuildContext(context);
            return this;
        }

        public MsBuildProjectContextBuilder WithProperty(string property, string value)
        {
            _globalProperties[property] = value;
            return this;
        }

        public MsBuildProjectContextBuilder WithProjectFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var fileInfo = new PhysicalFileInfo(new FileInfo(filePath));
            return WithProjectFile(fileInfo);
        }

        public MsBuildProjectContextBuilder WithProjectFile(IFileInfo fileInfo)
        {
            if (!_explicitMsBuild)
            {
                var projectDir = Path.GetDirectoryName(fileInfo.PhysicalPath);
                var sdk = DotNetCoreSdkResolver.DefaultResolver.ResolveProjectSdk(projectDir);
                SetMsBuildContext(MsBuildContext.FromDotNetSdk(sdk));
            }

            _fileInfo = fileInfo;
            return this;
        }

        public MsBuildProjectContextBuilder WithTargetFramework(NuGetFramework framework)
            => WithTargetFramework(framework.GetShortFolderName());

        public MsBuildProjectContextBuilder WithTargetFramework(string framework)
            => WithProperty("TargetFramework", framework);

        public virtual MsBuildProjectContext Build(bool ignoreBuildErrors = false)
        {
            var projectCollection = CreateProjectCollection();
            var project = CreateProject(_fileInfo, _configuration, _globalProperties, projectCollection);
            var projectInstance = CreateProjectInstance(project, _buildTargets, ignoreBuildErrors);

            var name = Path.GetFileNameWithoutExtension(_fileInfo.Name);
            return new MsBuildProjectContext(name, _configuration, projectInstance);
        }

        protected virtual void Initialize()
        {
            WithBuildTargets(new[] { "ResolveReferences" });
            WithProperty("_ResolveReferenceDependencies", "true");
        }

        protected virtual ProjectCollection CreateProjectCollection() => new ProjectCollection();

        protected virtual Project CreateProject(IFileInfo fileInfo,
            string configuration,
            IDictionary<string, string> globalProps,
            ProjectCollection projectCollection)
        {
            using (var stream = fileInfo.CreateReadStream())
            {
                var xmlReader = XmlReader.Create(stream);

                var xml = ProjectRootElement.Create(xmlReader, projectCollection, preserveFormatting: true);
                xml.FullPath = fileInfo.PhysicalPath;

                return new Project(xml, globalProps, toolsVersion: null, projectCollection: projectCollection);
            }
        }

        protected virtual ProjectInstance CreateProjectInstance(Project project, string[] targets, bool ignoreErrors)
        {
            var projectInstance = project.CreateProjectInstance();
            if (targets.Length == 0)
            {
                return projectInstance;
            }

            var logger = new InMemoryLogger();
            projectInstance.Build(targets, new[] { logger });

            if (!ignoreErrors && logger.Errors.Count > 0)
            {
                throw CreateBuildFailedException(project.FullPath, logger.Errors);
            }

            return projectInstance;
        }

        private Exception CreateBuildFailedException(string filePath, IList<BuildErrorEventArgs> errors)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Building '{filePath}' failed.");

            for (var i = 0; i < errors.Count; i++)
            {
                sb.Append(i).Append(" :").AppendLine(errors[i].Message);
            }

            throw new InvalidOperationException(sb.ToString());
        }

        private void SetMsBuildContext(MsBuildContext context)
        {
            /*
            Workaround https://github.com/Microsoft/msbuild/issues/999
            Error: System.TypeInitializationException : The type initializer for 'BuildEnvironmentHelperSingleton' threw an exception.
            Could not determine a valid location to MSBuild. Try running this process from the Developer Command Prompt for Visual Studio.
            */

            Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", context.MsBuildExecutableFullPath);
            WithProperty("MSBuildExtensionsPath", context.ExtensionsPath);
        }

        private class InMemoryLogger : ILogger
        {
            private readonly Stack<Action> _onShutdown = new Stack<Action>();
            
            internal IList<BuildErrorEventArgs> Errors = new List<BuildErrorEventArgs>();

            public string Parameters { get; set; }
            public LoggerVerbosity Verbosity { get; set; }

            public void Initialize(IEventSource eventSource)
            {
                eventSource.ErrorRaised += OnError;
                _onShutdown.Push(() =>
                {
                    eventSource.ErrorRaised -= OnError;
                });
            }

            private void OnError(object sender, BuildErrorEventArgs e)
            {
                Errors.Add(e);
            }

            public void Shutdown()
            {
                while (_onShutdown.Count > 0)
                {
                    _onShutdown.Pop()?.Invoke();
                }
            }
        }
    }
}