using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Logging;
using NuGet;

namespace NuGetClone
{
    public class MyService : ServiceBase
    {
        private static readonly Uri DeveloperFeed = new Uri("https://www.myget.org/F/aspnetvnext/api/v2");
        private static readonly ICredentials _credentials = new NetworkCredential("aspnetreadonly", "4d8a2d9c-7b80-4162-9978-47e918c9658c");

        private Timer _timer;
        private string _targetDirectory;

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            Init();
            _timer = new Timer(Run, null, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2));
        }

        public void Init()
        {
            _targetDirectory = Environment.GetEnvironmentVariable("PROJECTK_PACKAGE_CACHE");
            if (string.IsNullOrEmpty(_targetDirectory))
            {
                _targetDirectory = @"c:\projectk-cache";
            }

            var fileTraceListener = new FileLogTraceListener
            {
                AutoFlush = true,
                Location = LogFileLocation.Custom,
                CustomLocation = _targetDirectory,
                BaseFileName = "ProjectKClone",
                TraceOutputOptions = TraceOptions.DateTime,
                LogFileCreationSchedule = LogFileCreationScheduleOption.Weekly
            };
            Trace.Listeners.Add(fileTraceListener);
        }

        public void Run(object state)
        {
            try
            {
                RunFromGallery();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("{0}: ERROR {1}", DateTime.Now, ex));
            }
        }

        public void RunFromGallery()
        {
            Directory.CreateDirectory(_targetDirectory);

            var client = new HttpClient(DeveloperFeed);
            client.SendingRequest += (sender, e) =>
            {
                e.Request.Credentials = _credentials;
                e.Request.PreAuthenticate = true;
            };
            var remoteRepo = new DataServicePackageRepository(client);
            var targetRepo = new LocalPackageRepository(_targetDirectory);
            var packages = remoteRepo.GetPackages()
                                     .Where(p => p.IsAbsoluteLatestVersion)
                                     .ToList();
            Parallel.ForEach(packages,
                             new ParallelOptions { MaxDegreeOfParallelism = 4 },
                             package =>
                             {
                                 // Some packages are updated without revving the version. We'll only opt not to re-download
                                 // a package if an identical version does not exist on disk.
                                 var existingPackage = targetRepo.FindPackage(package.Id, package.Version);
                                 var dataServicePackage = (DataServicePackage)package;
                                 if (existingPackage == null ||
                                     !existingPackage.GetHash(dataServicePackage.PackageHashAlgorithm).Equals(dataServicePackage.PackageHash, StringComparison.Ordinal))
                                 {
                                     Trace.WriteLine(string.Format("{0}: Adding package {1}", DateTime.Now, package.GetFullName()));
                                     var packagePath = GetPackagePath(package);

                                     using (var input = package.GetStream())
                                     using (var output = File.Create(packagePath))
                                     {
                                         input.CopyTo(output);
                                     }

                                     PurgeOldVersions(targetRepo, package);
                                 }
                             });
        }

        private string GetPackagePath(IPackage package)
        {
            return Path.Combine(_targetDirectory, package.Id + "." + package.Version + ".nupkg");
        }

        private void PurgeOldVersions(LocalPackageRepository targetRepo, IPackage package)
        {
            foreach (var oldPackage in targetRepo.FindPackagesById(package.Id).Where(p => p.Version < package.Version))
            {
                try
                {
                    var path = GetPackagePath(oldPackage);
                    Trace.WriteLine(string.Format("Deleting package {0}", oldPackage.GetFullName()));
                    File.Delete(path);
                }
                catch
                {
                    // Ignore
                }
            }
        }


        protected override void OnStop()
        {
            base.OnStop();
        }
    }
}
