// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AspNetCoreModule.Test.HttpClientHelper;
using Microsoft.Web.Administration;
using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace AspNetCoreModule.Test.Framework
{
    public class IISConfigUtility : IDisposable
    {
        public class Strings
        {
            public static string AppHostConfigPath = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "config", "applicationHost.config");
            public static string IIS64BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv");
            public static string IIS32BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "syswow64", "inetsrv");
            public static string IISExpress64BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "IIS Express");
            public static string IISExpress32BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "IIS Express");
            public static string DefaultAppPool = "DefaultAppPool";
        }

        public static string ApppHostTemporaryBackupFileExtention = null;
        private ServerType _serverType = ServerType.IIS;
        private string _iisExpressConfigPath = null;
        
        public enum AppPoolBitness
        {
            enable32Bit,
            noChange
        }

        public void Dispose()
        {
        }

        public ServerManager GetServerManager()
        {
            if (_serverType == ServerType.IISExpress)
            {
                return new ServerManager(
                    false,                         // readOnly 
                    _iisExpressConfigPath          // applicationhost.config path for IISExpress
                );
            }
            else
            {
                return new ServerManager(
                    false,                         // readOnly 
                    Strings.AppHostConfigPath      // applicationhost.config path for IIS
                );
            }
        }

        public IISConfigUtility(ServerType type, string iisExpressConfigPath)
        {
            _serverType = type;
            _iisExpressConfigPath = iisExpressConfigPath;
        }

        public static bool BackupAppHostConfig(string fileExtenstion, bool overWriteMode)
        {
            bool result = true;
            string fromfile = Strings.AppHostConfigPath;
            string tofile = Strings.AppHostConfigPath + fileExtenstion;
            if (File.Exists(fromfile))
            {
                try
                {
                    TestUtility.FileCopy(fromfile, tofile, overWrite: overWriteMode);
                }
                catch
                {
                    result = false;
                }
            }
            return result;
        }

        public static void RestoreAppHostConfig(bool restoreFromMasterBackupFile = true)
        {
            string masterBackupFileExtension = ".ancmtest.mastebackup";
            string masterBackupFilePath = Strings.AppHostConfigPath + masterBackupFileExtension;
            string temporaryBackupFileExtenstion = null;
            string temporaryBackupFilePath = null;
            string tofile = Strings.AppHostConfigPath;

            string backupFileExentsionForDebug = ".ancmtest.debug";
            string backupFilePathForDebug = Strings.AppHostConfigPath + backupFileExentsionForDebug;
            TestUtility.DeleteFile(backupFilePathForDebug);

            // Create a master backup file
            if (restoreFromMasterBackupFile)
            {
                // Create a master backup file if it does not exist
                if (!File.Exists(masterBackupFilePath))
                {
                    if (!File.Exists(tofile))
                    {
                        throw new ApplicationException("Can't find " + tofile);
                    }
                    BackupAppHostConfig(masterBackupFileExtension, overWriteMode: false);
                }

                if (!File.Exists(masterBackupFilePath))
                {
                    throw new ApplicationException("Not found master backup file " + masterBackupFilePath);
                }
            }

            // if applicationhost.config does not exist but master backup file is available, create a new applicationhost.config from the master backup file first
            if (!File.Exists(tofile))
            {
                CopyAppHostConfig(masterBackupFilePath, tofile);
            }

            // Create a temporary backup file with the current applicationhost.config to rollback after test is completed.
            if (ApppHostTemporaryBackupFileExtention == null)
            {
                // retry 10 times until it really creates the temporary backup file
                for (int i = 0; i < 10; i++)
                {
                    temporaryBackupFileExtenstion = "." + TestUtility.RandomString(5);
                    string tempFile = Strings.AppHostConfigPath + temporaryBackupFileExtenstion;
                    if (File.Exists(tempFile))
                    {
                        // file already exists, try with a different file name
                        continue;
                    }

                    bool backupSuccess = BackupAppHostConfig(temporaryBackupFileExtenstion, overWriteMode: false);
                    if (backupSuccess && File.Exists(tempFile))
                    {
                        if (File.Exists(tempFile))
                        {
                            ApppHostTemporaryBackupFileExtention = temporaryBackupFileExtenstion;
                            break;
                        }                        
                    }
                }

                if (ApppHostTemporaryBackupFileExtention == null)
                {
                    throw new ApplicationException("Can't make a temporary backup file");
                }
            }

            if (restoreFromMasterBackupFile)
            {
                // restoring applicationhost.config from the master backup file
                CopyAppHostConfig(masterBackupFilePath, tofile);
            }
            else
            {
                // Create a temporary backup file to preserve the last state for debugging purpose before rolling back from the temporary backup file
                try
                {
                    BackupAppHostConfig(backupFileExentsionForDebug, overWriteMode: true);
                }
                catch
                {
                    TestUtility.LogInformation("Failed to create a backup file for debugging");
                }

                // restoring applicationhost.config from the temporary backup file
                temporaryBackupFilePath = Strings.AppHostConfigPath + ApppHostTemporaryBackupFileExtention;
                CopyAppHostConfig(temporaryBackupFilePath, tofile);

                // delete the temporary backup file because it is not used anymore
                try
                {
                    TestUtility.DeleteFile(temporaryBackupFilePath);
                }
                catch
                {
                    TestUtility.LogInformation("Failed to cleanup temporary backup file : " + temporaryBackupFilePath);
                }
            }
        }

        private static void CopyAppHostConfig(string fromfile, string tofile)
        {
            if (!File.Exists(fromfile) && !File.Exists(tofile))
            {
                // IIS is not installed, don't do anything here
                return;
            }

            if (!File.Exists(fromfile))
            {
                throw new System.ApplicationException("Failed to backup " + tofile);
            }

            // try restoring applicationhost.config again after the ininial clean up for better reliability
            try
            {
                TestUtility.FileCopy(fromfile, tofile, true, true);
            }
            catch
            {
                // ignore
            }

            // try again 
            if (!File.Exists(tofile) || File.ReadAllBytes(fromfile).Length != File.ReadAllBytes(tofile).Length)
            {
                // try again
                TestUtility.ResetHelper(ResetHelperMode.KillWorkerProcess);
                TestUtility.FileCopy(fromfile, tofile, true, true);
            }

            // verify restoration is done successfully
            if (File.ReadAllBytes(fromfile).Length != File.ReadAllBytes(tofile).Length)
            {
                throw new System.ApplicationException("Failed to restore applicationhost.config from " + fromfile + " to " + tofile);
            }
        }

        public void SetAppPoolSetting(string appPoolName, string attribute, object value)
        {
            TestUtility.LogInformation("Setting Apppool : " + appPoolName + "::" + attribute.ToString() + " <== " + value.ToString());
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection applicationPoolsSection = config.GetSection("system.applicationHost/applicationPools");
                ConfigurationElementCollection applicationPoolsCollection = applicationPoolsSection.GetCollection();
                ConfigurationElement addElement = FindElement(applicationPoolsCollection, "add", "name", appPoolName);
                if (addElement == null) throw new InvalidOperationException("Element not found!");

                switch (attribute)
                {
                    case "privateMemory":
                    case "memory":
                        ConfigurationElement recyclingElement = addElement.GetChildElement("recycling");
                        ConfigurationElement periodicRestartElement = recyclingElement.GetChildElement("periodicRestart");
                        periodicRestartElement[attribute] = value;
                        break;
                    case "rapidFailProtectionMaxCrashes":
                        ConfigurationElement failureElement = addElement.GetChildElement("failure");
                        failureElement["rapidFailProtectionMaxCrashes"] = value;
                        break;
                    default:
                        addElement[attribute] = value;
                        break;
                }
                serverManager.CommitChanges();                 
            }
        }

        public void RecycleAppPool(string appPoolName)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                serverManager.ApplicationPools[appPoolName].Recycle();
            }
        }

        public void StopAppPool(string appPoolName)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                serverManager.ApplicationPools[appPoolName].Stop();
            }
        }

        public void StartAppPool(string appPoolName)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                serverManager.ApplicationPools[appPoolName].Start();
            }
        }

        public void CreateSite(string siteName, string physicalPath, int siteId, int tcpPort, string appPoolName = "DefaultAppPool")
        {
            TestUtility.LogInformation("Creating web site : " + siteName);

            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection sitesSection = config.GetSection("system.applicationHost/sites");
                ConfigurationElementCollection sitesCollection = sitesSection.GetCollection();
                ConfigurationElement siteElement = FindElement(sitesCollection, "site", "name", siteName);
                if (siteElement != null)
                {
                    sitesCollection.Remove(siteElement);
                }
                siteElement = sitesCollection.CreateElement("site");
                siteElement["id"] = siteId;
                siteElement["name"] = siteName;
                ConfigurationElementCollection bindingsCollection = siteElement.GetCollection("bindings");

                ConfigurationElement bindingElement = bindingsCollection.CreateElement("binding");
                bindingElement["protocol"] = @"http";
                bindingElement["bindingInformation"] = "*:" + tcpPort + ":";
                bindingsCollection.Add(bindingElement);

                ConfigurationElementCollection siteCollection = siteElement.GetCollection();
                ConfigurationElement applicationElement = siteCollection.CreateElement("application");
                applicationElement["path"] = @"/";
                applicationElement["applicationPool"] = appPoolName;

                ConfigurationElementCollection applicationCollection = applicationElement.GetCollection();
                ConfigurationElement virtualDirectoryElement = applicationCollection.CreateElement("virtualDirectory");
                virtualDirectoryElement["path"] = @"/";
                virtualDirectoryElement["physicalPath"] = physicalPath;
                applicationCollection.Add(virtualDirectoryElement);
                siteCollection.Add(applicationElement);
                sitesCollection.Add(siteElement);
                
                serverManager.CommitChanges();
            }
        }

        public void CreateApp(string siteName, string appName, string physicalPath, string appPoolName = "DefaultAppPool")
        {
            TestUtility.LogInformation("Creating web app : " + siteName + "/" + appName);
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();

                ConfigurationSection sitesSection = config.GetSection("system.applicationHost/sites");

                ConfigurationElementCollection sitesCollection = sitesSection.GetCollection();

                ConfigurationElement siteElement = FindElement(sitesCollection, "site", "name", siteName);
                if (siteElement == null) throw new InvalidOperationException("Element not found!");

                ConfigurationElementCollection siteCollection = siteElement.GetCollection();

                ConfigurationElement applicationElement = siteCollection.CreateElement("application");
                string appPath = @"/" + appName;
                appPath = appPath.Replace("//", "/");
                applicationElement["path"] = appPath;
                applicationElement["applicationPool"] = appPoolName;

                ConfigurationElementCollection applicationCollection = applicationElement.GetCollection();

                ConfigurationElement virtualDirectoryElement = applicationCollection.CreateElement("virtualDirectory");
                virtualDirectoryElement["path"] = @"/";
                virtualDirectoryElement["physicalPath"] = physicalPath;
                applicationCollection.Add(virtualDirectoryElement);
                siteCollection.Add(applicationElement);

                serverManager.CommitChanges();
            }
        }

        public void EnableIISAuthentication(string siteName, bool windows, bool basic, bool anonymous)
        {
            TestUtility.LogInformation("Enable Windows authentication : " + siteName);
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();

                ConfigurationSection anonymousAuthenticationSection = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", siteName);
                anonymousAuthenticationSection["enabled"] = anonymous;
                ConfigurationSection basicAuthenticationSection = config.GetSection("system.webServer/security/authentication/basicAuthentication", siteName);
                basicAuthenticationSection["enabled"] = basic;
                ConfigurationSection windowsAuthenticationSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication", siteName);
                windowsAuthenticationSection["enabled"] = windows;

                serverManager.CommitChanges();
            }
        }

        public void EnableOneToOneClientCertificateMapping(string siteName, string userName, string password, string publicKey)
        {
            TestUtility.LogInformation("Enable one-to-one client certificate mapping authentication : " + siteName);
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();

                ConfigurationSection iisClientCertificateMappingAuthenticationSection = config.GetSection("system.webServer/security/authentication/iisClientCertificateMappingAuthentication", siteName);

                // enable iisClientCertificateMappingAuthentication 
                ConfigurationElementCollection oneToOneMappingsCollection = iisClientCertificateMappingAuthenticationSection.GetCollection("oneToOneMappings");
                iisClientCertificateMappingAuthenticationSection["enabled"] = true;

                // add a new oneToOne mapping collection item
                ConfigurationElement addElement = oneToOneMappingsCollection.CreateElement("add");
                addElement["userName"] = userName;
                if (password != null)
                {
                    addElement["password"] = password;
                }
                addElement["certificate"] = publicKey;
                oneToOneMappingsCollection.Add(addElement);

                // set sslFlags with SslNegotiateCert
                ConfigurationSection accessSection = config.GetSection("system.webServer/security/access", siteName);
                accessSection["sslFlags"] = "Ssl, SslNegotiateCert, SslRequireCert";

                // disable other authentication to avoid any noise affected by other authentications
                ConfigurationSection anonymousAuthenticationSection = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", siteName);
                anonymousAuthenticationSection["enabled"] = false;
                ConfigurationSection windowsAuthenticationSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication", siteName);
                windowsAuthenticationSection["enabled"] = false;

                serverManager.CommitChanges();
            }
        }

        public void SetCompression(string siteName, bool enabled)
        {
            TestUtility.LogInformation("Enable Compression : " + siteName);
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();

                ConfigurationSection urlCompressionSection = config.GetSection("system.webServer/urlCompression", siteName);
                urlCompressionSection["doStaticCompression"] = enabled;
                urlCompressionSection["doDynamicCompression"] = enabled;
                                
                serverManager.CommitChanges();
            }
        }

        public void DisableWindowsAuthentication(string siteName)
        {
            TestUtility.LogInformation("Enable Windows authentication : " + siteName);
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();

                ConfigurationSection anonymousAuthenticationSection = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", siteName);
                anonymousAuthenticationSection["enabled"] = true;
                ConfigurationSection windowsAuthenticationSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication", siteName);
                windowsAuthenticationSection["enabled"] = false;

                serverManager.CommitChanges();
            }
        }

        public void SetANCMConfig(string siteName, string appName, string attributeName, object attributeValue)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    Configuration config = serverManager.GetWebConfiguration(siteName, appName);
                    ConfigurationSection aspNetCoreSection = config.GetSection("system.webServer/aspNetCore");
                    if (attributeName == "environmentVariable")
                    {
                        string name = ((string[])attributeValue)[0];
                        string value = ((string[])attributeValue)[1];
                        ConfigurationElementCollection environmentVariablesCollection = aspNetCoreSection.GetCollection("environmentVariables");
                        ConfigurationElement environmentVariableElement = environmentVariablesCollection.CreateElement("environmentVariable");
                        environmentVariableElement["name"] = name;
                        environmentVariableElement["value"] = value;
                        var element = FindElement(environmentVariablesCollection, "add", "name", value);
                        if (element != null)
                        {
                            throw new System.ApplicationException("duplicated collection item");
                        }
                        environmentVariablesCollection.Add(environmentVariableElement);
                    }
                    else
                    {
                        aspNetCoreSection[attributeName] = attributeValue;
                    }

                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void ConfigureCustomLogging(string siteName, string appName, int statusCode, int subStatusCode, string path)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetWebConfiguration(siteName, appName);
                ConfigurationSection httpErrorsSection = config.GetSection("system.webServer/httpErrors");
                httpErrorsSection["errorMode"] = @"Custom";

                ConfigurationElementCollection httpErrorsCollection = httpErrorsSection.GetCollection();
                ConfigurationElement errorElement = FindElement(httpErrorsCollection, "error", "statusCode", statusCode.ToString(), "subStatusCode", subStatusCode.ToString());
                if (errorElement != null)
                {
                    httpErrorsCollection.Remove(errorElement);
                }

                ConfigurationElement errorElement2 = httpErrorsCollection.CreateElement("error");
                errorElement2["statusCode"] = statusCode;
                errorElement2["subStatusCode"] = subStatusCode;
                errorElement2["path"] = path;
                httpErrorsCollection.Add(errorElement2);
                
                serverManager.CommitChanges();
            }
            Thread.Sleep(500);
        }

        private static bool? _isIISInstalled = null;
        public static bool? IsIISInstalled
        {
            get
            {
                if (_isIISInstalled == null)
                {
                    _isIISInstalled = true;
                    if (_isIISInstalled == true && !File.Exists(Path.Combine(Strings.IIS64BitPath, "iiscore.dll")))
                    {
                        _isIISInstalled = false;
                    }
                    if (_isIISInstalled == true && !File.Exists(Path.Combine(Strings.IIS64BitPath, "config", "applicationhost.config")))
                    {
                        _isIISInstalled = false;
                    }
                }
                return _isIISInstalled;
            }
            set
            {
                _isIISInstalled = value;
            }
        }

        public static bool IsIISReady {
            get;
            set;
        }
        
        public bool IsAncmInstalled(ServerType servertype)
        {
            bool result = true;
            if (servertype == ServerType.IIS)
            {
                if (!File.Exists(InitializeTestMachine.IISAspnetcoreSchema_path))
                {
                    result = false;
                }
            }            
            else
            {
                if (!File.Exists(InitializeTestMachine.IISExpressAspnetcoreSchema_path))
                {
                    result = false;
                }
            }
            return result;
        }

        public static string GetServiceStatus(string serviceName)
        {
            ServiceController sc = new ServiceController(serviceName);

            switch (sc.Status)
            {
                case ServiceControllerStatus.Running:
                    return "Running";
                case ServiceControllerStatus.Stopped:
                    return "Stopped";
                case ServiceControllerStatus.Paused:
                    return "Paused";
                case ServiceControllerStatus.StopPending:
                    return "Stopping";
                case ServiceControllerStatus.StartPending:
                    return "Starting";
                default:
                    return "Status Changing";
            }
        }

        public bool IsUrlRewriteInstalledForIIS()
        {
            bool result = true;
            var toRewrite64 = Path.Combine(Strings.IIS64BitPath, "rewrite.dll");
            var toRewrite32 = Path.Combine(Strings.IIS32BitPath, "rewrite.dll");

            if (TestUtility.IsOSAmd64)
            {
                if (!File.Exists(toRewrite64))
                {
                    result = false;
                }
            }
            
            if (!File.Exists(toRewrite32))
            {
                result = false;
            }

            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection globalModulesSection = config.GetSection("system.webServer/globalModules");
                ConfigurationElementCollection globalModulesCollection = globalModulesSection.GetCollection();
                if (FindElement(globalModulesCollection, "add", "name", "RewriteModule") == null)
                {
                    result = false;
                }

                ConfigurationSection modulesSection = config.GetSection("system.webServer/modules");
                ConfigurationElementCollection modulesCollection = modulesSection.GetCollection();
                if (FindElement(modulesCollection, "add", "name", "RewriteModule") == null)
                {
                    result = false;
                }                
            }
            return result;
        }

        public bool RemoveModule(string moduleName)
        {
            bool result = true;
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection globalModulesSection = config.GetSection("system.webServer/globalModules");
                ConfigurationElementCollection globalModulesCollection = globalModulesSection.GetCollection();
                var globalModule = FindElement(globalModulesCollection, "add", "name", moduleName);
                if (globalModule != null)
                {
                    globalModulesCollection.Remove(globalModule);

                }
                ConfigurationSection modulesSection = config.GetSection("system.webServer/modules");
                ConfigurationElementCollection modulesCollection = modulesSection.GetCollection();
                var module = FindElement(modulesCollection, "add", "name", moduleName);
                if (module != null)
                {
                    modulesCollection.Remove(module);
                }
                
                serverManager.CommitChanges();
            }
            return result;
        }

        public bool AddModule(string moduleName, string image, string preCondition)
        {
            RemoveModule(moduleName);

            bool result = true;
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection globalModulesSection = config.GetSection("system.webServer/globalModules");
                ConfigurationElementCollection globalModulesCollection = globalModulesSection.GetCollection();

                ConfigurationElement globalModule = globalModulesCollection.CreateElement("add");
                globalModule["name"] = moduleName;
                globalModule["image"] = image;
                if (preCondition != null)
                {
                    globalModule["preCondition"] = preCondition;
                }
                globalModulesCollection.Add(globalModule);

                ConfigurationSection modulesSection = config.GetSection("system.webServer/modules");
                ConfigurationElementCollection modulesCollection = modulesSection.GetCollection();
                ConfigurationElement module = modulesCollection.CreateElement("add");
                module["name"] = moduleName;
                modulesCollection.Add(module);

                serverManager.CommitChanges();
            }
            return result;
        }

        private static ConfigurationElement FindElement(ConfigurationElementCollection collection, string elementTagName, params string[] keyValues)
        {
            foreach (ConfigurationElement element in collection)
            {
                if (String.Equals(element.ElementTagName, elementTagName, StringComparison.OrdinalIgnoreCase))
                {
                    bool matches = true;

                    for (int i = 0; i < keyValues.Length; i += 2)
                    {
                        object o = element.GetAttributeValue(keyValues[i]);
                        string value = null;
                        if (o != null)
                        {
                            value = o.ToString();
                        }

                        if (!String.Equals(value, keyValues[i + 1], StringComparison.OrdinalIgnoreCase))
                        {
                            matches = false;
                            break;
                        }
                    }
                    if (matches)
                    {
                        return element;
                    }
                }
            }
            return null;
        }

        public void CreateAppPool(string poolName, bool alwaysRunning = false)
        {
            try
            {
                TestUtility.LogTrace(String.Format("#################### Adding App Pool {0} with startMode = {1} ####################", poolName, alwaysRunning ? "AlwaysRunning" : "OnDemand"));
                using (ServerManager serverManager = GetServerManager())
                {
                    serverManager.ApplicationPools.Add(poolName);
                    ApplicationPool apppool = serverManager.ApplicationPools[poolName];
                    apppool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                    if (alwaysRunning)
                    {
                        apppool.SetAttributeValue("startMode", "AlwaysRunning");
                    }
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Create app pool {0} failed. Reason: {1} ####################", poolName, ex.Message));
            }
        }

        public void SetIdleTimeoutForAppPool(string appPoolName, int idleTimeoutMinutes)
        {
            TestUtility.LogTrace(String.Format("#################### Setting idleTimeout to {0} minutes for AppPool {1} ####################", idleTimeoutMinutes, appPoolName));
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    ApplicationPoolCollection appPools = serverManager.ApplicationPools;
                    appPools[appPoolName].ProcessModel.IdleTimeout = TimeSpan.FromMinutes(idleTimeoutMinutes);
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Setting idleTimeout to {0} minutes for AppPool {1} failed. Reason: {2} ####################", idleTimeoutMinutes, appPoolName, ex.Message));
            }
        }

        public void SetMaxProcessesForAppPool(string appPoolName, int maxProcesses)
        {
            TestUtility.LogTrace(String.Format("#################### Setting maxProcesses to {0} for AppPool {1} ####################", maxProcesses, appPoolName));
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    ApplicationPoolCollection appPools = serverManager.ApplicationPools;
                    appPools[appPoolName].ProcessModel.MaxProcesses = maxProcesses;
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Setting maxProcesses to {0} for AppPool {1} failed. Reason: {2} ####################", maxProcesses, appPoolName, ex.Message));
            }
        }

        public void SetIdentityForAppPool(string appPoolName, string userName, string password)
        {
            TestUtility.LogTrace(String.Format("#################### Setting userName {0} and password {1} for AppPool {2} ####################", userName, password, appPoolName));
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    ApplicationPoolCollection appPools = serverManager.ApplicationPools;
                    appPools[appPoolName].ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                    appPools[appPoolName].ProcessModel.UserName = userName;
                    appPools[appPoolName].ProcessModel.Password = password;
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Setting userName {0} and password {1} for AppPool {2} failed. Reason: {2} ####################", userName, password, appPoolName, ex.Message));
            }
        }

        public void SetStartModeAlwaysRunningForAppPool(string appPoolName, bool alwaysRunning)
        {
            string startMode = alwaysRunning ? "AlwaysRunning" : "OnDemand";

            TestUtility.LogTrace(String.Format("#################### Setting startMode to {0} for AppPool {1} ####################", startMode, appPoolName));

            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    ApplicationPoolCollection appPools = serverManager.ApplicationPools;
                    appPools[appPoolName]["startMode"] = startMode;
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Setting startMode to {0} for AppPool {1} failed. Reason: {2} ####################", startMode, appPoolName, ex.Message));
            }
        }

        public void StartAppPoolEx(string appPoolName)
        {
            StartOrStopAppPool(appPoolName, true);
        }

        public void StopAppPoolEx(string appPoolName)
        {
            StartOrStopAppPool(appPoolName, false);
        }

        private void StartOrStopAppPool(string appPoolName, bool start)
        {
            string action = start ? "Starting" : "Stopping";
            TestUtility.LogTrace(String.Format("#################### {0} app pool {1} ####################", action, appPoolName));

            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    ApplicationPoolCollection appPools = serverManager.ApplicationPools;
                    if (start)
                        appPools[appPoolName].Start();
                    else
                        appPools[appPoolName].Stop();
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                TestUtility.LogInformation(String.Format("#################### {0} app pool {1} failed. Reason: {2} ####################", action, appPoolName, ex.Message));
            }
        }

        public void VerifyAppPoolState(string appPoolName, Microsoft.Web.Administration.ObjectState state)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    ApplicationPoolCollection appPools = serverManager.ApplicationPools;
                    if (appPools[appPoolName].State == state)
                        TestUtility.LogInformation(String.Format("Verified state for app pool {0} is {1}.", appPoolName, state.ToString()));
                    else
                        TestUtility.LogInformation(String.Format("Unexpected state {0} for app pool  {1}.", state, appPoolName.ToString()));
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Failed to verify state for app pool {0}. Reason: {1} ####################", appPoolName, ex.Message));
            }
        }

        public void DeleteAppPool(string poolName)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    TestUtility.LogTrace(String.Format("#################### Deleting App Pool {0} ####################", poolName));

                    ApplicationPoolCollection appPools = serverManager.ApplicationPools;
                    appPools.Remove(appPools[poolName]);
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Delete app pool {0} failed. Reason: {1} ####################", poolName, ex.Message));
            }
        }

        public void DeleteAllAppPools(bool commitDelay = false)
        {
            TestUtility.LogTrace(String.Format("#################### Deleting all app pools ####################"));

            using (ServerManager serverManager = GetServerManager())
            {
                ApplicationPoolCollection appPools = serverManager.ApplicationPools;
                while (appPools.Count > 0)
                    appPools.RemoveAt(0);
                
                serverManager.CommitChanges();
            }
        }

        public void CreateSiteEx(int siteId, string siteName, string poolName, string dirRoot, string Ip, int Port, string host)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    string bindingInfo = "";
                    if (Ip == null)
                        Ip = "*";
                    bindingInfo += Ip;
                    bindingInfo += ":";
                    bindingInfo += Port;
                    bindingInfo += ":";
                    if (host != null)
                        bindingInfo += host;

                    TestUtility.LogTrace(String.Format("#################### Adding Site {0} with App Pool {1} with BindingInfo {2} ####################", siteName, poolName, bindingInfo));

                    SiteCollection sites = serverManager.Sites;
                    Site site = sites.CreateElement();
                    site.Id = siteId;
                    site.SetAttributeValue("name", siteName);
                    sites.Add(site);

                    Application app = site.Applications.CreateElement();
                    app.SetAttributeValue("path", "/");
                    app.SetAttributeValue("applicationPool", poolName);
                    site.Applications.Add(app);

                    VirtualDirectory vdir = app.VirtualDirectories.CreateElement();
                    vdir.SetAttributeValue("path", "/");
                    vdir.SetAttributeValue("physicalPath", dirRoot);

                    app.VirtualDirectories.Add(vdir);

                    Binding b = site.Bindings.CreateElement();
                    b.SetAttributeValue("protocol", "http");
                    b.SetAttributeValue("bindingInformation", bindingInfo);

                    site.Bindings.Add(b);

                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Create site {0} failed. Reason: {1} ####################", siteName, ex.Message));
            }
        }

        public void StartSite(string siteName)
        {
            StartOrStopSite(siteName, true);
        }

        public void StopSite(string siteName)
        {
            StartOrStopSite(siteName, false);
        }

        private void StartOrStopSite(string siteName, bool start)
        {
            string action = start ? "Starting" : "Stopping";
            TestUtility.LogTrace(String.Format("#################### {0} site {1} ####################", action, siteName));

            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    SiteCollection sites = serverManager.Sites;
                    if (start)
                    {
                        sites[siteName].Start();
                        sites[siteName].SetAttributeValue("serverAutoStart", true);
                    }
                    else
                    {
                        sites[siteName].Stop();
                        sites[siteName].SetAttributeValue("serverAutoStart", false);
                    }
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### {0} site {1} failed. Reason: {2} ####################", action, siteName, ex.Message));
            }
        }

        public ObjectState GetSiteState(string siteName)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                SiteCollection sites = serverManager.Sites;
                if (sites[siteName] != null)
                {
                    return sites[siteName].State;
                }
                else
                {
                    return ObjectState.Unknown;
                }
            }
        }

        public void AddApplicationToSite(string siteName, string appPath, string physicalPath, string poolName)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    TestUtility.LogTrace(String.Format("#################### Adding Application {0} with App Pool {1} to Site {2} ####################", appPath, poolName, siteName));

                    SiteCollection sites = serverManager.Sites;
                    Application app = sites[siteName].Applications.CreateElement();
                    app.SetAttributeValue("path", appPath);
                    app.SetAttributeValue("applicationPool", poolName);
                    sites[siteName].Applications.Add(app);

                    VirtualDirectory vdir = app.VirtualDirectories.CreateElement();
                    vdir.SetAttributeValue("path", "/");
                    vdir.SetAttributeValue("physicalPath", physicalPath);

                    app.VirtualDirectories.Add(vdir);
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Add Application {0} with App Pool {1} to Site {2} failed. Reason: {3} ####################", appPath, poolName, siteName, ex.Message));
            }
        }

        public void ChangeApplicationPool(string siteName, int appIndex, string poolName)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    TestUtility.LogTrace(String.Format("#################### Changing Application Pool for App {0} of Site {1} to {2} ####################", appIndex, siteName, poolName));

                    serverManager.Sites[siteName].Applications[appIndex].SetAttributeValue("applicationPool", poolName);
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Changing Application Pool for App {0} of Site {1} to {2} failed. Reason: {3} ####################", appIndex, siteName, poolName, ex.Message));
            }
        }

        public void ChangeApplicationPath(string siteName, int appIndex, string path)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    TestUtility.LogTrace(String.Format("#################### Changing Path for App {0} of Site {1} to {2} ####################", appIndex, siteName, path));

                    serverManager.Sites[siteName].Applications[appIndex].SetAttributeValue("path", path);

                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Changing Path for App {0} of Site {1} to {2} failed. Reason: {3} ####################", appIndex, siteName, path, ex.Message));
            }
        }

        public void RemoveApplication(string siteName, int appIndex)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    TestUtility.LogTrace(String.Format("#################### Deleting App {0} from Site {1} ####################", appIndex, siteName));

                    serverManager.Sites[siteName].Applications.RemoveAt(appIndex);

                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Deleting App {0} from Site {1} failed. Reason: {2} ####################", appIndex, siteName, ex.Message));
            }
        }

        public string CreateSelfSignedCertificateWithMakeCert(string subjectName, string issuerName = null, string extendedKeyUsage = null)
        {
            string makecertExeFilePath = TestUtility.GetMakeCertPath();

            string parameter;
            string targetSSLStore = string.Empty;
            if (issuerName == null)
            {
                // if issuer Name is null, you are going to create a root level certificate
                parameter = "-r -pe -n \"CN = " + subjectName + "\" -b 12/22/2013 -e 12/23/2020 -ss root -sr localmachine -len 2048 -a sha256";
                targetSSLStore = @"Cert:\LocalMachine\Root"; // => -ss root -sr localmachine
            }
            else
            {
                // if issuer Name is *not* null, you are going to create a child evel certificate from the given issuer certificate
                switch (extendedKeyUsage)
                {
                    // for web server certificate
                    case "1.3.6.1.5.5.7.3.1":
                        parameter = "-pe -n \"CN=" + subjectName + "\" -b 12/22/2013 -e 12/23/" + (System.DateTime.Now.Year + 10).ToString() + "  -eku " + extendedKeyUsage + " -is root -ir localmachine -in \"" + issuerName + "\" -len 2048 -ss my -sr localmachine -a sha256";
                        targetSSLStore = @"Cert:\LocalMachine\My";  // => -ss my -sr localmachine
                        break;

                    // for client authentication
                    case "1.3.6.1.5.5.7.3.2":
                        parameter = "-pe -n \"CN=" + subjectName + "\" -eku " + extendedKeyUsage + " -is root -ir localmachine -in \"" + issuerName + "\" -ss my -sr currentuser -len 2048 -a sha256";
                        targetSSLStore = @"Cert:\CurrentUser\My"; // => -ss my -sr currentuser
                        break;

                    default:
                        throw new NotImplementedException(extendedKeyUsage);
                }
            }
            try
            {
                TestUtility.RunCommand(makecertExeFilePath, parameter);
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation("Failed to run makecert.exe. Makecert.exe is installed with Visual Studio or SDK. Please make sure setting PATH environment to include the directory path of the makecert.exe file");
                throw ex;
            }

            string toolsPath = Path.Combine(InitializeTestMachine.GetSolutionDirectory(), "tools");
            string powershellScript = Path.Combine(toolsPath, "certificate.ps1")
                + " -Command Get-CertificateThumbPrint" + 
                " -Subject " + subjectName +                
                " -TargetSSLStore \"" + targetSSLStore + "\"";

            if (issuerName != null)
            {
                powershellScript += " -IssuerName " + issuerName;
            }

            string output = TestUtility.RunPowershellScript(powershellScript);
            if (output.Length != 40)
            {
                throw new System.ApplicationException("Failed to create a certificate, output: " + output);
            }
            return output;
        }

        public string CreateSelfSignedCertificate(string subjectName)
        {
            string toolsPath = Path.Combine(InitializeTestMachine.GetSolutionDirectory(), "tools");
            string powershellScript = Path.Combine(toolsPath, "certificate.ps1") 
                + " -Command Create-SelfSignedCertificate" 
                + " -Subject " + subjectName;

            string output = TestUtility.RunPowershellScript(powershellScript);
            if (output.Length != 40)
            {
                throw new System.ApplicationException("Failed to create a certificate, output: " + output);
            }
            return output;
        }
                
        public string ExportCertificateTo(string thumbPrint, string sslStoreFrom = @"Cert:\LocalMachine\My", string sslStoreTo = @"Cert:\LocalMachine\Root", string pfxPassword = null)
        {
            string toolsPath = Path.Combine(InitializeTestMachine.GetSolutionDirectory(), "tools");
            string powershellScript = Path.Combine(toolsPath, "certificate.ps1") + 
                " -Command Export-CertificateTo" + 
                " -TargetThumbPrint " + thumbPrint + 
                " -TargetSSLStore " + sslStoreFrom +
                " -ExportToSSLStore " + sslStoreTo;

            if (pfxPassword != null)
            {
                powershellScript += " -PfxPassword " + pfxPassword;
            }

            string output = TestUtility.RunPowershellScript(powershellScript);
            if (output != string.Empty)
            {
                throw new System.ApplicationException("Failed to export a certificate to RootCA, output: " + output);
            }
            return output;
        }

        public string GetCertificatePublicKey(string thumbPrint, string sslStore = @"Cert:\LocalMachine\My")
        {
            string toolsPath = Path.Combine(InitializeTestMachine.GetSolutionDirectory(), "tools");
            string powershellScript = Path.Combine(toolsPath, "certificate.ps1") +
                " -Command Get-CertificatePublicKey" +
                " -TargetThumbPrint " + thumbPrint +
                " -TargetSSLStore " + sslStore;

            string output = TestUtility.RunPowershellScript(powershellScript);
            if (output.Length < 500)
            {
                throw new System.ApplicationException("Failed to get certificate public key, output: " + output);
            }
            return output;
        }

        public string DeleteCertificate(string thumbPrint, string sslStore= @"Cert:\LocalMachine\My")
        {
            string toolsPath = Path.Combine(InitializeTestMachine.GetSolutionDirectory(), "tools");
            string powershellScript = Path.Combine(toolsPath, "certificate.ps1") + 
                " -Command Delete-Certificate" + 
                " -TargetThumbPrint " + thumbPrint + 
                " -TargetSSLStore " + sslStore;

            string output = TestUtility.RunPowershellScript(powershellScript);
            if (output != string.Empty)
            {
                throw new System.ApplicationException("Failed to delete a certificate (thumbprint: " + thumbPrint + ", output: " + output);
            }
            return output;
        }

        public void SetSSLCertificate(int port, string hexIpAddress, string thumbPrint, string sslStore = @"Cert:\LocalMachine\My")
        {
            // Remove a certificate mapping if it exists
            RemoveSSLCertificate(port, hexIpAddress);

            // Configure certificate mapping with the newly created certificate
            string toolsPath = Path.Combine(InitializeTestMachine.GetSolutionDirectory(), "tools");
            string powershellScript = Path.Combine(toolsPath, "httpsys.ps1") + 
                " -Command Add-SslBinding" + 
                " -IpAddress " + hexIpAddress + 
                " -Port " + port.ToString() + 
                " –Thumbprint \"" + thumbPrint + "\"" + 
                " -TargetSSLStore " + sslStore;

            string output = TestUtility.RunPowershellScript(powershellScript);
            if (output != string.Empty)
            {
                throw new System.ApplicationException("Failed to configure certificate, output: " + output);
            }
        }

        public void RemoveSSLCertificate(int port, string hexIpAddress, string sslStore = @"Cert:\LocalMachine\My")
        {
            string toolsPath = Path.Combine(InitializeTestMachine.GetSolutionDirectory(), "tools");
            string powershellScript = Path.Combine(toolsPath, "httpsys.ps1") + 
                " -Command Get-SslBinding" + 
                " -IpAddress " + hexIpAddress + 
                " -Port " + port.ToString();

            string output = TestUtility.RunPowershellScript(powershellScript);
            if (output != string.Empty)
            {
                // Delete a certificate mapping if it exists
                powershellScript = Path.Combine(toolsPath, "httpsys.ps1") + " -Command Delete-SslBinding -IpAddress " + hexIpAddress + " -Port " + port.ToString();
                output = TestUtility.RunPowershellScript(powershellScript);
                if (output != string.Empty)
                {
                    throw new System.ApplicationException("Failed to delete certificate, output: " + output);
                }
            }
        }

        public void AddBindingToSite(string siteName, string ipAddress, int port, string host, string protocol = "http")
        {
            string bindingInfo = "";
            if (ipAddress == null)
                ipAddress = "*";
            bindingInfo += ipAddress;
            bindingInfo += ":";
            bindingInfo += port;
            bindingInfo += ":";
            if (host != null)
                bindingInfo += host;

            TestUtility.LogInformation(String.Format("#################### Adding Binding {0} to Site {1} ####################", bindingInfo, siteName));

            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    SiteCollection sites = serverManager.Sites;
                    Binding b = sites[siteName].Bindings.CreateElement();
                    b.SetAttributeValue("protocol", protocol);
                    b.SetAttributeValue("bindingInformation", bindingInfo);

                    sites[siteName].Bindings.Add(b);

                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Adding Binding {0} to Site {1} failed. Reason: {2} ####################", bindingInfo, siteName, ex.Message));
            }
        }

        public void RemoveBindingFromSite(string siteName, BindingInfo bindingInfo)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    TestUtility.LogTrace(String.Format("#################### Removing Binding {0} from Site {1} ####################", bindingInfo.ToBindingString(), siteName));

                    for (int i = 0; i < serverManager.Sites[siteName].Bindings.Count; i++)
                    {
                        if (serverManager.Sites[siteName].Bindings[i].BindingInformation.ToString() == bindingInfo.ToBindingString())
                        {
                            serverManager.Sites[siteName].Bindings.RemoveAt(i);
                            
                            serverManager.CommitChanges();
                            return;
                        }
                    }

                    TestUtility.LogInformation(String.Format("#################### Remove binding failed because binding was not found ####################"));
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Remove binding failed. Reason: {0} ####################", ex.Message));
            }
        }

        public void ModifyBindingForSite(string siteName, BindingInfo bindingInfoOld, BindingInfo bindingInfoNew)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    TestUtility.LogTrace(String.Format("#################### Changing Binding {0} for Site {1} to {2} ####################", bindingInfoOld.ToBindingString(), siteName, bindingInfoNew.ToBindingString()));

                    for (int i = 0; i < serverManager.Sites[siteName].Bindings.Count; i++)
                    {
                        if (serverManager.Sites[siteName].Bindings[i].BindingInformation.ToString() == bindingInfoOld.ToBindingString())
                        {
                            serverManager.Sites[siteName].Bindings[i].SetAttributeValue("bindingInformation", bindingInfoNew.ToBindingString());
                            
                            serverManager.CommitChanges();
                            return;
                        }
                    }

                    TestUtility.LogInformation(String.Format("#################### Modify binding failed because binding was not found ####################"));
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Changing binding failed. Reason: {0} ####################", ex.Message));
            }
        }

        public void DeleteSite(string siteName)
        {
            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    TestUtility.LogTrace(String.Format("#################### Deleting Site {0} ####################", siteName));

                    SiteCollection sites = serverManager.Sites;
                    sites.Remove(sites[siteName]);
                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogInformation(String.Format("#################### Delete site {0} failed. Reason: {1} ####################", siteName, ex.Message));
            }
        }

        public void DeleteAllSites(bool commitDelay = false)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                TestUtility.LogTrace(String.Format("#################### Deleting all sites ####################"));

                SiteCollection sites = serverManager.Sites;
                while (sites.Count > 0)
                    sites.RemoveAt(0);
                
                serverManager.CommitChanges();
            }
        }

        public void SetDynamicSiteRegistrationThreshold(int threshold)
        {
            try
            {
                TestUtility.LogTrace(String.Format("#################### Changing dynamicRegistrationThreshold to {0} ####################", threshold));

                using (ServerManager serverManager = new ServerManager())
                {
                    Configuration config = serverManager.GetApplicationHostConfiguration();

                    ConfigurationSection webLimitsSection = config.GetSection("system.applicationHost/webLimits");
                    webLimitsSection["dynamicRegistrationThreshold"] = threshold;

                    
                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogTrace(String.Format("#################### Changing dynamicRegistrationThreshold failed. Reason: {0} ####################", ex.Message));
            }
        }        
    }
}