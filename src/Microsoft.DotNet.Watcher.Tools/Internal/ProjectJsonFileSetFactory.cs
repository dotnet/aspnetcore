// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class ProjectJsonFileSetFactory : IFileSetFactory
    {
        private readonly ILogger _logger;
        private readonly string _projectFile;
        public ProjectJsonFileSetFactory(ILogger logger, string projectFile)
        {
            Ensure.NotNull(logger, nameof(logger));
            Ensure.NotNullOrEmpty(projectFile, nameof(projectFile));

            _logger = logger;
            _projectFile = projectFile;
        }

        public async Task<IFileSet> CreateAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Project project;
                string errors;
                if (ProjectReader.TryReadProject(_projectFile, out project, out errors))
                {
                    return new ProjectJsonFileSet(_projectFile);
                }

                _logger.LogError($"Error(s) reading project file '{_projectFile}': ");
                _logger.LogError(errors);
                _logger.LogInformation("Fix the error to continue.");

                var fileSet = new FileSet(new[] { _projectFile });

                using (var watcher = new FileSetWatcher(fileSet))
                {
                    await watcher.GetChangedFileAsync(cancellationToken);

                    _logger.LogInformation($"File changed: {_projectFile}");
                }
            }
        }
    }
}
