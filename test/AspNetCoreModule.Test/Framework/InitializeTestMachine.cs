// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.PlatformAbstractions;

namespace AspNetCoreModule.Test.Framework
{
    public class InitializeTestMachine : IDisposable
    {
        // 
        // By default, we don't use the private AspNetCoreFile
        // 
        public static bool UsePrivateAspNetCoreFile = false;

        public static int SiteId = 40000;
        public static string Aspnetcore_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "aspnetcore_private.dll");
        public static string Aspnetcore_path_original = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "aspnetcore.dll");
        public static string Aspnetcore_X86_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "syswow64", "inetsrv", "aspnetcore_private.dll");
        public static string IISExpressAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "IIS Express", "config", "schema", "aspnetcore_schema.xml");
        public static string IISAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "config", "schema", "aspnetcore_schema.xml");
        public static int _referenceCount = 0;
        private static bool _InitializeTestMachineCompleted = false;
        private string _setupScriptPath = null;
        
        public InitializeTestMachine()
        {
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                TestUtility.LogInformation("Error!!! Skipping to run InitializeTestMachine::InitializeTestMachine() because the test process is started on syswow mode");
                throw new NotSupportedException("Running this test progrom in syswow64 mode is not supported");                
            }
            _referenceCount++;

            if (_referenceCount == 1)
            {
                TestUtility.LogInformation("InitializeTestMachine::InitializeTestMachine() Start");

                _InitializeTestMachineCompleted = false;

                TestUtility.LogInformation("InitializeTestMachine::Start");
                if (Environment.ExpandEnvironmentVariables("%ANCMDebug%").ToLower() == "true")
                {
                    System.Diagnostics.Debugger.Launch();                    
                }

                TestUtility.ResetHelper(ResetHelperMode.KillIISExpress);
                TestUtility.ResetHelper(ResetHelperMode.KillWorkerProcess);
                // cleanup before starting
                string siteRootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "inetpub", "ANCMTest");
                try
                {
                    if (IISConfigUtility.IsIISInstalled == true)
                    {
                        IISConfigUtility.RestoreAppHostConfig();                        
                    }
                }
                catch
                {
                    TestUtility.LogInformation("Failed to restore applicationhost.config");
                }

                if (!Directory.Exists(siteRootPath))
                {
                    Directory.CreateDirectory(siteRootPath);
                }

                foreach (string directory in Directory.GetDirectories(siteRootPath))
                {
                    bool successDeleteChildDirectory = true;
                    try
                    {
                        TestUtility.DeleteDirectory(directory);
                    }
                    catch
                    {
                        successDeleteChildDirectory = false;
                        TestUtility.LogInformation("Failed to delete " + directory);
                    }
                    if (successDeleteChildDirectory)
                    {
                        try
                        {
                            TestUtility.DeleteDirectory(siteRootPath);
                        }
                        catch
                        {
                            TestUtility.LogInformation("Failed to delete " + siteRootPath);
                        }
                    }
                }
                
                if (InitializeTestMachine.UsePrivateAspNetCoreFile)
                {
                    PreparePrivateANCMFiles();

                    // update applicationhost.config for IIS server
                    if (IISConfigUtility.IsIISInstalled == true)
                    {

                        using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                        {
                            iisConfig.AddModule("AspNetCoreModule", Aspnetcore_path, null);
                        }
                    }
                }
                
                _InitializeTestMachineCompleted = true;

                TestUtility.LogInformation("InitializeTestMachine::InitializeTestMachine() End");
            }

            for (int i=0; i<120; i++)                    
            {
                if (_InitializeTestMachineCompleted)
                {
                    break;
                }   
                else
                {
                    TestUtility.LogInformation("InitializeTestMachine::InitializeTestMachine() Waiting...");
                    Thread.Sleep(500);
                }                 
            }
            if (!_InitializeTestMachineCompleted)
            {
                throw new System.ApplicationException("InitializeTestMachine failed");
            }
        }

        public void Dispose()
        {
            _referenceCount--;

            if (_referenceCount == 0)
            {
                TestUtility.LogInformation("InitializeTestMachine::Dispose() Start");
                TestUtility.ResetHelper(ResetHelperMode.KillIISExpress);

                if (InitializeTestMachine.UsePrivateAspNetCoreFile)
                {
                    if (IISConfigUtility.IsIISInstalled == true)
                    {
                        using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                        {
                            try
                            {
                                iisConfig.AddModule("AspNetCoreModule", Aspnetcore_path_original, null);
                            }
                            catch
                            {
                                TestUtility.LogInformation("Failed to restore aspnetcore.dll path!!!");
                            }
                        }
                    }
                }
                TestUtility.LogInformation("InitializeTestMachine::Dispose() End");
            }
        }
        
        private void PreparePrivateANCMFiles()
        {
            var solutionRoot = GetSolutionDirectory();
            string outputPath = string.Empty;
            _setupScriptPath = Path.Combine(solutionRoot, "tools");

            // First try with debug build
            outputPath = Path.Combine(solutionRoot, "artifacts", "build", "AspNetCore", "bin", "Debug");

            // If debug build does is not available, try with release build
            if (!File.Exists(Path.Combine(outputPath, "Win32", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore_schema.xml")))
            {
                outputPath = Path.Combine(solutionRoot, "artifacts", "build", "AspNetCore", "bin", "Release");
            }

            if (!File.Exists(Path.Combine(outputPath, "Win32", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore_schema.xml")))
            {
                outputPath = Path.Combine(solutionRoot, "src", "AspNetCore", "bin", "Debug");
            }

            if (!File.Exists(Path.Combine(outputPath, "Win32", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore_schema.xml")))
            {
                throw new ApplicationException("aspnetcore.dll is not available; build aspnetcore.dll for both x86 and x64 and then try again!!!");
            }
            
            // create an extra private copy of the private file on IISExpress directory
            if (InitializeTestMachine.UsePrivateAspNetCoreFile)
            {
                bool updateSuccess = false;

                for (int i = 0; i < 3; i++)
                {
                    updateSuccess = false;
                    try
                    {
                        TestUtility.ResetHelper(ResetHelperMode.KillWorkerProcess);
                        TestUtility.ResetHelper(ResetHelperMode.StopW3svcStartW3svc);
                        Thread.Sleep(1000);
                        TestUtility.FileCopy(Path.Combine(outputPath, "x64", "aspnetcore.dll"), Aspnetcore_path);
                        if (TestUtility.IsOSAmd64)
                        {
                            TestUtility.FileCopy(Path.Combine(outputPath, "Win32", "aspnetcore.dll"), Aspnetcore_X86_path);
                        }
                        updateSuccess = true;
                    }
                    catch
                    {
                        updateSuccess = false;
                    }
                    if (updateSuccess)
                    {
                        break;
                    }
                }
                if (!updateSuccess)
                {
                    throw new System.ApplicationException("Failed to update aspnetcore.dll");
                }
            }
        }

        public static string GetSolutionDirectory()
        {
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;
            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionFile = new FileInfo(Path.Combine(directoryInfo.FullName, "AspNetCoreModule.sln"));
                if (solutionFile.Exists)
                {
                    return directoryInfo.FullName;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Solution root could not be located using application root {applicationBasePath}.");
        }
    }
}
