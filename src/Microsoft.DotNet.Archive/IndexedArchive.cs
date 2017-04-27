// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Microsoft.DotNet.Archive
{
    public class IndexedArchive : IDisposable
    {
        private class DestinationFileInfo
        {
            public DestinationFileInfo(string destinationPath, string hash)
            {
                DestinationPath = destinationPath;
                Hash = hash;
            }

            public string DestinationPath { get; }
            public string Hash { get; }
        }

        private class ArchiveSource
        {
            public ArchiveSource(string sourceArchive, string sourceFile, string archivePath, string hash, long size)
            {
                SourceArchive = sourceArchive;
                SourceFile = sourceFile;
                ArchivePath = archivePath;
                Hash = hash;
                Size = size;
            }

            public string SourceArchive { get; set; }
            public string SourceFile { get; set; }
            public string ArchivePath { get; }
            public string Hash { get; }
            public string FileName { get { return Path.GetFileNameWithoutExtension(ArchivePath); } }
            public string Extension { get { return Path.GetExtension(ArchivePath); } }
            public long Size { get; }

            public void CopyTo(Stream destination)
            {
                if (!String.IsNullOrEmpty(SourceArchive))
                {
                    using (var zip = new ZipArchive(File.OpenRead(SourceArchive), ZipArchiveMode.Read))
                    using (var sourceStream = zip.GetEntry(SourceFile)?.Open())
                    {
                        if (sourceStream == null)
                        {
                            throw new Exception($"Couldn't find entry {SourceFile} in archive {SourceArchive}");
                        }

                        sourceStream.CopyTo(destination);
                    }
                }
                else
                {
                    using (var sourceStream = File.OpenRead(SourceFile))
                    {
                        sourceStream.CopyTo(destination);
                    }
                }
            }
        }

        static string[] ZipExtensions = new[] { ".zip", ".nupkg" };
        static string IndexFileName = "index.txt";

        // maps file hash to archve path
        // $ prefix indicates that the file is not in the archive and path is a hash
        private Dictionary<string, ArchiveSource> _archiveFiles = new Dictionary<string, ArchiveSource>();
        // maps file hash to external path
        private Dictionary<string, string> _externalFiles = new Dictionary<string, string>();
        // lists all extracted files & hashes
        private List<DestinationFileInfo> _destFiles = new List<DestinationFileInfo>();
        private bool _disposed = false;
        private ThreadLocal<SHA256> _sha = new ThreadLocal<SHA256>(() => SHA256.Create());

        public IndexedArchive()
        { }
        
        private static Stream CreateTemporaryStream()
        {
            string temp = Path.GetTempPath();
            string tempFile = Path.Combine(temp, Guid.NewGuid().ToString());
            return File.Create(tempFile, 4096, FileOptions.DeleteOnClose);
        }

        private static FileStream CreateTemporaryFileStream()
        {
            string temp = Path.GetTempPath();
            string tempFile = Path.Combine(temp, Guid.NewGuid().ToString());
            return new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete, 4096, FileOptions.DeleteOnClose);
        }

        public void Save(string archivePath, IProgress<ProgressReport> progress)
        {
            CheckDisposed();

            using (var archiveStream = CreateTemporaryStream())
            {
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
                {
                    BuildArchive(archive, progress);
                }  // close archive

                archiveStream.Seek(0, SeekOrigin.Begin);

                using (var lzmaStream = File.Create(archivePath))
                {
                    CompressionUtility.Compress(archiveStream, lzmaStream, progress);
                }
            }  // close archiveStream
        }

        private void BuildArchive(ZipArchive archive, IProgress<ProgressReport> progress)
        {
            // write the file index
            var indexEntry = archive.CreateEntry(IndexFileName, CompressionLevel.NoCompression);

            using (var stream = indexEntry.Open())
            using (var textWriter = new StreamWriter(stream))
            {
                foreach (var entry in _destFiles)
                {
                    var archiveFile = _archiveFiles[entry.Hash];
                    string archivePath = _archiveFiles[entry.Hash].ArchivePath;
                    if (archiveFile.SourceFile == null)
                    {
                        archivePath = "$" + archivePath;
                    }

                    textWriter.WriteLine($"{entry.DestinationPath}|{archivePath}");
                }
            }

            // sort the files so that similar files are close together
            var filesToArchive = _archiveFiles.Values.ToList();
            filesToArchive.Sort((f1, f2) =>
            {
                // first sort by extension
                var comp = String.Compare(f1.Extension, f2.Extension, StringComparison.OrdinalIgnoreCase);

                if (comp == 0)
                {
                    // then sort by filename
                    comp = String.Compare(f1.FileName, f2.FileName, StringComparison.OrdinalIgnoreCase);
                }

                if (comp == 0)
                {
                    // sort by file size (helps differentiate ref/lib/facade)
                    comp = f1.Size.CompareTo(f2.Size);
                }

                if (comp == 0)
                {
                    // finally sort by full archive path so we have stable output
                    comp = String.Compare(f1.ArchivePath, f2.ArchivePath, StringComparison.OrdinalIgnoreCase);
                }

                return comp;
            });

            int filesAdded = 0;
            // add all the files
            foreach (var fileToArchive in filesToArchive)
            {
                var entry = archive.CreateEntry(fileToArchive.ArchivePath, CompressionLevel.NoCompression);
                using (var entryStream = entry.Open())
                {
                    fileToArchive.CopyTo(entryStream);
                }

                progress.Report("Archiving files", ++filesAdded, filesToArchive.Count);
            }
        }

        private abstract class ExtractOperation
        {
            public ExtractOperation(string destinationPath)
            {
                DestinationPath = destinationPath;
            }

            public string DestinationPath { get; }
            public virtual void DoOperation()
            {
                string directory = Path.GetDirectoryName(DestinationPath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                Execute();
            }
            protected abstract void Execute();
        }

        private class CopyOperation : ExtractOperation
        {
            public CopyOperation(ExtractSource source, string destinationPath) : base(destinationPath)
            {
                Source = source;
            }
            public ExtractSource Source { get; }
            protected override void Execute()
            {
                if (Source.LocalPath != null)
                {
                    File.Copy(Source.LocalPath, DestinationPath, true);
                }
                else
                {
                    using (var destinationStream = File.Create(DestinationPath))
                    {
                        Source.CopyToStream(destinationStream);
                    }
                }
            }
        }

        private class ZipOperation : ExtractOperation
        {
            public ZipOperation(string destinationPath) : base(destinationPath)
            {
            }

            private List<Tuple<string, ExtractSource>> entries = new List<Tuple<string, ExtractSource>>();

            public void AddEntry(string entryName, ExtractSource source)
            {
                entries.Add(Tuple.Create(entryName, source));
            }

            protected override void Execute()
            {
                using (var archiveStream = File.Create(DestinationPath))
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create))
                {
                    foreach(var zipSource in entries)
                    {
                        var entry = archive.CreateEntry(zipSource.Item1, CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        {
                            zipSource.Item2.CopyToStream(entryStream);
                        }
                    }
                }
            }
        }

        private class ExtractSource
        {
            private string _entryName;
            private readonly string _localPath;
            private ThreadLocalZipArchive _archive;

            public ExtractSource(string sourceString, Dictionary<string, string> externalFiles, ThreadLocalZipArchive archive)
            {
                if (sourceString[0] == '$')
                {
                    var externalHash = sourceString.Substring(1);
                    if (!externalFiles.TryGetValue(externalHash, out _localPath))
                    {
                        throw new Exception("Could not find external file with hash {externalHash}.");
                    }
                }
                else
                {
                    _entryName = sourceString;
                    _archive = archive;
                }
            }

            public string LocalPath { get { return _localPath; } }

            public void CopyToStream(Stream destinationStream)
            {
                if (_localPath != null)
                {
                    using (var sourceStream = File.OpenRead(_localPath))
                    {
                        sourceStream.CopyTo(destinationStream);
                    }
                }
                else
                {
                    using (var sourceStream = _archive.Archive.GetEntry(_entryName).Open())
                    {
                        sourceStream.CopyTo(destinationStream);
                    }
                }

            }
        }

        private static char[] pipeSeperator = new[] { '|' };
        public void Extract(string compressedArchivePath, string outputDirectory, IProgress<ProgressReport> progress)
        {
            using (var archiveStream = CreateTemporaryFileStream())
            {
                // decompress the LZMA stream
                using (var lzmaStream = File.OpenRead(compressedArchivePath))
                {
                    CompressionUtility.Decompress(lzmaStream, archiveStream, progress);
                }

                var archivePath = ((FileStream)archiveStream).Name;

                // reset the uncompressed stream
                archiveStream.Seek(0, SeekOrigin.Begin);

                // read as a zip archive
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read))
                using (var tlArchive = new ThreadLocalZipArchive(archivePath, archive))
                {
                    List<ExtractOperation> extractOperations = new List<ExtractOperation>();
                    Dictionary<string, ExtractSource> sourceCache = new Dictionary<string, ExtractSource>();

                    // process the index to determine all extraction operations
                    var indexEntry = archive.GetEntry(IndexFileName);
                    using (var indexReader = new StreamReader(indexEntry.Open()))
                    {
                        Dictionary<string, ZipOperation> zipOperations = new Dictionary<string, ZipOperation>(StringComparer.OrdinalIgnoreCase);
                        for (var line = indexReader.ReadLine(); line != null; line = indexReader.ReadLine())
                        {
                            var lineParts = line.Split(pipeSeperator);
                            if (lineParts.Length != 2)
                            {
                                throw new Exception("Unexpected index line format, too many '|'s.");
                            }

                            string target = lineParts[0];
                            string source = lineParts[1];

                            ExtractSource extractSource;
                            if (!sourceCache.TryGetValue(source, out extractSource))
                            {
                                sourceCache[source] = extractSource = new ExtractSource(source, _externalFiles, tlArchive);
                            }

                            var zipSeperatorIndex = target.IndexOf("::", StringComparison.OrdinalIgnoreCase);

                            if (zipSeperatorIndex != -1)
                            {
                                string zipRelativePath = target.Substring(0, zipSeperatorIndex);
                                string zipEntryName = target.Substring(zipSeperatorIndex + 2);
                                string destinationPath = Path.Combine(outputDirectory, zipRelativePath);

                                // operations on a zip file will be sequential
                                ZipOperation currentZipOperation;

                                if (!zipOperations.TryGetValue(destinationPath, out currentZipOperation))
                                {
                                    extractOperations.Add(currentZipOperation = new ZipOperation(destinationPath));
                                    zipOperations.Add(destinationPath, currentZipOperation);
                                }
                                currentZipOperation.AddEntry(zipEntryName, extractSource);
                            }
                            else
                            {
                                string destinationPath = Path.Combine(outputDirectory, target);
                                extractOperations.Add(new CopyOperation(extractSource, destinationPath));
                            }
                        }
                    }

                    int opsExecuted = 0;
                    // execute all operations
                    //foreach(var extractOperation in extractOperations)
                    extractOperations.AsParallel().ForAll(extractOperation =>
                    {
                        extractOperation.DoOperation();
                        progress.Report("Expanding", Interlocked.Increment(ref opsExecuted), extractOperations.Count);
                    });
                }
            }
        }

        public void AddExternalDirectory(string externalDirectory)
        {
            CheckDisposed();
            foreach (var externalFile in Directory.EnumerateFiles(externalDirectory, "*", SearchOption.AllDirectories))
            {
                AddExternalFile(externalFile);
            }
        }

        public void AddExternalFile(string externalFile)
        {
            CheckDisposed();
            using (var fs = File.OpenRead(externalFile))
            {
                string hash = GetHash(fs); 
                // $ prefix indicates that the file is not in the archive and path is relative to an external directory
                _archiveFiles[hash] = new ArchiveSource(null, null, "$" + hash , hash, fs.Length);
                _externalFiles[hash] = externalFile;
            }
        }
        public void AddDirectory(string sourceDirectory, IProgress<ProgressReport> progress, string destinationDirectory = null)
        {
            var sourceFiles = Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories).ToArray();
            int filesAdded = 0;
            sourceFiles.AsParallel().ForAll(sourceFile =>
                {
                    // path relative to the destination/extracted directory to write the file
                    string destinationRelativePath = sourceFile.Substring(sourceDirectory.Length + 1);

                    if (destinationDirectory != null)
                    {
                        destinationRelativePath = Path.Combine(destinationDirectory, destinationRelativePath);
                    }

                    string extension = Path.GetExtension(sourceFile);

                    if (ZipExtensions.Any(ze => ze.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                    {
                        AddZip(sourceFile, destinationRelativePath);
                    }
                    else
                    {
                        AddFile(sourceFile, destinationRelativePath);
                    }

                    progress.Report($"Adding {sourceDirectory}", Interlocked.Increment(ref filesAdded), sourceFiles.Length);
                });
        }

        public void AddZip(string sourceZipFile, string destinationZipFile)
        {
            CheckDisposed();

            using (var sourceArchive = new ZipArchive(File.OpenRead(sourceZipFile), ZipArchiveMode.Read))
            {
                foreach(var entry in sourceArchive.Entries)
                {
                    string hash = null;
                    long size = entry.Length;
                    string destinationPath = $"{destinationZipFile}::{entry.FullName}";
                    using (var stream = entry.Open())
                    {
                        hash = GetHash(stream);
                    }

                    AddArchiveSource(sourceZipFile, entry.FullName, destinationPath, hash, size);
                }
            }
        }

        public void AddFile(string sourceFilePath, string destinationPath)
        {
            CheckDisposed();

            string hash;
            long size;
            // lifetime of this stream is managed by AddStream
            using (var stream = File.Open(sourceFilePath, FileMode.Open))
            {
                hash = GetHash(stream);
                size = stream.Length;
            }

            AddArchiveSource(null, sourceFilePath, destinationPath, hash, size);
        }

        private void AddArchiveSource(string sourceArchive, string sourceFile, string destinationPath, string hash, long size)
        {
            lock (_archiveFiles)
            {
                _destFiles.Add(new DestinationFileInfo(destinationPath, hash));

                // see if we already have this file in the archive/external
                ArchiveSource existing = null;
                if (_archiveFiles.TryGetValue(hash, out existing))
                {
                    // if we have raw source file, prefer that over a zipped source file
                    if (sourceArchive == null && existing.SourceArchive != null)
                    {
                        existing.SourceArchive = null;
                        existing.SourceFile = sourceFile;
                    }
                }
                else
                {
                    var archivePath = Path.Combine(hash, Path.GetFileName(destinationPath));

                    _archiveFiles.Add(hash, new ArchiveSource(sourceArchive, sourceFile, archivePath, hash, size));
                }
            }
        }

        public string GetHash(Stream stream)
        {
            var hashBytes = _sha.Value.ComputeHash(stream);

            return GetHashString(hashBytes);
        }

        private static string GetHashString(byte[] hashBytes)
        {
            StringBuilder builder = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                builder.AppendFormat("{0:x2}", b);
            }
            return builder.ToString();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_sha != null)
                {
                    _sha.Dispose();
                    _sha = null;
                }
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(IndexedArchive));
            }
        }
    }
}
