// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Tools
{
    // Note that this class has no thread-safety guarantees. The caller should use a lock 
    // if concurrency is required.
    internal class ShadowCopyManager : IDisposable
    {
        // Note that this class uses the *existance* of the Mutex to lock a directory.
        //
        // Nothing in this code actually ever acquires the Mutex, we just try to see if it exists
        // already.
        private readonly Mutex _mutex;

        private int _counter;

        public ShadowCopyManager(string baseDirectory = null)
        {
            BaseDirectory = baseDirectory ?? Path.Combine(Path.GetTempPath(), "Razor", "ShadowCopy");

            var guid = Guid.NewGuid().ToString("N").ToLowerInvariant();
            UniqueDirectory = Path.Combine(BaseDirectory, guid);

            _mutex = new Mutex(initiallyOwned: false, name: guid);

            Directory.CreateDirectory(UniqueDirectory);
        }

        public string BaseDirectory { get; }

        public string UniqueDirectory { get; }

        public string AddAssembly(string filePath)
        {
            var assemblyDirectory = CreateUniqueDirectory();

            var destination = Path.Combine(assemblyDirectory, Path.GetFileName(filePath));
            CopyFile(filePath, destination);
            
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var resourcesNameWithoutExtension = fileNameWithoutExtension + ".resources";
            var resourcesNameWithExtension = resourcesNameWithoutExtension + ".dll";

            foreach (var directory in Directory.EnumerateDirectories(Path.GetDirectoryName(filePath)))
            {
                var directoryName = Path.GetFileName(directory);

                var resourcesPath = Path.Combine(directory, resourcesNameWithExtension);
                if (File.Exists(resourcesPath))
                {
                    var resourcesShadowCopyPath = Path.Combine(assemblyDirectory, directoryName, resourcesNameWithExtension);
                    CopyFile(resourcesPath, resourcesShadowCopyPath);
                }

                resourcesPath = Path.Combine(directory, resourcesNameWithoutExtension, resourcesNameWithExtension);
                if (File.Exists(resourcesPath))
                {
                    var resourcesShadowCopyPath = Path.Combine(assemblyDirectory, directoryName, resourcesNameWithoutExtension, resourcesNameWithExtension);
                    CopyFile(resourcesPath, resourcesShadowCopyPath);
                }
            }

            return destination;
        }

        public void Dispose()
        {
            _mutex.ReleaseMutex();
        }

        public Task PurgeUnusedDirectoriesAsync()
        {
            return Task.Run((Action)PurgeUnusedDirectories);
        }

        private string CreateUniqueDirectory()
        {
            var id = _counter++;

            var directory = Path.Combine(UniqueDirectory, id.ToString());
            Directory.CreateDirectory(directory);
            return directory;
        }

        private void CopyFile(string originalPath, string shadowCopyPath)
        {
            var directory = Path.GetDirectoryName(shadowCopyPath);
            Directory.CreateDirectory(directory);

            File.Copy(originalPath, shadowCopyPath);

            MakeWritable(new FileInfo(shadowCopyPath));
        }

        private void MakeWritable(string directoryPath)
        {
            var directory = new DirectoryInfo(directoryPath);

            foreach (var file in directory.EnumerateFiles(searchPattern: "*", searchOption: SearchOption.AllDirectories))
            {
                MakeWritable(file);
            }
        }

        private void MakeWritable(FileInfo file)
        {
            try
            {
                if (file.IsReadOnly)
                {
                    file.IsReadOnly = false;
                }
            }
            catch
            {
                // There are many reasons this could fail. Ignore it and keep going.
            }
        }

        private void PurgeUnusedDirectories()
        {
            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(BaseDirectory);
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }

            foreach (var directory in directories)
            {
                Mutex mutex = null;
                try
                {
                    // We only want to try deleting the directory if no-one else is currently using it.
                    //
                    // Note that the mutex name is the name of the directory. This is OK because we're using
                    // GUIDs as directory/mutex names.
                    if (!Mutex.TryOpenExisting(Path.GetFileName(directory).ToLowerInvariant(), out mutex))
                    {
                        MakeWritable(directory);
                        Directory.Delete(directory, recursive: true);
                    }
                }
                catch
                {
                    // If something goes wrong we will leave it to the next run to clean up.
                    // Just swallow the exception and move on.
                }
                finally
                {
                    if (mutex != null)
                    {
                        mutex.Dispose();
                    }
                }
            }
        }
    }
}
