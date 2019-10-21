// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Tools.Internal
{
    public class TemporaryDirectory : IDisposable
    {
        private List<TemporaryCSharpProject> _projects = new List<TemporaryCSharpProject>();
        private List<TemporaryDirectory> _subdirs = new List<TemporaryDirectory>();
        private Dictionary<string, string> _files = new Dictionary<string, string>();
        private TemporaryDirectory _parent;

        public TemporaryDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "dotnet-tool-tests", Guid.NewGuid().ToString("N"));
        }

        private TemporaryDirectory(string path, TemporaryDirectory parent)
        {
            _parent = parent;
            Root = path;
        }

        public TemporaryDirectory SubDir(string name)
        {
            var subdir = new TemporaryDirectory(Path.Combine(Root, name), this);
            _subdirs.Add(subdir);
            return subdir;
        }

        public string Root { get; }

        public TemporaryCSharpProject WithCSharpProject(string name, string sdk = "Microsoft.NET.Sdk")
        {
            var project = new TemporaryCSharpProject(name, this, sdk);
            _projects.Add(project);
            return project;
        }

        public TemporaryCSharpProject WithCSharpProject(string name, out TemporaryCSharpProject project, string sdk = "Microsoft.NET.Sdk")
        {
            project = WithCSharpProject(name, sdk);
            return project;
        }

        public TemporaryDirectory WithFile(string name, string contents = "")
        {
            _files[name] = contents;
            return this;
        }

        public TemporaryDirectory WithContentFile(string name)
        {
            using (var stream = File.OpenRead(Path.Combine("TestContent", $"{name}.txt")))
            using (var streamReader = new StreamReader(stream))
            {
                _files[name] = streamReader.ReadToEnd();
            }
            return this;
        }

        public TemporaryDirectory Up()
        {
            if (_parent == null)
            {
                throw new InvalidOperationException("This is the root directory");
            }
            return _parent;
        }

        public void Create()
        {
            Directory.CreateDirectory(Root);

            foreach (var dir in _subdirs)
            {
                dir.Create();
            }

            foreach (var project in _projects)
            {
                project.Create();
            }

            foreach (var file in _files)
            {
                CreateFile(file.Key, file.Value);
            }
        }

        public void CreateFile(string filename, string contents)
        {
            File.WriteAllText(Path.Combine(Root, filename), contents);
        }

        public void Dispose()
        {
            if (Root == null || !Directory.Exists(Root) || _parent != null)
            {
                return;
            }

            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
                Console.Error.WriteLine($"Test cleanup failed to delete '{Root}'");
            }
        }
    }
}
