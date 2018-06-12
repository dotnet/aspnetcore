using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CODESIGN.Submitter;

namespace CodeSign
{
    public class SubmitCodeSignJob : IDisposable
    {
        const string Server = "codesign.gtm.microsoft.com";
        const int Port = 9556;
        static readonly string Submitter = Environment.GetEnvironmentVariable("USERNAME");
        readonly string _tempDirectory;
        readonly string _stagingDirectory;

        public SubmitCodeSignJob()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _stagingDirectory = Path.Combine(_tempDirectory, "staging-dir");
        }

        public List<string> Approvers { get; set; }

        public List<string> Certificates { get; set; }

        public bool LocalBuild { get; set; }

        public string Description { get; set; }

        public string DisplayName { get; set; }

        public string DisplayUrl { get; set; }

        public string JobName { get; set; }

        public string LogDir { get; set; }

        public List<string> Files { get; set; }

        public void Execute()
        {
            Directory.CreateDirectory(_stagingDirectory);

            Validate();

            var fileMapping = StageFiles();

            Console.WriteLine($"Job '{JobName}' contains {fileMapping.Count} unique file(s)");

            var sb = new StringBuilder();
            foreach (var group in fileMapping)
            {
                var first = group.Value[0];
                var fileName = Path.GetFileName(first.OriginalPath);
                sb.Append(fileName).Append(", hash=").AppendLine(first.Hash);
                foreach (var request in group.Value)
                {
                    sb.Append("      ").AppendLine(request.OriginalPath);
                }
                sb.AppendLine();
            }

            File.WriteAllText(Path.Combine(LogDir, $"signjob-{JobName}-files.txt"), sb.ToString());

            string completionPath;
            if (!LocalBuild)
            {
                var job = InitializeJob(fileMapping.Keys.Select(k => Path.Combine(_stagingDirectory, k)));
                job.Send();
                var timer = Stopwatch.StartNew();
                Console.WriteLine($"Job #{job.JobNumber} was sent.");
                WatchJob(job);
                timer.Stop();
                Console.WriteLine($"Job #{job.JobNumber} finished at '{job.JobCompletionPath}' in {timer.ElapsedMilliseconds / 1000} sec.");
                completionPath = job.JobCompletionPath;
            }
            else
            {
                var testSignedDir = Directory.CreateDirectory(Path.Combine(_tempDirectory, "test-sign")).FullName;
                // Copy the files to another directory so we can exercise CopyStagedFiles
                Parallel.ForEach(new DirectoryInfo(_stagingDirectory).EnumerateFiles(), file =>
                {
                    var target = Path.Combine(testSignedDir, file.Name);
                    file.CopyTo(target, overwrite: true);
                });

                completionPath = testSignedDir;
            }

            CopyStagedFiles(completionPath, fileMapping);
        }

