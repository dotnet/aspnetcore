// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class ProjectJsonFileSet : IFileSet
    {
        private readonly string _projectFile;
        private ISet<string> _currentFiles;

        public ProjectJsonFileSet(string projectFile)
        {
            _projectFile = projectFile;
        }

        public bool Contains(string filePath)
        {
            // if it was in the original list of files we were watching                 
            if (_currentFiles?.Contains(filePath) == true)
            {
                return true;
            }

            // It's possible the new file was not in the old set but will be in the new set.
            // Additions should be considered part of this.
            RefreshFileList();

            return _currentFiles.Contains(filePath);
        }

        public IEnumerator<string> GetEnumerator()
        {
            EnsureInitialized();
            return _currentFiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            EnsureInitialized();
            return _currentFiles.GetEnumerator();
        }

        private void EnsureInitialized()
        {
            if (_currentFiles == null)
            {
                RefreshFileList();
            }
        }

        private void RefreshFileList()
        {
            _currentFiles = new HashSet<string>(FindFiles(), StringComparer.OrdinalIgnoreCase);
        }

        private IEnumerable<string> FindFiles()
        {
            var projects = new HashSet<string>(); // temporary store to prevent re-parsing a project multiple times
            return GetProjectFilesClosure(_projectFile, projects);
        }

        private IEnumerable<string> GetProjectFilesClosure(string projectFile, ISet<string> projects)
        {
            if (projects.Contains(projectFile))
            {
                yield break;
            }

            projects.Add(projectFile);

            Project project;
            string errors;

            if (ProjectReader.TryReadProject(projectFile, out project, out errors))
            {
                foreach (var file in project.Files)
                {
                    yield return file;
                }

                foreach (var dependency in project.ProjectDependencies)
                {
                    foreach (var file in GetProjectFilesClosure(dependency, projects))
                    {
                        yield return file;
                    }
                }
            }
        }
    }
}
