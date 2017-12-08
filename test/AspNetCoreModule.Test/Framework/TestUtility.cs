// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Management;
using System.Threading;
using System.Diagnostics;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace AspNetCoreModule.Test.Framework
{
    public enum ResetHelperMode
    {
        CallIISReset,
        StopHttpStartW3svc,
        StopWasStartW3svc,
        StopW3svcStartW3svc,
        KillWorkerProcess,
        KillVSJitDebugger,
        KillIISExpress
    }

    public enum ServerType
    {
        IISExpress = 0,
        IIS = 1,
    }
    
    public class TestUtility
    {
        public static ILogger _logger = null;

        public static ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger("TestUtility");
                }
                return _logger;
            }
        }

        public TestUtility(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Retries every 1 sec for 60 times by default.
        /// </summary>
        /// <param name="retryBlock"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="retryCount"></param>
        public static async Task<HttpResponseMessage> RetryRequest(
            Func<Task<HttpResponseMessage>> retryBlock,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken),
            int retryCount = 60)
        {
            for (var retry = 0; retry < retryCount; retry++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Failed to connect, retry canceled.");
                    throw new OperationCanceledException("Failed to connect, retry canceled.", cancellationToken);
                }

                try
                {
                    logger.LogWarning("Retry count {retryCount}..", retry + 1);
                    var response = await retryBlock().ConfigureAwait(false);

                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        // Automatically retry on 503. May be application is still booting.
                        logger.LogWarning("Retrying a service unavailable error.");
                        continue;
                    }

                    return response; // Went through successfully
                }
                catch (Exception exception)
                {
                    if (retry == retryCount - 1)
                    {
                        logger.LogError(0, exception, "Failed to connect, retry limit exceeded.");
                        throw;
                    }
                    else
                    {
                        if (exception is HttpRequestException
#if NET451
                        || exception is System.Net.WebException
#endif
                        )
                        {
                            logger.LogWarning("Failed to complete the request : {0}.", exception.Message);
                            await Task.Delay(1 * 1000); //Wait for a while before retry.
                        }
                    }
                }
            }

            logger.LogInformation("Failed to connect, retry limit exceeded.");
            throw new OperationCanceledException("Failed to connect, retry limit exceeded.");
        }

        public static void RetryOperation(
            Action retryBlock,
            Action<Exception> exceptionBlock,
            int retryCount = 3,
            int retryDelayMilliseconds = 0)
        {
            for (var retry = 0; retry < retryCount; ++retry)
            {
                try
                {
                    retryBlock();
                    break;
                }
                catch (Exception exception)
                {
                    exceptionBlock(exception);
                }

                Thread.Sleep(retryDelayMilliseconds);
            }
        }

        public static bool RetryHelper<T> (
                   Func<T, bool> verifier,
                   T arg,
                   Action<Exception> exceptionBlock = null,
                   int retryCount = 3,
                   int retryDelayMilliseconds = 1000
                   )
        {
            for (var retry = 0; retry < retryCount; ++retry)
            {
                try
                {
                    if (verifier(arg))
                        return true;
                }
                catch (Exception exception)
                {
                    exceptionBlock?.Invoke(exception);
                }
                Thread.Sleep(retryDelayMilliseconds);
            }
            return false;
        }

        public static bool RetryHelper<T1, T2>(
                   Func<T1, T2, bool> verifier,
                   T1 arg1,
                   T2 arg2,
                   Action<Exception> exceptionBlock = null,
                   int retryCount = 3,
                   int retryDelayMilliseconds = 1000
                   )
        {
            for (var retry = 0; retry < retryCount; ++retry)
            {
                try
                {
                    if (verifier(arg1, arg2))
                        return true;
                }
                catch (Exception exception)
                {
                    exceptionBlock?.Invoke(exception);
                }
                Thread.Sleep(retryDelayMilliseconds);
            }
            return false;
        }

        public static bool RetryHelper<T1, T2, T3>(
                   Func<T1, T2, T3, bool> verifier,
                   T1 arg1,
                   T2 arg2,
                   T3 arg3,
                   Action<Exception> exceptionBlock = null,
                   int retryCount = 3,
                   int retryDelayMilliseconds = 1000
                   )
        {
            for (var retry = 0; retry < retryCount; ++retry)
            {
                try
                {
                    if (verifier(arg1, arg2, arg3))
                        return true;
                }
                catch (Exception exception)
                {
                    exceptionBlock?.Invoke(exception);
                }
                LogInformation("ANCMTEST::RetryHelper Retrying " + retry);
                Thread.Sleep(retryDelayMilliseconds);
            }
            return false;
        }
        
        public static void GiveWritePermissionTo(string folder, SecurityIdentifier sid)
        {
            DirectorySecurity fsecurity = Directory.GetAccessControl(folder);
            FileSystemAccessRule writerule = new FileSystemAccessRule(sid, FileSystemRights.Write, AccessControlType.Allow);            
            fsecurity.AddAccessRule(writerule);
            Directory.SetAccessControl(folder, fsecurity);
            Thread.Sleep(500);
        }
        
        public static bool IsOSAmd64
        {
            get
            {
                if (Environment.ExpandEnvironmentVariables("%PROCESSOR_ARCHITECTURE%") == "AMD64")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static void LogTrace(string format, params object[] parameters)
        {
            if (format != null)
            {
                Logger.LogTrace(format, parameters);
            }
        }
        public static void LogError(string format, params object[] parameters)
        {
            if (format != null)
            {
                Logger.LogError(format, parameters);
            }
        }
        public static void LogInformation(string format, params object[] parameters)
        {
            if (format != null)
            {
                Logger.LogInformation(format, parameters);
            }
        }

        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                RunCommand("cmd.exe", "/c del \"" + filePath + "\"");                
            }
            if (File.Exists(filePath))
            {
                throw new ApplicationException("Failed to delete file: " + filePath);
            }
        }

        public static void FileMove(string from, string to, bool overWrite = true)
        {
            if (overWrite)
            {
                DeleteFile(to);
            }
            if (File.Exists(from))
            {
                if (File.Exists(to) && !overWrite)
                {
                    return;
                }
                File.Move(from, to);
                if (!File.Exists(to))
                {
                    throw new ApplicationException("Failed to rename from : " + from + ", to : " + to);
                }
                if (File.Exists(from))
                {
                    throw new ApplicationException("Failed to rename from : " + from + ", to : " + to);
                }
            }
            else
            {
                throw new ApplicationException("File not found " + from);
            }
        }

        public static void FileCopy(string from, string to, bool overWrite = true, bool ignoreExceptionWhileDeletingExistingFile = false)
        {
            if (overWrite)
            {
                try
                {
                    DeleteFile(to);
                }
                catch
                {
                    if (!ignoreExceptionWhileDeletingExistingFile)
                    {
                        throw;
                    }
                }
            }

            if (File.Exists(from))
            {
                if (File.Exists(to) && !overWrite)
                {
                    return;                
                }
                RunCommand("cmd.exe", "/c copy /y \"" + from + "\" \"" + to + "\"");

                if (!File.Exists(to))
                {
                    throw new ApplicationException("Failed to move from : " + from + ", to : " + to);
                }
            }
            else
            {
                LogError("File not found " + from);
            }
        }

        public static void DeleteDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                RunCommand("cmd.exe", "/c rd \"" + directoryPath + "\" /s /q");                
            }
            if (Directory.Exists(directoryPath))
            {
                throw new ApplicationException("Failed to delete directory: " + directoryPath);
            }
        }

        public static void CreateDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                RunCommand("cmd.exe", "/c md \"" + directoryPath + "\"");                
            }
            if (!Directory.Exists(directoryPath))
            {
                throw new ApplicationException("Failed to create directory: " + directoryPath);
            }            
        }

        public static void DirectoryCopy(string from, string to)
        {
            if (Directory.Exists(to))
            {
                DeleteDirectory(to);
            }

            if (!Directory.Exists(to))
            {
                CreateDirectory(to);
            }

            if (Directory.Exists(from))
            {
                RunCommand("cmd.exe", "/c xcopy \"" + from + "\" \"" + to + "\" /s");                
            }
            else
            {
                TestUtility.LogInformation("Directory not found " + from);
            }
        }

        public static string FileReadAllText(string file)
        {
            string result = null;
            if (File.Exists(file))
            {
                result = File.ReadAllText(file);
            }
            return result;
        }

        public static void CreateFile(string file, string[] stringArray)
        {
            DeleteFile(file);
            using (StreamWriter sw = new StreamWriter(file))
            {
                foreach (string line in stringArray)
                {
                    sw.WriteLine(line);
                }
            }

            if (!File.Exists(file))
            {
                throw new ApplicationException("Failed to create " + file);
            }
        }

        public static void KillProcess(string processFileName)
        {
            string query = "Select * from Win32_Process Where Name = \"" + processFileName + "\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();
            foreach (ManagementObject obj in processList)
            {
                obj.InvokeMethod("Terminate", null);
            }
            Thread.Sleep(1000);

            processList = searcher.Get();
            if (processList.Count > 0)
            {
                TestUtility.LogInformation("Failed to kill process " + processFileName);
            }            
        }

        public static string GetMakeCertPath()
        {
            string makecertExeFilePath = "makecert.exe";
            var makecertExeFilePaths = new Dictionary<string, string>();
            makecertExeFilePaths.Add("default", Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Windows Kits"));
            if (IsOSAmd64)
            {
                makecertExeFilePaths.Add("wow64mode", Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "Windows Kits"));
            }

            foreach (var item in makecertExeFilePaths)
            {
                string[] files = null;
                if (!Directory.Exists(item.Value))
                {
                    continue;
                }
                files = Directory.GetFiles(item.Value, "makecert.exe", SearchOption.AllDirectories);
                
                foreach (string makecert in files)
                {
                    if (makecert.Contains("arm"))
                    {
                        // arm process version is skipped here
                        continue;
                    }                    
                    makecertExeFilePath = makecert;
                    try
                    {
                        TestUtility.RunCommand(makecertExeFilePath, null, true, true);
                    }
                    catch
                    {
                        continue;
                    }
                    break;
                }
            }
            return makecertExeFilePath;
        }

        public static int GetNumberOfProcess(string processFileName, int expectedNumber = 1, int retry = 0)
        {
            int result = 0;
            string query = "Select * from Win32_Process Where Name = \"" + processFileName + "\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();
            result = processList.Count;
            for (int i = 0; i < retry; i++)
            {
                if (result == expectedNumber)
                {
                    break;
                }
                Thread.Sleep(1000);
                processList = searcher.Get();
                result = processList.Count;
            }
            return result;
        }

        public static object GetProcessWMIAttributeValue(string processFileName, string attributeName, string owner = null)
        {
            string query = "Select * from Win32_Process Where Name = \"" + processFileName + "\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();
            object result = null;
            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                bool found = true;

                if (owner != null)
                {
                    found = false;
                    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                    if (returnVal == 0)
                    {
                        if (argList[0].ToUpper() == owner.ToUpper())
                        {
                            found = true;
                        }
                    }
                }
                if (found)
                {
                    result = obj.GetPropertyValue(attributeName);
                    break;
                }
            }
            return result;
        }

        public static string GetHttpUri(string Url, TestWebSite siteContext)
        {
            string tempUrl = Url.TrimStart(new char[] { '/' });
            return "http://" + siteContext.HostName + ":" + siteContext.TcpPort + "/" + tempUrl;
        }

        public static string XmlParser(string xmlFileContent, string elementName, string attributeName, string childkeyValue)
        {
            string result = string.Empty;

            XmlDocument serviceStateXml = new XmlDocument();
            serviceStateXml.LoadXml(xmlFileContent);

            XmlNodeList elements = serviceStateXml.GetElementsByTagName(elementName);
            foreach (XmlNode item in elements)
            {
                if (childkeyValue == null)
                {
                    if (item.Attributes[attributeName].Value != null)
                    {
                        string newValueFound = item.Attributes[attributeName].Value;
                        if (result != string.Empty)
                        {
                            newValueFound += "," + newValueFound;   // make the result value in comma seperated format if there are multiple nodes
                        }
                        result += newValueFound;
                    }
                }
                else
                {
                    //int groupIndex = 0;
                    foreach (XmlNode groupNode in item.ChildNodes)
                    {
                        /*UrlGroup urlGroup = new UrlGroup();
                        urlGroup._requestQueue = requestQueue._requestQueueName;
                        urlGroup._urlGroupId = groupIndex.ToString();

                        foreach (XmlNode urlNode in groupNode)
                            urlGroup._urls.Add(urlNode.InnerText.ToUpper());

                        requestQueue._urlGroupIds.Add(groupIndex);
                        requestQueue._urlGroups.Add(urlGroup);
                        groupIndex++; */
                    }
                }
            }
            return result;
        }

        public static string RandomString(long size)
        {
            var random = new Random((int)DateTime.Now.Ticks);

            var builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }
       
        public static bool ResetHelper(ResetHelperMode mode)
        {
            bool result = false;
            switch (mode)
            {
                case ResetHelperMode.CallIISReset:
                    result = CallIISReset();
                    break;
                case ResetHelperMode.StopHttpStartW3svc:
                    StopHttp();
                    result = StartW3svc();
                    break;
                case ResetHelperMode.StopWasStartW3svc:
                    StopWas();
                    result = StartW3svc();
                    break;
                case ResetHelperMode.StopW3svcStartW3svc:
                    StopW3svc();
                    result = StartW3svc();
                    break;
                case ResetHelperMode.KillWorkerProcess:
                    result = KillWorkerProcess();
                    break;
                case ResetHelperMode.KillVSJitDebugger:
                    result = KillVSJitDebugger();
                    break;
                case ResetHelperMode.KillIISExpress:
                    result = KillIISExpress();
                    break;
            };
            return result;
        }

        public static bool KillIISExpress()
        {
            bool result = false;
            string query = "Select * from Win32_Process Where Name = \"iisexpress.exe\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                bool foundProcess = true;
                if (foundProcess)
                {
                    obj.InvokeMethod("Terminate", null);
                    result = true;
                }
            }
            return result;
        }

        public static bool KillVSJitDebugger()
        {
            bool result = false;
            string query = "Select * from Win32_Process Where Name = \"vsjitdebugger.exe\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                bool foundProcess = true;
                if (foundProcess)
                {
                    LogError("Jit Debugger found");
                    obj.InvokeMethod("Terminate", null);
                    result = true;
                }
            }
            return result;
        }

        public static bool KillWorkerProcess(string owner = null)
        {
            bool result = false;

            string query = "Select * from Win32_Process Where Name = \"w3wp.exe\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                if (owner != null)
                {
                    string[] argList = new string[] { string.Empty, string.Empty };
                    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                    if (returnVal == 0)
                    {
                        bool foundProcess = true;

                        if (String.Compare(argList[0], owner, true) != 0)
                        {
                            foundProcess = false;
                        }
                        if (foundProcess)
                        {
                            obj.InvokeMethod("Terminate", null);
                            result = true;
                        }
                    }
                }
                else
                {
                    obj.InvokeMethod("Terminate", null);
                    result = true;
                }
            }
            return result;
        }

        public static bool KillIISExpressProcess(string owner = null)
        {
            bool result = false;

            string query = "Select * from Win32_Process Where Name = \"iisexpress.exe\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                if (owner != null)
                {
                    string[] argList = new string[] { string.Empty, string.Empty };
                    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                    if (returnVal == 0)
                    {
                        bool foundProcess = true;

                        if (String.Compare(argList[0], owner, true) != 0)
                        {
                            foundProcess = false;
                        }
                        if (foundProcess)
                        {
                            obj.InvokeMethod("Terminate", null);
                            result = true;
                        }
                    }
                }
                else
                {
                    obj.InvokeMethod("Terminate", null);
                    result = true;
                }
            }
            return result;
        }

        public static string RunPowershellScript(string scriptText)
        {
            IPEndPoint a = new IPEndPoint(0, 443);

            // create Powershell runspace
            Runspace runspace = null;
            try
            {
                runspace = RunspaceFactory.CreateRunspace();
            }
            catch
            {
                LogInformation("Failed to instantiate powershell Runspace; if this is Win7, install Powershell 4.0 to fix this problem");
                throw new ApplicationException("Failed to instantiate powershell Runspace");
            }
            
            // open it
            runspace.Open();

            // create a pipeline and feed it the script text
            Pipeline pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(scriptText);

            // add an extra command to transform the script output objects into nicely formatted strings
            // remove this line to get the actual objects that the script returns. For example, the script
            // "Get-Process" returns a collection of System.Diagnostics.Process instances.
            pipeline.Commands.Add("Out-String");
            Collection<PSObject> results = null;
            try
            {
                // execute the script
                results = pipeline.Invoke();
            }
            catch (System.Exception ex)
            {
                throw new Exception("Failed to run " + scriptText + " " + ex.ToString());
            }

            // close the runspace
            runspace.Close();

            // convert the script result into a single string
            StringBuilder stringBuilder = new StringBuilder();
            foreach (PSObject obj in results)
            {
                stringBuilder.AppendLine(obj.ToString());
            }

            return stringBuilder.ToString().Trim(new char[] { ' ', '\r', '\n' });
        }

        public static void RunPowershellScript(string scriptText, string expectedResult, int retryCount = 3)
        {
            bool isReady = false;
            string result = string.Empty;

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    result = TestUtility.RunPowershellScript(scriptText);
                }
                catch
                {
                    result = "ExceptionError";
                }

                if (expectedResult != null)
                {
                    if (expectedResult == result)
                    {
                        isReady = true;
                        break;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                else
                {
                    isReady = true;
                    break;
                }
            }
            if (!isReady)
            {
                throw new ApplicationException("Failed to execute command: " + scriptText + ", expected result: " + expectedResult + ", actual result = " + result);
            }
        }

        public static int RunCommand(string fileName, string arguments = null, bool checkStandardError = true, bool waitForExit=true)
        {
            int pid = -1;
            Process p = new Process();
            p.StartInfo.FileName = fileName;
            if (arguments != null)
            {
                p.StartInfo.Arguments = arguments;
            }

            if (waitForExit)
            {
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
            }
            
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start(); 
            pid = p.Id;
            string standardOutput = string.Empty;
            string standardError = string.Empty;
            if (waitForExit)
            {
                standardOutput = p.StandardOutput.ReadToEnd();
                standardError = p.StandardError.ReadToEnd();
                p.WaitForExit();
            }
            if (checkStandardError && standardError != string.Empty)
            {
                throw new Exception("Failed to run " + fileName + " " + arguments + ", Error: " + standardError + ", StandardOutput: " + standardOutput);
            }
            return pid;  
        }

        public static bool CallIISReset()
        {
            int result = RunCommand("iisreset", null, false);
            return (result != -1);
        }

        public static bool StopHttp()
        {
            int result = RunCommand("net", "stop http /y", false);
            return (result != -1);
        }

        public static bool StopWas()
        {
            int result = RunCommand("net", "stop was /y", false);
            return (result != -1);
        }

        public static bool StartWas()
        {
            int result = RunCommand("net", "start was", false);
            return (result != -1);
        }

        public static bool StopW3svc()
        {
            int result = RunCommand("net", "stop w3svc /y", false);
            return (result != -1);
        }

        public static bool StartW3svc()
        {
            int result = RunCommand("net", "start w3svc", false);
            return (result != -1);
        }

        public static string GetApplicationPath()
        {
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;
            string solutionPath = InitializeTestMachine.GetSolutionDirectory();
            string applicationPath = string.Empty;
            applicationPath = Path.Combine(solutionPath, "test", "AspNetCoreModule.TestSites.Standard");
            return applicationPath;
        }

        public static string GetConfigContent(ServerType serverType, string iisConfig)
        {
            string content = null;
            if (serverType == ServerType.IISExpress)
            {
                content = File.ReadAllText(iisConfig);
            }
            return content;
        }
        
        public static void ClearApplicationEventLog()
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Clear();
            }
            for (int i = 0; i < 5; i++)
            {
                TestUtility.LogInformation("Waiting 1 seconds for eventlog to clear...");
                Thread.Sleep(1000);
                EventLog systemLog = new EventLog("Application");
                if (systemLog.Entries.Count == 0)
                {
                    break;
                }
            }
        }

        public static List<String> GetApplicationEvent(int id, DateTime startFrom)
        {
            var result = new List<String>();
            TestUtility.LogInformation("Waiting 1 seconds for eventlog to update...");
            Thread.Sleep(1000);
            EventLog systemLog = new EventLog("Application");
            foreach (EventLogEntry entry in systemLog.Entries)
            {
                if (entry.InstanceId == id && entry.TimeWritten >= startFrom)
                {
                    result.Add(entry.ReplacementStrings[0]);
                }
            }
            
            return result;
        }

        public static string ConvertToPunycode(string domain)
        {
            Uri uri = new Uri("http://" + domain);
            return uri.DnsSafeHost;
        }
    }
}