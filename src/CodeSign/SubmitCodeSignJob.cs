using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public List<string> Files { get; set; }

        public void Execute()
        {
            Directory.CreateDirectory(_stagingDirectory);

            Validate();

            var fileMapping = StageFiles();

            string completionPath;
            if (!LocalBuild)
            {
                var job = InitializeJob(fileMapping.Keys);
                job.Send();
                Console.WriteLine($"Job #{job.JobNumber} was sent.");
                WatchJob(job);
                Console.WriteLine($"Job #{job.JobNumber} finished at '{job.JobCompletionPath}'.");
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

        private Dictionary<string, string> StageFiles()
        {
            Directory.CreateDirectory(_stagingDirectory);
            var fileMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < Files.Count; i++)
            {
                var file = Files[i];
                var mappedFileName = $"{i}_{Path.GetFileName(file)}";
                var mappedFilePath = Path.Combine(_stagingDirectory, mappedFileName);

                fileMapping.Add(mappedFilePath, file);
                File.Copy(file, mappedFilePath, overwrite: true);
            }

            return fileMapping;
        }

        private Job InitializeJob(ICollection<string> files)
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
            foreach (var approver in Approvers.Except(new [] { Submitter }))
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

        private void CopyStagedFiles(string completionPath, Dictionary<string, string> fileMappings)
        {
            var copiedFiles = new ConcurrentBag<string>();
            Parallel.ForEach(new DirectoryInfo(completionPath).EnumerateFiles(), file =>
            {
                // Calculate the destination using the name of the drop share. e.g.
                // \\drop-share\1.dll -> c:\temp\staging-dir\1.dll -> obj\extracted\Lib.dll
                var mappedLocation = Path.Combine(_stagingDirectory, file.Name);
                if (fileMappings.TryGetValue(mappedLocation, out var finalDestination))
                {
                    file.CopyTo(finalDestination, overwrite: true);
                    copiedFiles.Add(mappedLocation);
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
