// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.PlatformAbstractions;
using System.Security.Principal;
using System.Security.AccessControl;

namespace AspNetCoreModule.Test.Framework
{
    public static class TestFlags
    {
        public const string SkipTest = "SkipTest";
        public const string UsePrivateANCM = "UsePrivateANCM";
        public const string UseIISExpress = "UseIISExpress";
        public const string UseFullIIS = "UseFullIIS";
        public const string RunAsAdministrator = "RunAsAdministrator";
        public const string MakeCertExeAvailable = "MakeCertExeAvailable";
        public const string WebSocketModuleAvailable = "WebSocketModuleAvailable";
        public const string UrlRewriteModuleAvailable = "UrlRewriteModuleAvailable";
        public const string X86Platform = "X86Platform";
        public const string Wow64BitMode = "Wow64BitMode";
        public const string RequireRunAsAdministrator = "RequireRunAsAdministrator";
        public const string Default = "Default";

        public static bool Enabled(string flagValue)
        {
            return InitializeTestMachine.GlobalTestFlags.Contains(flagValue.ToLower());
        }
    }

    public class InitializeTestMachine : IDisposable
    {
        public const string ANCMTestFlagsEnvironmentVariable = "%ANCMTestFlags%";
        
        public static int SiteId = 40000;
        public const string PrivateFileName = "aspnetcore_private.dll";
        public static string FullIisAspnetcore_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", PrivateFileName);
        public static string FullIisAspnetcore_path_original = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "aspnetcore.dll");
        public static string FullIisAspnetcore_X86_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "syswow64", "inetsrv", PrivateFileName);
        public static string IisExpressAspnetcore_path;
        public static string IisExpressAspnetcore_X86_path;

        public static string IisExpressAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "IIS Express", "config", "schema", "aspnetcore_schema.xml");
        public static string IisExpressAspnetcoreSchema_X86_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "IIS Express", "config", "schema", "aspnetcore_schema.xml");
        public static string FullIisAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "config", "schema", "aspnetcore_schema.xml");
        public static int _referenceCount = 0;
        private static bool _InitializeTestMachineCompleted = false;
        private string _setupScriptPath = null;
        
        private static bool? _makeCertExeAvailable = null;
        public static bool MakeCertExeAvailable
        {
            get
            {
                if (_makeCertExeAvailable == null)
                {
                    _makeCertExeAvailable = false;
                    try
                    {
                        string makecertExeFilePath = TestUtility.GetMakeCertPath();
                        TestUtility.RunCommand(makecertExeFilePath, null, true, true);
                        TestUtility.LogInformation("Verified makecert.exe is available : " + makecertExeFilePath);
                        _makeCertExeAvailable = true;
                    }
                    catch
                    {
                        // ignore exception
                    }
                }
                return (_makeCertExeAvailable == true);
            }
        }

        public static string TestRootDirectory
        {
            get
            {
                return Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "_ANCMTest");
            }
        }

        private static string _globalTestFlags = null;
        public static string GlobalTestFlags
        {
            get
            {
                if (_globalTestFlags == null)
                {
                    WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                    bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

                    // check if this test process is started with the Run As Administrator start option
                    _globalTestFlags = Environment.ExpandEnvironmentVariables(ANCMTestFlagsEnvironmentVariable);

                    //
                    // Check if ANCMTestFlags environment is not defined and the test program was started 
                    // without using the Run As Administrator start option. 
                    // In that case, we have to use the default TestFlags of UseIISExpress and UsePrivateANCM
                    //
                    if (!isElevated)
                    {
                        if (_globalTestFlags.ToLower().Contains("%" + ANCMTestFlagsEnvironmentVariable.ToLower() + "%"))
                        {
                            _globalTestFlags = TestFlags.UsePrivateANCM + ";" + TestFlags.UseIISExpress;
                        }
                    }

                    //
                    // convert in lower case 
                    //
                    _globalTestFlags = _globalTestFlags.ToLower();

                    //
                    // error handling: UseIISExpress and UseFullIIS can be used together. 
                    //
                    if (_globalTestFlags.Contains(TestFlags.UseIISExpress.ToLower()) && _globalTestFlags.Contains(TestFlags.UseFullIIS.ToLower()))
                    {
                        _globalTestFlags = _globalTestFlags.Replace(TestFlags.UseFullIIS.ToLower(), "");                        
                    }

                    //
                    // adjust the default test context in run time to figure out wrong test context values
                    //
                    if (isElevated)
                    {
                        // add RunAsAdministrator
                        if (!_globalTestFlags.Contains(TestFlags.RunAsAdministrator.ToLower()))
                        {
                            TestUtility.LogInformation("Added test context of " + TestFlags.RunAsAdministrator);
                            _globalTestFlags += ";" + TestFlags.RunAsAdministrator;
                        }
                    }
                    else
                    {
                        // add UseIISExpress
                        if (!_globalTestFlags.Contains(TestFlags.UseIISExpress.ToLower()))
                        {
                            TestUtility.LogInformation("Added test context of " + TestFlags.UseIISExpress);
                            _globalTestFlags += ";" + TestFlags.UseIISExpress;
                        }

                        // remove UseFullIIS
                        if (_globalTestFlags.Contains(TestFlags.UseFullIIS.ToLower()))
                        {
                            _globalTestFlags = _globalTestFlags.Replace(TestFlags.UseFullIIS.ToLower(), "");
                        }

                        // remove RunAsAdmistrator
                        if (_globalTestFlags.Contains(TestFlags.RunAsAdministrator.ToLower()))
                        {
                            _globalTestFlags = _globalTestFlags.Replace(TestFlags.RunAsAdministrator.ToLower(), "");
                        }
                    }

                    if (MakeCertExeAvailable)
                    {
                        // Add MakeCertExeAvailable
                        if (!_globalTestFlags.Contains(TestFlags.MakeCertExeAvailable.ToLower()))
                        {
                            TestUtility.LogInformation("Added test context of " + TestFlags.MakeCertExeAvailable);
                            _globalTestFlags += ";" + TestFlags.MakeCertExeAvailable;
                        }
                    }

                    if (!Environment.Is64BitOperatingSystem)
                    {
                        // Add X86Platform
                        if (!_globalTestFlags.Contains(TestFlags.X86Platform.ToLower()))
                        {
                            TestUtility.LogInformation("Added test context of " + TestFlags.X86Platform);
                            _globalTestFlags += ";" + TestFlags.X86Platform;
                        }
                    }

                    if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                    {
                        // Add Wow64bitMode
                        if (!_globalTestFlags.Contains(TestFlags.Wow64BitMode.ToLower()))
                        {
                            TestUtility.LogInformation("Added test context of " + TestFlags.Wow64BitMode);
                            _globalTestFlags += ";" + TestFlags.Wow64BitMode;
                        }

                        // remove X86Platform
                        if (_globalTestFlags.Contains(TestFlags.X86Platform.ToLower()))
                        {
                            _globalTestFlags = _globalTestFlags.Replace(TestFlags.X86Platform.ToLower(), "");
                        }
                    }

                    if (File.Exists(Path.Combine(IISConfigUtility.Strings.IIS64BitPath, "iiswsock.dll")))
                    {
                        // Add WebSocketModuleAvailable
                        if (!_globalTestFlags.Contains(TestFlags.WebSocketModuleAvailable.ToLower()))
                        {
                            TestUtility.LogInformation("Added test context of " + TestFlags.WebSocketModuleAvailable);
                            _globalTestFlags += ";" + TestFlags.WebSocketModuleAvailable;
                        }
                    }

                    if (File.Exists(Path.Combine(IISConfigUtility.Strings.IIS64BitPath, "rewrite.dll")))
                    {
                        // Add UrlRewriteModuleAvailable
                        if (!_globalTestFlags.Contains(TestFlags.UrlRewriteModuleAvailable.ToLower()))
                        {
                            TestUtility.LogInformation("Added test context of " + TestFlags.UrlRewriteModuleAvailable);
                            _globalTestFlags += ";" + TestFlags.UrlRewriteModuleAvailable;
                        }
                    }

                    _globalTestFlags = _globalTestFlags.ToLower();
                }

                return _globalTestFlags;
            }
        }

        public void InitializeIISServer()
        {
            // Check if IIS server is installed or not
            bool isIISInstalled = true;
            if (!File.Exists(Path.Combine(IISConfigUtility.Strings.IIS64BitPath, "iiscore.dll")))
            {
                isIISInstalled = false;
            }

            if (!File.Exists(Path.Combine(IISConfigUtility.Strings.IIS64BitPath, "config", "applicationhost.config")))
            {
                isIISInstalled = false;
            }
            
            if (!isIISInstalled)
            {
                throw new ApplicationException("IIS server is not installed");
            }

            // Clean up IIS worker process
            TestUtility.ResetHelper(ResetHelperMode.KillWorkerProcess);

            // Reset applicationhost.config
            TestUtility.LogInformation("Restoring applicationhost.config");
            IISConfigUtility.RestoreAppHostConfig(restoreFromMasterBackupFile: true);
            TestUtility.StartW3svc();

            // check w3svc is running after resetting applicationhost.config
            if (IISConfigUtility.GetServiceStatus("w3svc") == "Running")
            {
                TestUtility.LogInformation("W3SVC service is restarted after restoring applicationhost.config");
            }
            else
            {
                throw new ApplicationException("WWW service can't start");
            }

            if (IISConfigUtility.ApppHostTemporaryBackupFileExtention == null)
            {
                throw new ApplicationException("Failed to backup applicationhost.config");
            }
        }

        public InitializeTestMachine()
        {
            _referenceCount++;

            // This method should be called only one time
            if (_referenceCount == 1)
            {
                TestUtility.LogInformation("InitializeTestMachine::InitializeTestMachine() Start");

                _InitializeTestMachineCompleted = false;

                TestUtility.LogInformation("InitializeTestMachine::Start");
                if (Environment.ExpandEnvironmentVariables("%ANCMTEST_DEBUG%").ToLower() == "true")
                {
                    System.Diagnostics.Debugger.Launch();
                }
                
                //
                // Clean up IISExpress processes
                //
                TestUtility.ResetHelper(ResetHelperMode.KillIISExpress);

                //
                // Initalize IIS server
                //

                if (TestFlags.Enabled(TestFlags.UseFullIIS))
                {
                    InitializeIISServer();
                }
                
                string siteRootPath = TestRootDirectory;
                if (!Directory.Exists(siteRootPath))
                {
                    //
                    // Create a new directory and set the write permission for the SID of AuthenticatedUser
                    //
                    Directory.CreateDirectory(siteRootPath);
                    DirectorySecurity sec = Directory.GetAccessControl(siteRootPath);
                    SecurityIdentifier authenticatedUser = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                    sec.AddAccessRule(new FileSystemAccessRule(authenticatedUser, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    Directory.SetAccessControl(siteRootPath, sec);
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
                
                //
                // Intialize Private ANCM files for Full IIS server or IISExpress
                //
                if (TestFlags.Enabled(TestFlags.UsePrivateANCM))
                {
                    PreparePrivateANCMFiles();
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
                throw new ApplicationException("InitializeTestMachine failed");
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

            //
            // NOTE: 
            // ANCM schema file can't be overwritten here
            // If there is any schema change, that should be updated with installing setup or manually copied with the new schema file.
            //

            if (TestFlags.Enabled(TestFlags.UseIISExpress))
            {
                //
                // Initialize 32 bit IisExpressAspnetcore_path
                //
                IisExpressAspnetcore_path = Path.Combine(outputPath, "x64", "aspnetcore.dll");
                IisExpressAspnetcore_X86_path = Path.Combine(outputPath, "Win32", "aspnetcore.dll");
            }
            else  // if use Full IIS server
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
                        
                        // Copy private file on Inetsrv directory
                        TestUtility.FileCopy(Path.Combine(outputPath, "x64", "aspnetcore.dll"), FullIisAspnetcore_path, overWrite: true, ignoreExceptionWhileDeletingExistingFile: false);
                                                
                        if (TestUtility.IsOSAmd64)
                        {
                            
                            // Copy 32bit private file on Inetsrv directory
                            TestUtility.FileCopy(Path.Combine(outputPath, "Win32", "aspnetcore.dll"), FullIisAspnetcore_X86_path, overWrite: true, ignoreExceptionWhileDeletingExistingFile: false);
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
                    throw new ApplicationException("Failed to update aspnetcore.dll");
                }

                // update applicationhost.config for IIS server with the new private ASPNET Core file name
                if (TestFlags.Enabled(TestFlags.UseFullIIS))
                {
                    using (var iisConfig = new IISConfigUtility(ServerType.IIS, null))
                    {
                        iisConfig.AddModule("AspNetCoreModule", FullIisAspnetcore_path, null);
                    }
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
