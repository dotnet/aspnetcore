// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.HttpClientHelper;
using Microsoft.Web.Administration;
using System;
using System.IO;
using System.Management;
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

        public IISConfigUtility(ServerType type, string iisExpressConfigPath = null)
        {
            _serverType = type;
            _iisExpressConfigPath = iisExpressConfigPath;
        }

        public static void BackupAppHostConfig()
        {
            string fromfile = Strings.AppHostConfigPath;
            string tofile = Strings.AppHostConfigPath + ".ancmtest.bak";
            if (File.Exists(fromfile))
            {
                TestUtility.FileCopy(fromfile, tofile, overWrite: false);
            }
        }

        public static void RestoreAppHostConfig()
        {
            string fromfile = Strings.AppHostConfigPath + ".ancmtest.bak";
            string tofile = Strings.AppHostConfigPath;

            if (!File.Exists(fromfile) && !File.Exists(tofile))
            {
                // IIS is not installed, don't do anything here
                return;
            }

            // backup first if the backup file is not available
            if (!File.Exists(fromfile))
            {
                BackupAppHostConfig();
            }

            // try again after the ininial clean up 
            if (File.Exists(fromfile))
            {
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

                if (File.ReadAllBytes(fromfile).Length != File.ReadAllBytes(tofile).Length)
                {
                    throw new System.ApplicationException("Failed to restore applicationhost.config");
                }
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

        public void EnableWindowsAuthentication(string siteName)
        {
            TestUtility.LogInformation("Enable Windows authentication : " + siteName);
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();

                ConfigurationSection anonymousAuthenticationSection = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", siteName);
                anonymousAuthenticationSection["enabled"] = false;
                ConfigurationSection windowsAuthenticationSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication", siteName);
                windowsAuthenticationSection["enabled"] = true;

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
                    bool result = true;
                    if (!File.Exists(Path.Combine(Strings.IIS64BitPath, "iiscore.dll")))
                    {
                        result = false;
                    }
                    if (!File.Exists(Path.Combine(Strings.IIS64BitPath, "config", "applicationhost.config")))
                    {
                        result = false;
                    }
                    _isIISInstalled = result;
                }
                return _isIISInstalled;
            }
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

        public string GetServiceStatus(string serviceName)
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

        public void AddBindingToSite(string siteName, string Ip, int Port, string host)
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

            TestUtility.LogInformation(String.Format("#################### Adding Binding {0} to Site {1} ####################", bindingInfo, siteName));

            try
            {
                using (ServerManager serverManager = GetServerManager())
                {
                    SiteCollection sites = serverManager.Sites;
                    Binding b = sites[siteName].Bindings.CreateElement();
                    b.SetAttributeValue("protocol", "http");
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