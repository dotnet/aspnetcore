// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using RepoTasks.Utilities;

namespace RepoTasks
{
    public class CopyPackagesToSplitFolders : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// The item group containing the nuget packages to split in different folders.
        /// </summary>
        [Required]
        public ITaskItem[] Packages { get; set; }

        [Required]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// The folder where packages should be copied. Subfolders will be created based on package category.
        /// </summary>
        [Required]
        public string DestinationFolder { get; set; }

        public bool Overwrite { get; set; }

        public override bool Execute()
        {
            if (Files?.Length == 0)
            {
                Log.LogError("No packages were found.");
                return false;
            }

            var expectedPackages = PackageCollection.FromItemGroup(Packages);

            Directory.CreateDirectory(DestinationFolder);

            foreach (var file in Files)
            {
                PackageIdentity identity;
                using (var reader = new PackageArchiveReader(file.ItemSpec))
                {
                    identity = reader.GetIdentity();
                }

                var isSymbolsPackage = file.ItemSpec.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase);
                PackageCategory category;
                if (isSymbolsPackage)
                {
                    category = PackageCategory.Symbols;
                }
                else if (!expectedPackages.TryGetCategory(identity.Id, out category))
                {
                    Log.LogError($"Unexpected package artifact with id: {identity.Id}");
                    continue;
                }

                string destDir;
                switch (category)
                {
                    case PackageCategory.Unknown:
                        throw new InvalidOperationException($"Package {identity} does not have a recognized package category.");
                    case PackageCategory.Shipping:
                        destDir = Path.Combine(DestinationFolder, "ship");
                        break;
                    case PackageCategory.NoShip:
                        destDir = Path.Combine(DestinationFolder, "noship");
                        break;
                    case PackageCategory.ShipOob:
                        destDir = Path.Combine(DestinationFolder, "shipoob");
                        break;
                    case PackageCategory.Symbols:
                        destDir = Path.Combine(DestinationFolder, "symbols");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Directory.CreateDirectory(destDir);

                var destFile = Path.Combine(destDir, Path.GetFileName(file.ItemSpec));

                if (!Overwrite && File.Exists(destFile))
                {
                    Log.LogError($"File already exists in {destFile}");
                    continue;
                }

                Log.LogMessage($"Copying {file.ItemSpec} to {destFile}");
                File.Copy(file.ItemSpec, destFile, Overwrite);
                expectedPackages.Remove(identity.Id);
            }

            if (expectedPackages.Count != 0)
            {
                var error = new StringBuilder();
                foreach (var key in expectedPackages.Keys)
                {
                    error.Append(" - ").AppendLine(key);
                }

                Log.LogError($"Expected the following packages, but they were not found:" + error.ToString());
                return false;
            }

            return !Log.HasLoggedErrors;
        }
    }
}