        private void Validate()
        {
            // At least two approvers are required
            if (Approvers == null || Approvers.Count < 2)
            {
                throw new Exception("Too few code-sign approvers. At least two approvers must be specified excluding the person submitting the job.");
            }

            // At least one certificate must be selected
            if (Certificates == null || Certificates.Count == 0)
            {
                throw new Exception("No certificates specified.");
            }

            if (string.IsNullOrEmpty(DisplayName))
            {
                throw new Exception("The DisplayName property cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(DisplayUrl))
            {
                throw new Exception("The DisplayUrl property cannot be null or empty.");
            }

            if (Files == null || Files.Count == 0)
            {
                throw new Exception("The submissions does not contain any files.");
            }

            foreach (var file in Files)
            {
                if (!File.Exists(file))
                {
                    throw new FileNotFoundException($"Cannot find {file}. Verify that the file exists.", file);
                }
            }
        }

        private IReadOnlyDictionary<string, List<FileRequest>> StageFiles()
        {
            Directory.CreateDirectory(_stagingDirectory);

            var fileMapping = new ConcurrentDictionary<string, List<FileRequest>>(StringComparer.OrdinalIgnoreCase);
            Parallel.ForEach(Files, f =>
            {
                var hash = GetFileHash(f);
                var mapKey = hash + Path.GetExtension(f);
                var request = new FileRequest
                {
                    OriginalPath = f,
                    Hash = hash,
                };
                fileMapping.AddOrUpdate(mapKey,
                    (_) => { return new List<FileRequest> { request }; },
                    (_, list) => { list.Add(request); return list; });
            });

            foreach (var item in fileMapping)
            {
                var mappedFilePath = Path.Combine(_stagingDirectory, item.Key);
                File.Copy(item.Value[0].OriginalPath, mappedFilePath, overwrite: true);
            }

            return fileMapping;
        }

        private static string GetFileHash(string path)
        {
            byte[] hash;
            using (var algorithm = new SHA256Managed())
            using (var file = File.OpenRead(path))
            {
                hash = algorithm.ComputeHash(file);
            }

            var sb = new StringBuilder();
            foreach (var b in hash)
            {
                sb.AppendFormat("{0:X2}", b);
            }

            return sb.ToString();
        }

        private Job InitializeJob(IEnumerable<string> files)
        {
            // Begin job submission
            var isSSL = true;
            var job = Job.Initialize(Server, Port, Mode: isSSL);
            job.IsRequireHash = true;

            // Add miscellaneous items and properties
            if (!string.IsNullOrEmpty(Description))
            {
                job.Description = Description;
            }
            // Add certificates
            foreach (var certificate in Certificates)
            {
                job.SelectCertificate(certificate);
            }

            // Add approvers and exclude the submitter from the list
            foreach (var approver in Approvers.Except(new[] { Submitter }))
            {
                job.AddApprover(approver);
            }

            // Set notification events
            job.SetNotification(Submitter, new[]
            {
                CODESIGN.NotificationEventTypeEnum.JobCompletionFailure,
                CODESIGN.NotificationEventTypeEnum.JobCompletionSuccess,
                CODESIGN.NotificationEventTypeEnum.JobVirusScanFailure,
                CODESIGN.NotificationEventTypeEnum.JobApprovalInitiate
            });

            job.SetNotification(job.ApproverList.Values, new[]
            {
                CODESIGN.NotificationEventTypeEnum.JobApprovalFailure,
                CODESIGN.NotificationEventTypeEnum.JobArchivalFailure,
                CODESIGN.NotificationEventTypeEnum.JobCompletionFailure,
                CODESIGN.NotificationEventTypeEnum.JobSigningFailure,
                CODESIGN.NotificationEventTypeEnum.JobSubmissionFailure,
                CODESIGN.NotificationEventTypeEnum.JobVirusScanFailure
            });

            // Add files
            foreach (var file in files)
            {
                job.AddFile(file, DisplayName, DisplayUrl, CODESIGN.JavaPermissionsTypeEnum.None);
            }

            return job;
        }

        private void WatchJob(Job job)
        {
            using (var jw = new JobWatcher())
            {
                do
                {
                    jw.Watch(job.JobNumber, Server, Port, IsSSL: true, IsImmediate: false);
                } while (!jw.IsDone);

                if (jw.ErrorList.Count > 0)
                {
                    var messages = jw.ErrorList.Values.Cast<JobError>().Select(v => $"{v.Number}: {v.Description} {v.Explanation}");
                    throw new Exception(string.Join(Environment.NewLine, messages));
                }
            }
        }

        private void CopyStagedFiles(string completionPath, IReadOnlyDictionary<string, List<FileRequest>> fileMappings)
        {
            var copiedFiles = new ConcurrentBag<string>();
            Parallel.ForEach(new DirectoryInfo(completionPath).EnumerateFiles(), file =>
            {
                // Calculate the destination using the name of the drop share. e.g.
                // obj\extracted\Lib.dll -> c:\temp\staging-dir\HASHABC123 -> $completionPath\HASHABC123 -> obj\extracted\Lib.dll
                if (fileMappings.TryGetValue(file.Name, out var requests))
                {
                    foreach (var dest in requests)
                    {
                        file.CopyTo(dest.OriginalPath, overwrite: true);
                    }
                    copiedFiles.Add(file.Name);
                }
                else
                {
                    Console.WriteLine($"warning: Unknown file {file.Name} at job completion path {completionPath}.");
                }
            });

            if (copiedFiles.Count != fileMappings.Count)
            {
                var missingFiles = fileMappings.Keys.Except(copiedFiles, StringComparer.OrdinalIgnoreCase);
                throw new Exception($"The following files were not found in the drop location:{Environment.NewLine}{missingFiles}");
            }
        }

        private struct FileRequest
        {
            public string OriginalPath;
            public string Hash;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Don't throw if we fail to cleanup
            }
        }
    }
}
