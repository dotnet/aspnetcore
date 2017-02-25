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
                iisExpressProcess.Kill();
                iisExpressProcess.WaitForExit();
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
        
        public TestWebSite(IISConfigUtility.AppPoolBitness appPoolBitness, string loggerPrefix = "ANCMTest", ServerType serverType = ServerType.IIS)
        {
            TestUtility.LogInformation("TestWebSite::TestWebSite() Start");

            string solutionPath = InitializeTestMachine.GetSolutionDirectory();

            if (serverType == ServerType.IIS)
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

            //
            // Currently we use only DotnetCore v1.1 
            //
            string publishPath = Path.Combine(srcPath, "bin", "Debug", "netcoreapp1.1", "publish");

            //
            // Publish aspnetcore app
            //
            if (_publishedAspnetCoreApp != true)
            {
                string argumentForDotNet = "publish " + srcPath;
                TestUtility.LogInformation("TestWebSite::TestWebSite() StandardTestApp is not published, trying to publish on the fly: dotnet.exe " + argumentForDotNet);
                TestUtility.RunCommand("dotnet", argumentForDotNet);
                _publishedAspnetCoreApp = true;
            }

            // check published files
            bool checkPublishedFiles = false;
            string[] publishedFiles = Directory.GetFiles(publishPath);
            foreach (var item in publishedFiles)
            {
                if (Path.GetFileName(item) == "web.config")
                {
                    checkPublishedFiles = true;
                }
            }

            if (!checkPublishedFiles)
            {
                throw new System.ApplicationException("web.config is not available in " + publishPath);
            }

            // Copy the pubishpath to standardAppRootPath
            TestUtility.DirectoryCopy(publishPath, aspnetCoreAppRootPath);

            int tcpPort = InitializeTestMachine.SiteId++;
            int siteId = tcpPort;

            //
            // initialize class member variables
            //
            string appPoolName = null;
            if (serverType == ServerType.IIS)
            {
                appPoolName = siteName;
            }
            else if (serverType == ServerType.IISExpress)
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

            // copy http.config to the test site root directory and initialize iisExpressConfigPath with the path
            string iisExpressConfigPath = null;
            if (serverType == ServerType.IISExpress)
            {
                iisExpressConfigPath = Path.Combine(siteRootPath, "http.config");
                TestUtility.FileCopy(Path.Combine(solutionPath, "test", "AspNetCoreModule.Test", "http.config"), iisExpressConfigPath);
            }

            //
            // Create site and apps
            //
            using (var iisConfig = new IISConfigUtility(serverType, iisExpressConfigPath))
            {
                if (serverType == ServerType.IIS)
                {
                    iisConfig.CreateAppPool(appPoolName);
                    bool is32bit = (appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit);
                    iisConfig.SetAppPoolSetting(appPoolName, "enable32BitAppOnWin64", is32bit);
                }
                iisConfig.CreateSite(siteName, RootAppContext.PhysicalPath, siteId, this.TcpPort, appPoolName);
                iisConfig.CreateApp(siteName, AspNetCoreApp.Name, AspNetCoreApp.PhysicalPath, appPoolName);
                iisConfig.CreateApp(siteName, WebSocketApp.Name, WebSocketApp.PhysicalPath, appPoolName);
                iisConfig.CreateApp(siteName, URLRewriteApp.Name, URLRewriteApp.PhysicalPath, appPoolName);
            }
            
            if (serverType == ServerType.IISExpress)
            {
                string cmdline;
                string argument = "/siteid:" + siteId + " /config:" + iisExpressConfigPath;

                if (Directory.Exists(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")) && appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
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

            TestUtility.LogInformation("TestWebSite::TestWebSite() End");
        }
    }
}
