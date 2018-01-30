// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace RepoTasks
{
    /// <summary>
    /// Layouts nuget packages as a V3 feed
    /// </summary>
    public class CreateNuGetV3FolderFeed : Task
    {
        [Required]
        public ITaskItem[] Packages { get; set; }

        [Required]
        public string DestinationFolder { get; set; }

        public bool Overwrite { get; set; }

        public override bool Execute()
        {
            Directory.CreateDirectory(DestinationFolder);

            foreach (var file in Packages)
            {
                PackageIdentity identity;
                using (var reader = new PackageArchiveReader(file.ItemSpec))
                {
                    identity = reader.GetIdentity();
                }

                Log.LogMessage(MessageImportance.High, "Adding {0} to feed '{1}'", identity, DestinationFolder);

                var packageFolder = Path.Combine(DestinationFolder, identity.Id.ToLowerInvariant(), identity.Version.ToNormalizedString());
                var nuspecFile = Path.Combine(packageFolder, $"{identity.Id.ToLowerInvariant()}.{identity.Version.ToNormalizedString()}.nuspec");
                var nupkgFile = Path.Combine(packageFolder, $"{identity.Id.ToLowerInvariant()}.{identity.Version.ToNormalizedString()}.nupkg");
                var sha512File = Path.Combine(packageFolder,  $"{identity.Id.ToLowerInvariant()}.{identity.Version.ToNormalizedString()}.nupkg.sha512");

                if (!Overwrite && File.Exists(nuspecFile))
                {
                    Log.LogError("File already exists: {0}", nuspecFile);
                    continue;
                }
                if (!Overwrite && File.Exists(nupkgFile))
                {
                    Log.LogError("File already exists: {0}", nupkgFile);
                    continue;
                }
                if (!Overwrite && File.Exists(sha512File))
                {
                    Log.LogError("File already exists: {0}", sha512File);
                    continue;
                }

                Directory.CreateDirectory(packageFolder);
                using (var reader = new PackageArchiveReader(file.ItemSpec))
                using (var nuspec = File.Create(nuspecFile))
                using (var metadata = reader.GetNuspec())
                {
                    Log.LogMessage("Creating {0}", nuspecFile);
                    metadata.CopyTo(nuspec);
                }

                Log.LogMessage("Copying {0}", nupkgFile);
                File.Copy(file.ItemSpec, nupkgFile);
                Log.LogMessage("Creating {0}", sha512File);
                File.WriteAllText(sha512File, GetFileHash(file.ItemSpec));
            }

            return !Log.HasLoggedErrors;
        }

        private static string GetFileHash(string filePath)
        {
            byte[] hash;

            using (var algorithm = new SHA512Managed())
            using (var stream = File.OpenRead(filePath))
            {
                hash = algorithm.ComputeHash(stream);
            }

            return Convert.ToBase64String(hash);
        }
    }
}
