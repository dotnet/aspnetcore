// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace RepoTasks.CodeGen
{
    class RepositoryProject
    {
        private readonly ProjectRootElement _doc;

        public RepositoryProject(string repositoryRoot)
        {
            _doc = ProjectRootElement.Create(NewProjectFileOptions.None);
            var import = _doc.CreateImportElement(@"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props");
            var propGroup = _doc.AddPropertyGroup();
            if (repositoryRoot[repositoryRoot.Length - 1] != '\\')
            {
                repositoryRoot += '\\';
            }
            propGroup.AddProperty("RepositoryRoot", repositoryRoot);
            _doc.AddItemGroup();
            _doc.PrependChild(import);
            _doc.AddImport(@"$(MSBuildToolsPath)\Microsoft.Common.targets");
        }

        public void AddProjectReference(string path)
        {
            _doc.AddItem("ProjectReference", path);
        }

        public void AddProperty(string name, string value)
        {
            _doc.AddProperty(name, value);
        }

        public void Save(string filePath)
        {
            _doc.Save(filePath, Encoding.UTF8);
        }
    }
}
