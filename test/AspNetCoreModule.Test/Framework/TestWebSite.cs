// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AspNetCoreModule.Test.Framework
{
    public class TestWebSite : IDisposable
    {
        static private bool _publishedAspnetCoreApp = false;

        public TestWebApplication RootAppContext;
        public TestWebApplication AspNetCoreApp;
        public TestWebApplication WebSocketApp;
        public TestWebApplication URLRewriteApp;
        public TestUtility testHelper;
        private ILogger _logger;
        private int _iisExpressPidBackup = -1;

        private string postfix = string.Empty;

        public void Dispose()
        {
            TestUtility.LogInformation("TestWebSite::Dispose() Start");

            if (_iisExpressPidBackup != -1)
            {
                var iisExpressProcess = Process.GetProcessById(Convert.ToInt32(_iisExpressPidBackup));
                try
                {
                    iisExpressProcess.Kill();
                    iisExpressProcess.WaitForExit();
                    iisExpressProcess.Close();
                }
                catch
                {
                    TestUtility.RunPowershellScript("stop-process -id " + _iisExpressPidBackup);
                }
            }
            TestUtility.LogInformation("TestWebSite::Dispose() End");
        }

        public string _hostName = null;
        public string HostName
        {
            get
            {
                if (_hostName == null)
                {
                    _hostName = "localhost";
                }
                return _hostName;
            }
            set
            {
                _hostName = value;
            }
        }

        public string _siteName = null;
        public string SiteName
        {
            get
            {
                return _siteName;
            }
            set
            {
                _siteName = value;
            }
        }

        public string _postFix = null;
        public string PostFix
        {
            get
            {
                return _postFix;
            }
            set
            {
                _postFix = value;
            }
        }

        public int _tcpPort = 8080;
        public int TcpPort
        {
            get
            {
                return _tcpPort;
            }
            set
            {
                _tcpPort = value;
            }
        }

        private int _workerProcessID = 0;
        public int WorkerProcessID
        {
            get
            {
                if (_workerProcessID == 0)
                {
                    try
                    {
                        if (IisServerType == ServerType.IISExpress)
                        {
                            _workerProcessID = Convert.ToInt32(TestUtility.GetProcessWMIAttributeValue("iisexpress.exe", "Handle", null));
                        }
                        else
                        {
                            _workerProcessID = Convert.ToInt32(TestUtility.GetProcessWMIAttributeValue("w3wp.exe", "Handle", null));
                        }
                    }
                    catch
                    {
                        TestUtility.LogInformation("Failed to get process id of w3wp.exe");
                    }
                }
                return _workerProcessID;
            }
            set
            {
                _workerProcessID = value;
            }
        }

        public ServerType IisServerType { get; set; }
        public string IisExpressConfigPath { get; set; }
        private int _siteId { get; set; }
        private IISConfigUtility.AppPoolBitness _appPoolBitness { get; set; }
        
        public TestWebSite(IISConfigUtility.AppPoolBitness appPoolBitness, string loggerPrefix = "ANCMTest", bool startIISExpress = true, bool copyAllPublishedFiles = false, bool attachAppVerifier = false)
        {
            _appPoolBitness = appPoolBitness;

            //
            // Default server type is IISExpress. we, however, should use IIS server instead if IIS server is ready to use.
            //
            IisServerType = ServerType.IISExpress;
            if (IISConfigUtility.IsIISReady)
            {
                IisServerType = ServerType.IIS;
            }

            TestUtility.LogInformation("TestWebSite::TestWebSite() Start");

            string solutionPath = InitializeTestMachine.GetSolutionDirectory();

            if (IisServerType == ServerType.IIS)
            {
                // check JitDebugger before continuing 
                TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);
            }

            // initialize logger for TestUtility
            _logger = new LoggerFactory()
                    .AddConsole()
                    .CreateLogger(string.Format(loggerPrefix));

            testHelper = new TestUtility(_logger);

            //
            // Initialize context variables
            //
            string siteRootPath = string.Empty;
            string siteName = string.Empty;
            string postfix = string.Empty;

            // repeat three times until getting the valid temporary directory path
            for (int i = 0; i < 3; i++)
            {
                postfix = Path.GetRandomFileName();
                siteName = loggerPrefix.Replace(" ", "") + "_" + postfix;
                siteRootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "inetpub", "ANCMTest", siteName);
                if (!Directory.Exists(siteRootPath))
                {
                    break;
                }
            }

            TestUtility.DirectoryCopy(Path.Combine(solutionPath, "test", "WebRoot"), siteRootPath);
            string aspnetCoreAppRootPath = Path.Combine(siteRootPath, "AspNetCoreApp");
            string srcPath = TestUtility.GetApplicationPath();

            // copy http.config to the test site root directory and initialize iisExpressConfigPath with the path
            if (IisServerType == ServerType.IISExpress)
            {
                IisExpressConfigPath = Path.Combine(siteRootPath, "http.config");
                TestUtility.FileCopy(Path.Combine(solutionPath, "test", "AspNetCoreModule.Test", "http.config"), IisExpressConfigPath);
            }

            //
            // Currently we use DotnetCore v1.0
            //
            string publishPath = Path.Combine(srcPath, "bin", "Debug", "netcoreapp1.0", "publish");
            string publishPathOutput = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "inetpub", "ANCMTest", "publishPathOutput");
            
            //
            // Publish aspnetcore app
            //
            if (_publishedAspnetCoreApp != true)
            {
                string argumentForDotNet = "publish " + srcPath;
                TestUtility.LogInformation("TestWebSite::TestWebSite() StandardTestApp is not published, trying to publish on the fly: dotnet.exe " + argumentForDotNet);
                TestUtility.DeleteDirectory(publishPath);
                TestUtility.RunCommand("dotnet", argumentForDotNet);
                if (!File.Exists(Path.Combine(publishPath, "AspNetCoreModule.TestSites.Standard.dll")))
                {
                    throw new Exception("Failed to publish");
                }
                TestUtility.DirectoryCopy(publishPath, publishPathOutput);
                TestUtility.FileCopy(Path.Combine(publishPathOutput, "web.config"), Path.Combine(publishPathOutput, "web.config.bak"));

                // Adjust the arguments attribute value with IISConfigUtility from a temporary site
                using (var iisConfig = new IISConfigUtility(IisServerType, IisExpressConfigPath))
                {
                    string tempSiteName = "ANCMTest_Temp";
                    int tempId = InitializeTestMachine.SiteId - 1;
                    string argumentFileName = (new TestWebApplication("/", publishPathOutput, null)).GetArgumentFileName();
                    if (string.IsNullOrEmpty(argumentFileName))
                    {
                        argumentFileName = "AspNetCoreModule.TestSites.Standard.dll";
                    }
                    iisConfig.CreateSite(tempSiteName, publishPathOutput, tempId, tempId);
                    iisConfig.SetANCMConfig(tempSiteName, "/", "arguments", Path.Combine(publishPathOutput, argumentFileName));
                    iisConfig.DeleteSite(tempSiteName);
                }
                _publishedAspnetCoreApp = true;
            }
            
            if (copyAllPublishedFiles)
            {
                // Copy all the files in the pubishpath to the standardAppRootPath
                TestUtility.DirectoryCopy(publishPath, aspnetCoreAppRootPath);
                TestUtility.FileCopy(Path.Combine(publishPathOutput, "web.config.bak"), Path.Combine(aspnetCoreAppRootPath, "web.config"));
            }
            else
            {
                // Copy only web.config file, which points to the shared publishPathOutput, to the standardAppRootPath
                TestUtility.CreateDirectory(aspnetCoreAppRootPath);
                TestUtility.FileCopy(Path.Combine(publishPathOutput, "web.config"), Path.Combine(aspnetCoreAppRootPath, "web.config"));
            }

            int tcpPort = InitializeTestMachine.SiteId++;
            _siteId = tcpPort;

            //
            // initialize class member variables
            //
            string appPoolName = null;
            if (IisServerType == ServerType.IIS)
            {
                appPoolName = siteName;
            }
            else if (IisServerType == ServerType.IISExpress)
            {
                appPoolName = "Clr4IntegratedAppPool";
            }

            // Initialize member variables
            _hostName = "localhost";
            _siteName = siteName;
            _postFix = postfix;
            _tcpPort = tcpPort;

            RootAppContext = new TestWebApplication("/", Path.Combine(siteRootPath, "WebSite1"), this);
            RootAppContext.RestoreFile("web.config");
            RootAppContext.DeleteFile("app_offline.htm");
            RootAppContext.AppPoolName = appPoolName;

            AspNetCoreApp = new TestWebApplication("/AspNetCoreApp", aspnetCoreAppRootPath, this);
            AspNetCoreApp.AppPoolName = appPoolName;
            AspNetCoreApp.RestoreFile("web.config");
            AspNetCoreApp.DeleteFile("app_offline.htm");

            WebSocketApp = new TestWebApplication("/WebSocketApp", Path.Combine(siteRootPath, "WebSocket"), this);
            WebSocketApp.AppPoolName = appPoolName;
            WebSocketApp.RestoreFile("web.config");
            WebSocketApp.DeleteFile("app_offline.htm");

            URLRewriteApp = new TestWebApplication("/URLRewriteApp", Path.Combine(siteRootPath, "URLRewrite"), this);
            URLRewriteApp.AppPoolName = appPoolName;
            URLRewriteApp.RestoreFile("web.config");
            URLRewriteApp.DeleteFile("app_offline.htm");

            //
            // Create site and apps
            //
            using (var iisConfig = new IISConfigUtility(IisServerType, IisExpressConfigPath))
            {
                // Create apppool
                if (IisServerType == ServerType.IIS)
                {
                    iisConfig.CreateAppPool(appPoolName);

                    // Switch bitness
                    if (TestUtility.IsOSAmd64 && appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
                    {
                        iisConfig.SetAppPoolSetting(appPoolName, "enable32BitAppOnWin64", true);
                    }
                }
                
                if (InitializeTestMachine.UsePrivateAspNetCoreFile == true && IisServerType == ServerType.IISExpress)
                {
                    iisConfig.AddModule("AspNetCoreModule", ("%IIS_BIN%\\" + InitializeTestMachine.PrivateFileName), null);
                }

                iisConfig.CreateSite(siteName, RootAppContext.PhysicalPath, _siteId, TcpPort, appPoolName);
                iisConfig.CreateApp(siteName, AspNetCoreApp.Name, AspNetCoreApp.PhysicalPath, appPoolName);
                iisConfig.CreateApp(siteName, WebSocketApp.Name, WebSocketApp.PhysicalPath, appPoolName);
                iisConfig.CreateApp(siteName, URLRewriteApp.Name, URLRewriteApp.PhysicalPath, appPoolName);
            }

            if (startIISExpress)
            {
                // clean up IISExpress before starting a new instance
                TestUtility.KillIISExpressProcess();

                StartIISExpress();

                // send a startup request to IISExpress instance to make sure that it is fully ready to use before starting actual test scenarios
                TestUtility.RunPowershellScript("( invoke-webrequest http://localhost:" + TcpPort + " ).StatusCode", "200");
            }
            TestUtility.LogInformation("TestWebSite::TestWebSite() End");
        }

        public void StartIISExpress()
        {
            if (IisServerType == ServerType.IIS)
            {
                return;
            }

            // reset workerProcessID
            this.WorkerProcessID = 0;

            string cmdline;
            string argument = "/siteid:" + _siteId + " /config:" + IisExpressConfigPath;

            if (Directory.Exists(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")) && _appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
            {
                cmdline = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "IIS Express", "iisexpress.exe");
            }
            else
            {
                cmdline = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "IIS Express", "iisexpress.exe");
            }
            TestUtility.LogInformation("TestWebSite::TestWebSite() Start IISExpress: " + cmdline + " " + argument);
            _iisExpressPidBackup = TestUtility.RunCommand(cmdline, argument, false, false);
        }
        
        public void AttachAppverifier()
        {
            string cmdline;
            string processName = "iisexpress.exe";
            if (IisServerType == ServerType.IIS)
            {
                processName = "w3wp.exe";
            }
            string argument = "-enable Heaps COM RPC Handles Locks Memory TLS Exceptions Threadpool Leak SRWLock -for " + processName;
            if (Directory.Exists(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")) && _appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
            {
                cmdline = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "syswow64", "appverif.exe");
                if (!File.Exists(cmdline))
                {
                    throw new System.ApplicationException("Not found :" + cmdline + "; this test requires appverif.exe.");
                }
            }
            else
            {
                cmdline = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "appverif.exe");
                if (!File.Exists(cmdline))
                {
                    throw new System.ApplicationException("Not found :" + cmdline + "; this test requires appverif.exe.");
                }
            }

            try
            {
                TestUtility.LogInformation("Configure Appverifier: " + cmdline + " " + argument);
                TestUtility.RunCommand(cmdline, argument, true, false);
            }
            catch
            {
                throw new System.ApplicationException("Failed to configure Appverifier");
            }
        }

        public void AttachWinDbg(int processIdOfWorkerProcess)
        {
            string processName = "iisexpress.exe";
            string debuggerCmdline;
            if (IisServerType == ServerType.IIS)
            {
                processName = "w3wp.exe";
            }
            string argument = "-enable Heaps COM RPC Handles Locks Memory TLS Exceptions Threadpool Leak SRWLock -for " + processName;
            if (Directory.Exists(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")) && _appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
            {
                debuggerCmdline = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Debugging Tools for Windows (x64)", "wow64", "windbg.exe");
                if (!File.Exists(debuggerCmdline))
                {
                    throw new System.ApplicationException("Not found :" + debuggerCmdline + "; this test requires windbg.exe.");
                }
            }
            else
            {
                debuggerCmdline = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Debugging Tools for Windows (x64)", "windbg.exe");
                if (!File.Exists(debuggerCmdline))
                {
                    throw new System.ApplicationException("Not found :" + debuggerCmdline + "; this test requires windbg.exe.");
                }
            }
            
            try
            {
                TestUtility.RunCommand(debuggerCmdline, " -g -G -p " + processIdOfWorkerProcess.ToString(), true, false);                
                System.Threading.Thread.Sleep(3000);
            }
            catch
            {
                throw new System.ApplicationException("Failed to attach debuger");
            }
        }

        public void DetachAppverifier()
        {
            try
            {
                string cmdline;
                string processName = "iisexpress.exe";
                string debuggerCmdline;
                if (IisServerType == ServerType.IIS)
                {
                    processName = "w3wp.exe";
                }

                string argument = "-disable * -for " + processName;
                if (Directory.Exists(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")) && _appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
                {
                    cmdline = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "syswow64", "appverif.exe");
                    if (!File.Exists(cmdline))
                    {
                        throw new System.ApplicationException("Not found :" + cmdline);
                    }
                    debuggerCmdline = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Debugging Tools for Windows (x64)", "wow64", "windbg.exe");
                    if (!File.Exists(debuggerCmdline))
                    {
                        throw new System.ApplicationException("Not found :" + debuggerCmdline);
                    }
                }
                else
                {
                    cmdline = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "appverif.exe");
                    if (!File.Exists(cmdline))
                    {
                        throw new System.ApplicationException("Not found :" + cmdline);
                    }
                    debuggerCmdline = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Debugging Tools for Windows (x64)", "windbg.exe");
                    if (!File.Exists(debuggerCmdline))
                    {
                        throw new System.ApplicationException("Not found :" + debuggerCmdline);
                    }
                }
                TestUtility.RunCommand(cmdline, argument, true, false);
            }
            catch
            {
                TestUtility.LogInformation("Failed to detach Appverifier");
            }
        }
    }
}
