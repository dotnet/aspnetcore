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
using System.Linq;

namespace Microsoft.Extensions.ProjectModel
{
    internal class MsBuildProjectContextBuilder
    {
        protected string _configuration;
        protected IFileInfo _fileInfo;
        protected string[] _buildTargets;
        protected Dictionary<string, string> _globalProperties = new Dictionary<string, string>();
        private MsBuildContext _msbuildContext;

        public MsBuildProjectContextBuilder()
        {
            Initialize();
        }

        public virtual MsBuildProjectContextBuilder Clone()
        {
            var builder = new MsBuildProjectContextBuilder()
                .WithProperties(_globalProperties)
                .WithBuildTargets(_buildTargets);

            if (_msbuildContext != null)
            {
                builder.UseMsBuild(_msbuildContext);
            }

            if (_fileInfo != null)
            {
                builder.WithProjectFile(_fileInfo);
            }

            if (_configuration != null)
            {
                builder.WithConfiguration(_configuration);
            }

            return builder;
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
            _msbuildContext = context;

            /*
            Workaround https://github.com/Microsoft/msbuild/issues/999
            Error: System.TypeInitializationException : The type initializer for 'BuildEnvironmentHelperSingleton' threw an exception.
            Could not determine a valid location to MSBuild. Try running this process from the Developer Command Prompt for Visual Studio.
            */

            Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", context.MsBuildExecutableFullPath);
            WithProperty("MSBuildExtensionsPath", context.ExtensionsPath);

            return this;
        }

        public MsBuildProjectContextBuilder WithProperties(IDictionary<string, string> properties)
        {
            foreach (var prop in properties)
            {
                _globalProperties[prop.Key] = prop.Value;
            }

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
            if (_msbuildContext == null)
            {
                var projectDir = Path.GetDirectoryName(fileInfo.PhysicalPath);
                var sdk = DotNetCoreSdkResolver.DefaultResolver.ResolveProjectSdk(projectDir);
                UseMsBuild(MsBuildContext.FromDotNetSdk(sdk));
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
            if (project.GetProperty("TargetFramework") == null)
            {
                var frameworks = GetAvailableTargetFrameworks(project).ToList();
                if (frameworks.Count > 1)
                {
                    throw new InvalidOperationException($"Multiple frameworks are available. Either use {nameof(WithTargetFramework)} or {nameof(BuildAllTargetFrameworks)}");
                }

                if (frameworks.Count == 0)
                {
                    throw new InvalidOperationException($"No frameworks are available. Either use {nameof(WithTargetFramework)} or {nameof(BuildAllTargetFrameworks)}");
                }

                project.SetGlobalProperty("TargetFramework", frameworks.Single());
            }

            var projectInstance = CreateProjectInstance(project, _buildTargets, ignoreBuildErrors);

            var name = Path.GetFileNameWithoutExtension(_fileInfo.Name);
            return new MsBuildProjectContext(name, _configuration, projectInstance);
        }

        public IEnumerable<MsBuildProjectContext> BuildAllTargetFrameworks()
        {
            var projectCollection = CreateProjectCollection();
            var project = CreateProject(_fileInfo, _configuration, _globalProperties, projectCollection);

            foreach (var framework in GetAvailableTargetFrameworks(project))
            {
                var builder = Clone();
                builder.WithTargetFramework(framework);
                yield return builder.Build();
            }
        }

        protected virtual void Initialize()
        {
            WithBuildTargets(new[] { "ResolveReferences", "ResolvePackageDependenciesDesignTime" });
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

        private IEnumerable<string> GetAvailableTargetFrameworks(Project project)
        {
            var frameworks = project.GetProperty("TargetFrameworks")?.EvaluatedValue;
            if (string.IsNullOrEmpty(frameworks))
            {
                return Enumerable.Empty<string>();
            }
            return frameworks.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
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