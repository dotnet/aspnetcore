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
        public const string ANCMTestFlagsEnvironmentVariable = "%ANCMTestFlags%";
        public const string ANCMTestFlagsDefaultContext = "AdminAnd64Bit";
        public const string ANCMTestFlagsTestSkipContext = "SkipTest";
        public const string ANCMTestFlagsUsePrivateAspNetCoreFileContext = "UsePrivateAspNetCoreFile";
        
        private static bool? _usePrivateAspNetCoreFile = null;
        public static bool? UsePrivateAspNetCoreFile
        {
            get {
                // 
                // By default, we don't use the private AspNetCore.dll that is compiled with this solution.
                // In order to use the private file, you should add 'UsePrivateAspNetCoreFile' flag to the Environmnet variable %ANCMTestFlag%.
                //
                //     Set ANCMTestFlag=%ANCMTestFlag%;UsePrivateAspNetCoreFile 
                //     Or
                //     $Env:ANCMTestFlag=$Env:ANCMTestFlag + ";UsePrivateAspNetCoreFile"
                //
                if (_usePrivateAspNetCoreFile == null)
                {
                    _usePrivateAspNetCoreFile = false;
                    var envValue = Environment.ExpandEnvironmentVariables(ANCMTestFlagsEnvironmentVariable);
                    if (envValue.ToLower().Contains(ANCMTestFlagsUsePrivateAspNetCoreFileContext.ToLower()))
                    {
                        TestUtility.LogInformation("PrivateAspNetCoreFile is set");
                        _usePrivateAspNetCoreFile = true;
                    }
                    else
                    {
                        TestUtility.LogInformation("PrivateAspNetCoreFile is not set");
                    }
                }
                return _usePrivateAspNetCoreFile;
            }
            set
            {
                _usePrivateAspNetCoreFile = value;
            }
        }

        public static int SiteId = 40000;
        public const string PrivateFileName = "aspnetcore_private.dll";
        public static string FullIisAspnetcore_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", PrivateFileName);
        public static string FullIisAspnetcore_path_original = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "aspnetcore.dll");
        public static string FullIisAspnetcore_X86_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "syswow64", "inetsrv", PrivateFileName);
        public static string IisExpressAspnetcore_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "IIS Express", PrivateFileName);
        public static string IisExpressAspnetcore_X86_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "IIS Express", PrivateFileName);

        public static string IisExpressAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "IIS Express", "config", "schema", "aspnetcore_schema.xml");
        public static string IisExpressAspnetcoreSchema_X86_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "IIS Express", "config", "schema", "aspnetcore_schema.xml");
        public static string FullIisAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "config", "schema", "aspnetcore_schema.xml");
        public static int _referenceCount = 0;
        private static bool _InitializeTestMachineCompleted = false;
        private string _setupScriptPath = null;
        
        private bool CheckPerquisiteForANCMTest()
        {
            bool result = true;
            TestUtility.LogInformation("CheckPerquisiteForANCMTest(): Environment.Is64BitOperatingSystem: {0}, Environment.Is64BitProcess {1}", Environment.Is64BitOperatingSystem, Environment.Is64BitProcess);
            TestUtility.LogInformation("%ANCMTestFlags%: {0}", Environment.ExpandEnvironmentVariables(ANCMTestFlagsEnvironmentVariable));

            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                TestUtility.LogInformation("CheckPerquisiteForANCMTest() Failed: ANCM test should be started with x64 process mode on 64 bit machine; if you run this test on Visual Studio, you should set X64 first after selecting 'Test -> Test Settings -> Default Process Architecture' menu");
                result = false;
            }
            return result;
        }
        public InitializeTestMachine()
        {
            _referenceCount++;

            if (_referenceCount == 1)
            {
                CheckPerquisiteForANCMTest();

                TestUtility.LogInformation("InitializeTestMachine::InitializeTestMachine() Start");

                _InitializeTestMachineCompleted = false;

                TestUtility.LogInformation("InitializeTestMachine::Start");
                if (Environment.ExpandEnvironmentVariables("%ANCMTEST_DEBUG%").ToLower() == "true")
                {
                    System.Diagnostics.Debugger.Launch();                    
                }

                // check Makecert.exe exists
                try
                {
                    string makecertExeFilePath = TestUtility.GetMakeCertPath();
                    TestUtility.RunCommand(makecertExeFilePath, null, true, true);
                    TestUtility.LogInformation("Verified makecert.exe is available : " + makecertExeFilePath);
                }
                catch (Exception ex)
                {
                    throw new System.ApplicationException("makecert.exe is not available : " + ex.Message);
                }

                TestUtility.ResetHelper(ResetHelperMode.KillIISExpress);

                // check if we can use IIS server instead of IISExpress
                try
                {
                    IISConfigUtility.IsIISReady = false;
                    if (IISConfigUtility.IsIISInstalled == true)
                    {
                        if (Environment.GetEnvironmentVariable("ANCMTEST_USE_IISEXPRESS") != null && Environment.GetEnvironmentVariable("ANCMTEST_USE_IISEXPRESS").Equals("true", StringComparison.InvariantCultureIgnoreCase))
                        {   
                            throw new System.ApplicationException("'ANCMTestServerType' environment variable is set to 'true'");
                        }

                        // check websocket is installed
                        if (File.Exists(Path.Combine(IISConfigUtility.Strings.IIS64BitPath, "iiswsock.dll")))
                        {
                            TestUtility.LogInformation("Websocket is installed");
                        }
                        else
                        {
                            throw new System.ApplicationException("websocket module is not installed");
                        }

                        TestUtility.ResetHelper(ResetHelperMode.KillWorkerProcess);

                        // Reset applicationhost.config
                        TestUtility.LogInformation("Restoring applicationhost.config");                        
                        IISConfigUtility.RestoreAppHostConfig(restoreFromMasterBackupFile:true);
                        TestUtility.StartW3svc();

                        // check w3svc is running after resetting applicationhost.config
                        if (IISConfigUtility.GetServiceStatus("w3svc") == "Running")
                        {
                            TestUtility.LogInformation("W3SVC service is restarted after restoring applicationhost.config");
                        }
                        else
                        {
                            throw new System.ApplicationException("WWW service can't start");
                        }

                        // check URLRewrite module exists
                        if (File.Exists(Path.Combine(IISConfigUtility.Strings.IIS64BitPath, "rewrite.dll")))
                        {
                            TestUtility.LogInformation("Verified URL Rewrite module installed for IIS server");
                        }
                        else
                        {
                            throw new System.ApplicationException("URL Rewrite module is not installed");
                        }

                        if (IISConfigUtility.ApppHostTemporaryBackupFileExtention == null)
                        {
                            throw new System.ApplicationException("Failed to backup applicationhost.config");
                        }
                        IISConfigUtility.IsIISReady = true;
                    }
                }
                catch (Exception ex)
                {
                    RollbackIISApplicationhostConfigFile();
                    TestUtility.LogInformation("We will use IISExpress instead of IIS: " + ex.Message);
                }

                string siteRootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "inetpub", "ANCMTest");
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
                
                if (InitializeTestMachine.UsePrivateAspNetCoreFile == true)
                {
                    PreparePrivateANCMFiles();

                    // update applicationhost.config for IIS server
                    if (IISConfigUtility.IsIISReady)
                    {
                        using (var iisConfig = new IISConfigUtility(ServerType.IIS, null))
                        {
                            iisConfig.AddModule("AspNetCoreModule", FullIisAspnetcore_path, null);
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
                RollbackIISApplicationhostConfigFile();
                TestUtility.LogInformation("InitializeTestMachine::Dispose() End");
            }
        }

        private void RollbackIISApplicationhostConfigFile()
        {
            if (IISConfigUtility.ApppHostTemporaryBackupFileExtention != null)
            {
                try
                {
                    TestUtility.ResetHelper(ResetHelperMode.KillWorkerProcess);
                }
                catch
                {
                    TestUtility.LogInformation("Failed to stop IIS worker processes");
                }
                try
                {
                    IISConfigUtility.RestoreAppHostConfig(restoreFromMasterBackupFile: false);
                }
                catch
                {
                    TestUtility.LogInformation("Failed to rollback applicationhost.config");
                }
                try
                {
                    TestUtility.StartW3svc();
                }
                catch
                {
                    TestUtility.LogInformation("Failed to start w3svc");
                }
                IISConfigUtility.ApppHostTemporaryBackupFileExtention = null;
            }
        }

        private void PreparePrivateANCMFiles()
        {
            var solutionRoot = GetSolutionDirectory();
            string outputPath = string.Empty;
            _setupScriptPath = Path.Combine(solutionRoot, "tools");

            // First try with release build
            outputPath = Path.Combine(solutionRoot, "artifacts", "build", "AspNetCore", "bin", "Release");

            // If release build is not available, try with debug build
            if (!File.Exists(Path.Combine(outputPath, "Win32", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore_schema.xml")))
            {
                outputPath = Path.Combine(solutionRoot, "artifacts", "build", "AspNetCore", "bin", "Debug");
            }
            
            if (!File.Exists(Path.Combine(outputPath, "Win32", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore_schema.xml")))
            {
                throw new ApplicationException("aspnetcore.dll is not available; check if there is any build issue!!!");
            }
            
            // create an extra private copy of the private file on IIS directory
            if (InitializeTestMachine.UsePrivateAspNetCoreFile == true)
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

                        string from = Path.Combine(outputPath, "x64", "aspnetcore.dll");
                        TestUtility.FileCopy(from, FullIisAspnetcore_path, overWrite:true, ignoreExceptionWhileDeletingExistingFile:false);
                        TestUtility.FileCopy(from, IisExpressAspnetcore_path, overWrite: true, ignoreExceptionWhileDeletingExistingFile: false);

                        // NOTE: schema file can't be overwritten, if there is any schema change, that should be updated manually
                        from = Path.Combine(outputPath, "x64", "aspnetcore_schema.xml");
                        TestUtility.FileCopy(from, FullIisAspnetcoreSchema_path, overWrite: false, ignoreExceptionWhileDeletingExistingFile: false);
                        TestUtility.FileCopy(from, IisExpressAspnetcoreSchema_path, overWrite: false, ignoreExceptionWhileDeletingExistingFile: false);

                        if (TestUtility.IsOSAmd64)
                        {
                            from = Path.Combine(outputPath, "Win32", "aspnetcore.dll");
                            TestUtility.FileCopy(from, FullIisAspnetcore_X86_path, overWrite: true, ignoreExceptionWhileDeletingExistingFile: false);
                            TestUtility.FileCopy(from, IisExpressAspnetcore_X86_path, overWrite: true, ignoreExceptionWhileDeletingExistingFile: false);

                            // NOTE: schema file can't be overwritten, if there is any schema change, that should be updated manually
                            from = Path.Combine(outputPath, "Win32", "aspnetcore_schema.xml");
                            TestUtility.FileCopy(from, IisExpressAspnetcoreSchema_X86_path, overWrite: false, ignoreExceptionWhileDeletingExistingFile: false);
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
