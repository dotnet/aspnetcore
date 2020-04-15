// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Blazor.Build
{
    internal class FileThumbPrint : IEquatable<FileThumbPrint>
    {
        private FileThumbPrint(string path, DateTime lastWriteTimeUtc, string hash)
        {
            FilePath = path;
            LastWriteTimeUtc = lastWriteTimeUtc;
            Hash = hash;
        }

        public string FilePath { get; }

        public DateTime LastWriteTimeUtc { get; }

        public string Hash { get; }

        public override string ToString()
        {
            return $"{Path.GetFileName(FilePath)}, {LastWriteTimeUtc.ToString("u")}, {Hash}";
        }

        /// <summary>
        /// Returns a list of thumbprints for all files (recursive) in the specified directory, sorted by file paths.
        /// </summary>
        public static List<FileThumbPrint> CreateFolderThumbprint(ProjectDirectory project, string directoryPath, params string[] filesToIgnore)
        {
            directoryPath = Path.Combine(project.DirectoryPath, directoryPath);
            var files = Directory.GetFiles(directoryPath).Where(p => !filesToIgnore.Contains(p));
            var thumbprintLookup = new List<FileThumbPrint>();
            foreach (var file in files)
            {
                var thumbprint = Create(file);
                thumbprintLookup.Add(thumbprint);
            }

            thumbprintLookup.Sort(Comparer<FileThumbPrint>.Create((a, b) => StringComparer.Ordinal.Compare(a.FilePath, b.FilePath)));
            return thumbprintLookup;
        }

        public static FileThumbPrint Create(string path)
        {
            byte[] hashBytes;
            using (var sha1 = SHA1.Create())
            using (var fileStream = File.OpenRead(path))
            {
                hashBytes = sha1.ComputeHash(fileStream);
            }

            var hash = Convert.ToBase64String(hashBytes);
            var lastWriteTimeUtc = File.GetLastWriteTimeUtc(path);
            return new FileThumbPrint(path, lastWriteTimeUtc, hash);
        }

        public bool Equals(FileThumbPrint other)
        {
            return
                string.Equals(FilePath, other.FilePath, StringComparison.Ordinal) &&
                LastWriteTimeUtc == other.LastWriteTimeUtc &&
                string.Equals(Hash, other.Hash, StringComparison.Ordinal);
        }

        public override int GetHashCode() => LastWriteTimeUtc.GetHashCode();
    }
}
