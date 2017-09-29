// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Sdk;
using System.Diagnostics;
using System.Net;
using System.Threading;
using AspNetCoreModule.Test.WebSocketClient;
using System.Text;
using System.IO;
using System.Security.Principal;
using System.IO.Compression;
using Microsoft.AspNetCore.Testing.xunit;

namespace AspNetCoreModule.Test
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ANCMTestFlags : Attribute, ITestCondition
    {
        private readonly string _attributeValue;
        public ANCMTestFlags(string attributeValue)
        {
            _attributeValue = attributeValue.ToString();
        }

        public bool IsMet
        {
            get
            {
                if (InitializeTestMachine.GlobalTestFlags.Contains(TestFlags.SkipTest))
                {
                    AdditionalInfo = TestFlags.SkipTest + " is set";
                    return false;
                }

                if (_attributeValue == TestFlags.RequireRunAsAdministrator 
                    && !InitializeTestMachine.GlobalTestFlags.Contains(TestFlags.RunAsAdministrator))
                { 
                    AdditionalInfo = _attributeValue + " is not belong to the given global test context(" + InitializeTestMachine.GlobalTestFlags + ")";
                    return false;
                }
                return true;
            }
        }

        public string SkipReason
        {
            get
            {
                return $"Skip condition: ANCMTestFlags: this test case is skipped becauset {AdditionalInfo}.";
            }
        }

        public string AdditionalInfo { get; set; }
    }

    public class FunctionalTestHelper
    {
        public FunctionalTestHelper()
        {
        }

        private const int _repeatCount = 3;

        public enum ReturnValueType
        {
            ResponseBody,
            ResponseBodyAndHeaders,
            ResponseStatus,
            None
        }

        public static async Task DoBasicTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoBasicTest"))
            {
                string backendProcessId_old = null;

                DateTime startTime = DateTime.Now;
                Thread.Sleep(3000);

                string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                Assert.NotEqual(backendProcessId_old, backendProcessId);
                var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), testSite.AspNetCoreApp.GetProcessFileName().ToLower().Replace(".exe", ""));

                Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                var httpClientHandler = new HttpClientHandler();
                var httpClient = new HttpClient(httpClientHandler)
                {
                    BaseAddress = testSite.AspNetCoreApp.GetUri(),
                    Timeout = TimeSpan.FromSeconds(5),
                };

                // Invoke given test scenario function
                await CheckChunkedAsync(httpClient, testSite.AspNetCoreApp);
            }
        }

        public static async Task DoRecycleApplicationAfterBackendProcessBeingKilled(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoRecycleApplicationAfterBackendProcessBeingKilled"))
            {
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(1000);

                    string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), testSite.AspNetCoreApp.GetProcessFileName().ToLower().Replace(".exe", ""));

                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                    backendProcess.Kill();
                    Thread.Sleep(500);
                }
            }
        }

        public static async Task DoRecycleApplicationAfterW3WPProcessBeingKilled(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoRecycleApplicationAfterW3WPProcessBeingKilled"))
            {
                string appDllFileName = testSite.AspNetCoreApp.GetArgumentFileName();

                if (testSite.IisServerType == ServerType.IISExpress)
                {
                    TestUtility.LogInformation("This test is not valid for IISExpress server type");
                    return;
                }

                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(1000);

                    string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), testSite.AspNetCoreApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    // get process id of IIS worker process (w3wp.exe)
                    string userName = testSite.SiteName;
                    int processIdOfWorkerProcess = Convert.ToInt32(TestUtility.GetProcessWMIAttributeValue("w3wp.exe", "Handle", userName));
                    var workerProcess = Process.GetProcessById(Convert.ToInt32(processIdOfWorkerProcess));
                    workerProcess.Kill();

                    Thread.Sleep(500);

                    // Verify the application file can be removed after worker process is stopped
                    testSite.AspNetCoreApp.BackupFile(appDllFileName);
                    testSite.AspNetCoreApp.DeleteFile(appDllFileName);
                    testSite.AspNetCoreApp.RestoreFile(appDllFileName);
                }
            }
        }

        public static async Task DoRecycleApplicationAfterWebConfigUpdated(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoRecycleApplicationAfterWebConfigUpdated"))
            {
                string backendProcessId_old = null;
                string appDllFileName = testSite.AspNetCoreApp.GetArgumentFileName();

                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(1000);

                    string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), testSite.AspNetCoreApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                    testSite.AspNetCoreApp.MoveFile("web.config", "_web.config");
                    Thread.Sleep(500);
                    testSite.AspNetCoreApp.MoveFile("_web.config", "web.config");

                    // Verify the application file can be removed after backend process is restarted
                    testSite.AspNetCoreApp.BackupFile(appDllFileName);
                    testSite.AspNetCoreApp.DeleteFile(appDllFileName);
                    testSite.AspNetCoreApp.RestoreFile(appDllFileName);
                }

                // restore web.config
                testSite.AspNetCoreApp.RestoreFile("web.config");

            }
        }

        public static async Task DoRecycleApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoRecycleApplicationWithURLRewrite"))
            {
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(1100);

                    string urlForUrlRewrite = testSite.URLRewriteApp.URL + "/Rewrite2/" + testSite.AspNetCoreApp.URL + "/GetProcessId";
                    string backendProcessId = await GetResponse(testSite.RootAppContext.GetUri(urlForUrlRewrite), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), testSite.AspNetCoreApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    testSite.AspNetCoreApp.MoveFile("web.config", "_web.config");
                    Thread.Sleep(500);
                    testSite.AspNetCoreApp.MoveFile("_web.config", "web.config");
                }

                // restore web.config
                testSite.AspNetCoreApp.RestoreFile("web.config");

            }
        }

        public static async Task DoRecycleParentApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoRecycleParentApplicationWithURLRewrite"))
            {
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(1000);

                    string urlForUrlRewrite = testSite.URLRewriteApp.URL + "/Rewrite2/" + testSite.AspNetCoreApp.URL + "/GetProcessId";
                    string backendProcessId = await GetResponse(testSite.RootAppContext.GetUri(urlForUrlRewrite), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), testSite.AspNetCoreApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                    testSite.RootAppContext.MoveFile("web.config", "_web.config");
                    Thread.Sleep(500);
                    testSite.RootAppContext.MoveFile("_web.config", "web.config");
                }

                // restore web.config
                testSite.RootAppContext.RestoreFile("web.config");
            }
        }

        public static async Task DoEnvironmentVariablesTest(string environmentVariableName, string environmentVariableValue, string expectedEnvironmentVariableValue, IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            if (environmentVariableName == null)
            {
                throw new InvalidDataException("envrionmentVarialbeName is null");
            }
            using (var testSite = new TestWebSite(appPoolBitness, "DoEnvironmentVariablesTest"))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    string totalNumber = await GetResponse(testSite.AspNetCoreApp.GetUri("GetEnvironmentVariables"), HttpStatusCode.OK);
                    Assert.True(totalNumber == (await GetResponse(testSite.AspNetCoreApp.GetUri("GetEnvironmentVariables"), HttpStatusCode.OK)));

                    iisConfig.SetANCMConfig(
                        testSite.SiteName,
                        testSite.AspNetCoreApp.Name,
                        "environmentVariable",
                        new string[] { "ANCMTestFoo", "foo" }
                        );

                    Thread.Sleep(500);

                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    int expectedValue = Convert.ToInt32(totalNumber) + 1;
                    string totalResult = (await GetResponse(testSite.AspNetCoreApp.GetUri("GetEnvironmentVariables"), HttpStatusCode.OK));
                    Assert.True(expectedValue.ToString() == (await GetResponse(testSite.AspNetCoreApp.GetUri("GetEnvironmentVariables"), HttpStatusCode.OK)));

                    bool setEnvironmentVariableConfiguration = true;

                    // Set authentication for ASPNETCORE_IIS_HTTPAUTH test scenarios
                    if (environmentVariableName == "ASPNETCORE_IIS_HTTPAUTH" && environmentVariableValue != "ignoredValue")
                    {
                        setEnvironmentVariableConfiguration = false;
                        bool windows = false;
                        bool basic = false;
                        bool anonymous = false;
                        if (environmentVariableValue.Contains("windows;"))
                        {
                            windows = true;
                        }
                        if (environmentVariableValue.Contains("basic;"))
                        {
                            basic = true;
                        }
                        if (environmentVariableValue.Contains("anonymous;"))
                        {
                            anonymous = true;
                        }
                        iisConfig.EnableIISAuthentication(testSite.SiteName, windows, basic, anonymous);
                    }

                    if (environmentVariableValue == "NA" || environmentVariableValue == null)
                    {
                        setEnvironmentVariableConfiguration = false;
                    }

                    // Add a new environment variable
                    if (setEnvironmentVariableConfiguration)
                    {
                        iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "environmentVariable", new string[] { environmentVariableName, environmentVariableValue });

                        // Adjust the new expected total number of environment variables
                        if (environmentVariableName != "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES" &&
                            environmentVariableName != "ASPNETCORE_IIS_HTTPAUTH")
                        {
                            expectedValue++;
                        }
                    }
                    Thread.Sleep(500);

                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);
                    totalResult = (await GetResponse(testSite.AspNetCoreApp.GetUri("GetEnvironmentVariables"), HttpStatusCode.OK));
                    Assert.True(expectedValue.ToString() == totalResult);
                    Assert.True("foo" == (await GetResponse(testSite.AspNetCoreApp.GetUri("ExpandEnvironmentVariablesANCMTestFoo"), HttpStatusCode.OK)));
                    Assert.True(expectedEnvironmentVariableValue == (await GetResponse(testSite.AspNetCoreApp.GetUri("ExpandEnvironmentVariables" + environmentVariableName), HttpStatusCode.OK)));

                    // Verify other common environment variables
                    string temp = (await GetResponse(testSite.AspNetCoreApp.GetUri("DumpEnvironmentVariables"), HttpStatusCode.OK));
                    Assert.Contains("ASPNETCORE_PORT", temp);
                    Assert.Contains("ASPNETCORE_APPL_PATH", temp);
                    Assert.Contains("ASPNETCORE_IIS_HTTPAUTH", temp);
                    Assert.Contains("ASPNETCORE_TOKEN", temp);
                    Assert.Contains("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", temp);

                    // Verify other inherited environment variables
                    Assert.Contains("PROCESSOR_ARCHITECTURE", temp);
                    Assert.Contains("USERNAME", temp);
                    Assert.Contains("USERDOMAIN", temp);
                    Assert.Contains("USERPROFILE", temp);
                }

                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoAppOfflineTestWithRenaming(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoAppOfflineTestWithRenaming"))
            {
                string backendProcessId_old = null;
                string fileContent = "BackEndAppOffline";
                string appDllFileName = testSite.AspNetCoreApp.GetArgumentFileName();

                testSite.AspNetCoreApp.CreateFile(new string[] { fileContent }, "App_Offline.Htm");
                                
                for (int i = 0; i < _repeatCount; i++)
                {
                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(1100);

                    // verify 503 
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), fileContent + "\r\n", HttpStatusCode.ServiceUnavailable);

                    // Verify the application file can be removed under app_offline mode
                    testSite.AspNetCoreApp.BackupFile(appDllFileName);
                    testSite.AspNetCoreApp.DeleteFile(appDllFileName);
                    testSite.AspNetCoreApp.RestoreFile(appDllFileName);

                    // rename app_offline.htm to _app_offline.htm and verify 200
                    testSite.AspNetCoreApp.MoveFile("App_Offline.Htm", "_App_Offline.Htm");
                    string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), testSite.AspNetCoreApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    // rename back to app_offline.htm
                    testSite.AspNetCoreApp.MoveFile("_App_Offline.Htm", "App_Offline.Htm");
                }
            }
        }

        public static async Task DoAppOfflineTestWithUrlRewriteAndDeleting(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoAppOfflineTestWithUrlRewriteAndDeleting"))
            {
                string backendProcessId_old = null;
                string fileContent = "BackEndAppOffline2";
                string appDllFileName = testSite.AspNetCoreApp.GetArgumentFileName();

                testSite.AspNetCoreApp.CreateFile(new string[] { fileContent }, "App_Offline.Htm");

                for (int i = 0; i < _repeatCount; i++)
                {
                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(1100);

                    // verify 503 
                    string urlForUrlRewrite = testSite.URLRewriteApp.URL + "/Rewrite2/" + testSite.AspNetCoreApp.URL + "/GetProcessId";
                    await VerifyResponseBody(testSite.RootAppContext.GetUri(urlForUrlRewrite), fileContent + "\r\n", HttpStatusCode.ServiceUnavailable);

                    // Verify the application file can be removed under app_offline mode
                    testSite.AspNetCoreApp.BackupFile(appDllFileName);
                    testSite.AspNetCoreApp.DeleteFile(appDllFileName);
                    testSite.AspNetCoreApp.RestoreFile(appDllFileName);

                    // delete app_offline.htm and verify 200 
                    testSite.AspNetCoreApp.DeleteFile("App_Offline.Htm");
                    string backendProcessId = await GetResponse(testSite.RootAppContext.GetUri(urlForUrlRewrite), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), testSite.AspNetCoreApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    // create app_offline.htm again
                    testSite.AspNetCoreApp.CreateFile(new string[] { fileContent }, "App_Offline.Htm");
                }
            }
        }

        public static async Task DoPostMethodTest(IISConfigUtility.AppPoolBitness appPoolBitness, string testData)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoPostMethodTest"))
            {
                var postFormData = new[]
                {
                new KeyValuePair<string, string>("FirstName", "Mickey"),
                new KeyValuePair<string, string>("LastName", "Mouse"),
                new KeyValuePair<string, string>("TestData", testData),
            };
                var expectedResponseBody = "FirstName=Mickey&LastName=Mouse&TestData=" + testData;
                await VerifyPostResponseBody(testSite.AspNetCoreApp.GetUri("EchoPostData"), postFormData, expectedResponseBody, HttpStatusCode.OK);
            }
        }

        public static async Task DoDisableStartUpErrorPageTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            int errorEventId = 1000;
            string errorMessageContainThis = "bogus"; // bogus path value to cause 502.3 error

            using (var testSite = new TestWebSite(appPoolBitness, "DoDisableStartUpErrorPageTest"))
            {
                testSite.AspNetCoreApp.DeleteFile("custom502-3.htm");
                string curstomErrorMessage = "ANCMTest502-3";
                testSite.AspNetCoreApp.CreateFile(new string[] { curstomErrorMessage }, "custom502-3.htm");

                Thread.Sleep(500);

                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    iisConfig.ConfigureCustomLogging(testSite.SiteName, testSite.AspNetCoreApp.Name, 502, 3, "custom502-3.htm");
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "disableStartUpErrorPage", true);
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "processPath", errorMessageContainThis);

                    var responseBody = await GetResponse(testSite.AspNetCoreApp.GetUri(), HttpStatusCode.BadGateway);
                    responseBody = responseBody.Replace("\r", "").Replace("\n", "").Trim();
                    Assert.True(responseBody == curstomErrorMessage);

                    // verify event error log
                    Assert.True(TestUtility.RetryHelper((arg1, arg2, arg3) => VerifyApplicationEventLog(arg1, arg2, arg3), errorEventId, startTime, errorMessageContainThis));

                    // try again after setting "false" value
                    startTime = DateTime.Now;
                    Thread.Sleep(500);

                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "disableStartUpErrorPage", false);
                    Thread.Sleep(500);

                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    responseBody = await GetResponse(testSite.AspNetCoreApp.GetUri(), HttpStatusCode.BadGateway);
                    Assert.Contains("808681", responseBody);

                    // verify event error log
                    Assert.True(TestUtility.RetryHelper((arg1, arg2, arg3) => VerifyApplicationEventLog(arg1, arg2, arg3), errorEventId, startTime, errorMessageContainThis));
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoRapidFailsPerMinuteTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfRapidFailsPerMinute)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoRapidFailsPerMinuteTest"))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    bool rapidFailsTriggered = false;
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "rapidFailsPerMinute", valueOfRapidFailsPerMinute);

                    string backendProcessId_old = null;
                    const int repeatCount = 10;

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(50);

                    for (int i = 0; i < repeatCount; i++)
                    {
                        // check JitDebugger before continuing 
                        TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                        DateTime startTimeInsideLooping = DateTime.Now;
                        Thread.Sleep(50);

                        var statusCode = await GetResponseStatusCode(testSite.AspNetCoreApp.GetUri("GetProcessId"));
                        if (statusCode != HttpStatusCode.OK.ToString())
                        {
                            Assert.True(i >= valueOfRapidFailsPerMinute, i.ToString() + "is greater than or equals to " + valueOfRapidFailsPerMinute.ToString());
                            Assert.True(i < valueOfRapidFailsPerMinute + 3, i.ToString() + "is less than " + (valueOfRapidFailsPerMinute + 3).ToString());
                            rapidFailsTriggered = true;
                            break;
                        }

                        string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                        Assert.NotEqual(backendProcessId_old, backendProcessId);
                        backendProcessId_old = backendProcessId;
                        var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                        Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), testSite.AspNetCoreApp.GetProcessFileName().ToLower().Replace(".exe", ""));

                        //Verifying EventID of new backend process is not necesssary and removed in order to fix some test reliablity issues
                        //Thread.Sleep(3000);
                        //Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTimeInsideLooping, backendProcessId), "Verifying event log of new backend process id " + backendProcessId);

                        backendProcess.Kill();
                        Thread.Sleep(3000);
                    }
                    Assert.True(rapidFailsTriggered, "Verify 502 Bad Gateway error");

                    // verify event error log
                    int errorEventId = 1003;
                    string errorMessageContainThis = "'" + valueOfRapidFailsPerMinute + "'"; // part of error message
                    Assert.True(TestUtility.RetryHelper((arg1, arg2, arg3) => VerifyApplicationEventLog(arg1, arg2, arg3), errorEventId, startTime, errorMessageContainThis));
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoProcessesPerApplicationTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfProcessesPerApplication)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoProcessesPerApplicationTest"))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(3000);

                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "processesPerApplication", valueOfProcessesPerApplication);
                    HashSet<int> processIDs = new HashSet<int>();

                    for (int i = 0; i < 20; i++)
                    {
                        string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                        int id = Convert.ToInt32(backendProcessId);
                        if (!processIDs.Contains(id))
                        {
                            processIDs.Add(id);
                        }

                        if (i == (valueOfProcessesPerApplication - 1))
                        {
                            Assert.Equal(valueOfProcessesPerApplication, processIDs.Count);
                        }
                    }

                    Assert.Equal(valueOfProcessesPerApplication, processIDs.Count);

                    foreach (var id in processIDs)
                    {
                        var backendProcess = Process.GetProcessById(id);
                        Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), testSite.AspNetCoreApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                        Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, id.ToString()));
                    }

                    // reset the value with 1 again
                    processIDs = new HashSet<int>();
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "processesPerApplication", 1);
                    Thread.Sleep(3000);

                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);
                    Thread.Sleep(500);

                    for (int i = 0; i < 20; i++)
                    {
                        string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                        int id = Convert.ToInt32(backendProcessId);
                        if (!processIDs.Contains(id))
                        {
                            processIDs.Add(id);
                        }
                    }
                    Assert.Single(processIDs);
                }

                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoStartupTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness, int startupTimeLimit)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoStartupTimeLimitTest"))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    int startupDelay = 3; //3 seconds
                    iisConfig.SetANCMConfig(
                        testSite.SiteName,
                        testSite.AspNetCoreApp.Name,
                        "environmentVariable",
                        new string[] { "ANCMTestStartUpDelay", (startupDelay * 1000).ToString() }
                        );

                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "requestTimeout", TimeSpan.Parse("00:01:00")); // 1 minute
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "startupTimeLimit", startupTimeLimit);

                    Thread.Sleep(500);
                    if (startupTimeLimit < startupDelay)
                    {
                        await VerifyResponseStatus(testSite.AspNetCoreApp.GetUri("DoSleep3000"), HttpStatusCode.BadGateway);
                    }
                    else
                    {
                        await VerifyResponseBody(testSite.AspNetCoreApp.GetUri("DoSleep3000"), "Running", HttpStatusCode.OK);
                    }
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoRequestTimeoutTest(IISConfigUtility.AppPoolBitness appPoolBitness, string requestTimeout)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoRequestTimeoutTest"))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "requestTimeout", TimeSpan.Parse(requestTimeout));
                    Thread.Sleep(500);

                    if (requestTimeout.ToString() == "00:02:00")
                    {
                        await VerifyResponseBody(testSite.AspNetCoreApp.GetUri("DoSleep65000"), "Running", HttpStatusCode.OK, timeout: 70);
                    }
                    else if (requestTimeout.ToString() == "00:01:00")
                    {
                        await VerifyResponseStatus(testSite.AspNetCoreApp.GetUri("DoSleep65000"), HttpStatusCode.BadGateway, 70);
                    }
                    else
                    {
                        throw new System.ApplicationException("wrong data");
                    }
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoShutdownTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfshutdownTimeLimit, int expectedClosingTime, bool isGraceFullShutdownEnabled)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoShutdownTimeLimitTest"))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    DateTime startTime = DateTime.Now;

                    // Make shutdownDelay time with hard coded value such as 20 seconds and test vairious shutdonwTimeLimit, either less than 20 seconds or bigger then 20 seconds
                    int shutdownDelayTime = 20000;
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "shutdownTimeLimit", valueOfshutdownTimeLimit);
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "environmentVariable", new string[] { "ANCMTestShutdownDelay", shutdownDelayTime.ToString() });
                    string expectedGracefulShutdownResponseStatusCode = "202";
                    if (!isGraceFullShutdownEnabled)
                    {
                        iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "environmentVariable", new string[] { "GracefulShutdown", "disabled" });
                        expectedGracefulShutdownResponseStatusCode = "200";
                    }

                    string response = await GetResponse(testSite.AspNetCoreApp.GetUri(""), HttpStatusCode.OK);
                    Assert.True(response == "Running");

                    string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));

                    // Set a new configuration value to make the backend process being recycled
                    DateTime startTime2 = DateTime.Now;
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "shutdownTimeLimit", 100);
                    backendProcess.WaitForExit(30000);
                    DateTime endTime = DateTime.Now;
                    var difference = endTime - startTime2;
                    Assert.True(difference.Seconds >= expectedClosingTime);
                    Assert.True(difference.Seconds < expectedClosingTime + 3);
                    string newBackendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.True(backendProcessId != newBackendProcessId);
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);

                    // if expectedClosing time is less than the shutdownDelay time, gracefulshutdown is supposed to fail and failure event is expected
                    if (expectedClosingTime * 1000 + 1000 == shutdownDelayTime)
                    {
                        Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMGracefulShutdownEvent(arg1, arg2), startTime, backendProcessId));
                        Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMGracefulShutdownEvent(arg1, arg2), startTime, expectedGracefulShutdownResponseStatusCode));
                    }
                    else
                    {
                        Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMGracefulShutdownFailureEvent(arg1, arg2), startTime, backendProcessId));
                    }
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }
        public static async Task DoStdoutLogEnabledTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoStdoutLogEnabledTest"))
            {
                testSite.AspNetCoreApp.DeleteDirectory("logs");

                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(3000);

                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "stdoutLogEnabled", true);
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "stdoutLogFile", @".\logs\stdout");

                    string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    string logPath = testSite.AspNetCoreApp.GetDirectoryPathWith("logs");
                    Assert.False(Directory.Exists(logPath));

                    Assert.True(TestUtility.RetryHelper((arg1, arg2, arg3) => VerifyApplicationEventLog(arg1, arg2, arg3), 1004, startTime, @"logs\stdout"));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    testSite.AspNetCoreApp.CreateDirectory("logs");

                    // verify the log file is not created because backend process is not recycled
                    Assert.True(Directory.GetFiles(logPath).Length == 0);
                    Assert.True(backendProcessId == (await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK)));

                    // reset web.config to recycle backend process and give write permission to the Users local group to which IIS workerprocess identity belongs
                    SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                    TestUtility.GiveWritePermissionTo(logPath, sid);

                    startTime = DateTime.Now;
                    Thread.Sleep(500);
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "stdoutLogEnabled", false);

                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "stdoutLogEnabled", true);

                    Assert.True(backendProcessId != (await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK)));

                    // Verify log file is created now after backend process is recycled
                    Assert.True(TestUtility.RetryHelper(p => { return Directory.GetFiles(p).Length > 0 ? true : false; }, logPath));
                }

                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoProcessPathAndArgumentsTest(IISConfigUtility.AppPoolBitness appPoolBitness, string processPath, string argumentsPrefix)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoProcessPathAndArgumentsTest", copyAllPublishedFiles: true))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    string arguments = argumentsPrefix + testSite.AspNetCoreApp.GetArgumentFileName();
                    string tempProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    var tempBackendProcess = Process.GetProcessById(Convert.ToInt32(tempProcessId));

                    // replace $env with the actual test value
                    if (processPath == "$env")
                    {
                        string tempString = Environment.ExpandEnvironmentVariables("%systemdrive%").ToLower();
                        processPath = Path.Combine(tempBackendProcess.MainModule.FileName).ToLower().Replace(tempString, "%systemdrive%");
                        arguments = testSite.AspNetCoreApp.GetDirectoryPathWith(arguments).ToLower().Replace(tempString, "%systemdrive%");
                    }

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "processPath", processPath);
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "arguments", arguments);
                    Thread.Sleep(500);

                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);
                    Thread.Sleep(500);

                    string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                }

                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoForwardWindowsAuthTokenTest(IISConfigUtility.AppPoolBitness appPoolBitness, bool enabledForwardWindowsAuthToken)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoForwardWindowsAuthTokenTest"))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    string result = string.Empty;
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "forwardWindowsAuthToken", enabledForwardWindowsAuthToken);
                    string requestHeaders = await GetResponse(testSite.AspNetCoreApp.GetUri("DumpRequestHeaders"), HttpStatusCode.OK);
                    Assert.DoesNotContain("MS-ASPNETCORE-WINAUTHTOKEN", requestHeaders.ToUpper());

                    iisConfig.EnableIISAuthentication(testSite.SiteName, windows: true, basic: false, anonymous: false);
                    Thread.Sleep(500);

                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);
                    Thread.Sleep(500);

                    requestHeaders = await GetResponse(testSite.AspNetCoreApp.GetUri("DumpRequestHeaders"), HttpStatusCode.OK);
                    if (enabledForwardWindowsAuthToken)
                    {

                        Assert.Contains("MS-ASPNETCORE-WINAUTHTOKEN", requestHeaders.ToUpper());

                        result = await GetResponse(testSite.AspNetCoreApp.GetUri("ImpersonateMiddleware"), HttpStatusCode.OK);
                        bool compare = false;

                        string expectedValue1 = "ImpersonateMiddleware-UserName = " + Environment.ExpandEnvironmentVariables("%USERDOMAIN%") + "\\" + Environment.ExpandEnvironmentVariables("%USERNAME%");
                        if (result.ToLower().Contains(expectedValue1.ToLower()))
                        {
                            compare = true;
                        }

                        string expectedValue2 = "ImpersonateMiddleware-UserName = " + Environment.ExpandEnvironmentVariables("%USERNAME%");
                        if (result.ToLower().Contains(expectedValue2.ToLower()))
                        {
                            compare = true;
                        }

                        Assert.True(compare);
                    }
                    else
                    {
                        Assert.DoesNotContain("MS-ASPNETCORE-WINAUTHTOKEN", requestHeaders.ToUpper());

                        result = await GetResponse(testSite.AspNetCoreApp.GetUri("ImpersonateMiddleware"), HttpStatusCode.OK);
                        Assert.Contains("ImpersonateMiddleware-UserName = NoAuthentication", result);
                    }
                }

                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoRecylingAppPoolTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoRecylingAppPoolTest"))
            {
                if (testSite.IisServerType == ServerType.IISExpress)
                {
                    TestUtility.LogInformation("This test is not valid for IISExpress server type");
                    return;
                }

                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {

                    // allocating 1024,000 KB
                    await VerifyResponseStatus(testSite.AspNetCoreApp.GetUri("MemoryLeak1024000"), HttpStatusCode.OK);

                    // get backend process id
                    string pocessIdBackendProcess = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);

                    // get process id of IIS worker process (w3wp.exe)
                    string userName = testSite.SiteName;
                    int processIdOfWorkerProcess = Convert.ToInt32(TestUtility.GetProcessWMIAttributeValue("w3wp.exe", "Handle", userName));
                    var workerProcess = Process.GetProcessById(Convert.ToInt32(processIdOfWorkerProcess));
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(pocessIdBackendProcess));

                    var privateMemoryKB = workerProcess.PrivateMemorySize64 / 1024;
                    var virtualMemoryKB = workerProcess.VirtualMemorySize64 / 1024;
                    var privateMemoryKBBackend = backendProcess.PrivateMemorySize64 / 1024;
                    var virtualMemoryKBBackend = backendProcess.VirtualMemorySize64 / 1024;
                    var totalPrivateMemoryKB = privateMemoryKB + privateMemoryKBBackend;
                    var totalVirtualMemoryKB = virtualMemoryKB + virtualMemoryKBBackend;

                    // terminate backend process
                    backendProcess.Kill();
                    backendProcess.Dispose();

                    // terminate IIS worker process
                    workerProcess.Kill();
                    workerProcess.Dispose();
                    Thread.Sleep(3000);

                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    iisConfig.SetAppPoolSetting(testSite.AspNetCoreApp.AppPoolName, "privateMemory", totalPrivateMemoryKB);

                    // set 100 for rapidFailProtection counter for both IIS worker process and aspnetcore backend process
                    iisConfig.SetAppPoolSetting(testSite.AspNetCoreApp.AppPoolName, "rapidFailProtectionMaxCrashes", 100);
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "rapidFailsPerMinute", 100);
                    Thread.Sleep(3000);

                    await VerifyResponseStatus(testSite.RootAppContext.GetUri("small.htm"), HttpStatusCode.OK);
                    Thread.Sleep(1000);
                    int x = Convert.ToInt32(TestUtility.GetProcessWMIAttributeValue("w3wp.exe", "Handle", userName));

                    // Verify that IIS recycling does not happen while there is no memory leak
                    bool foundVSJit = false;
                    for (int i = 0; i < 10; i++)
                    {
                        // check JitDebugger before continuing 
                        foundVSJit = TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                        await VerifyResponseStatus(testSite.RootAppContext.GetUri("small.htm"), HttpStatusCode.OK);
                        Thread.Sleep(3000);
                    }

                    int y = Convert.ToInt32(TestUtility.GetProcessWMIAttributeValue("w3wp.exe", "Handle", userName));
                    Assert.True(x == y && !foundVSJit, "worker process is not recycled after 30 seconds");

                    string backupPocessIdBackendProcess = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    string newPocessIdBackendProcess = backupPocessIdBackendProcess;

                    // Verify IIS recycling happens while there is memory leak
                    for (int i = 0; i < 10; i++)
                    {
                        // check JitDebugger before continuing 
                        foundVSJit = TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                        // allocating 2048,000 KB
                        await VerifyResponseStatus(testSite.AspNetCoreApp.GetUri("MemoryLeak2048000"), HttpStatusCode.OK);

                        newPocessIdBackendProcess = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                        if (foundVSJit || backupPocessIdBackendProcess != newPocessIdBackendProcess)
                        {
                            // worker process is recycled expectedly and backend process is recycled together
                            break;
                        }
                        Thread.Sleep(3000);
                    }
                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    int z = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        z = Convert.ToInt32(TestUtility.GetProcessWMIAttributeValue("w3wp.exe", "Handle", userName));
                        if (x != z)
                        {
                            break;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }
                    z = Convert.ToInt32(TestUtility.GetProcessWMIAttributeValue("w3wp.exe", "Handle", userName));
                    Assert.True(x != z, "worker process is recycled");

                    newPocessIdBackendProcess = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.True(backupPocessIdBackendProcess != newPocessIdBackendProcess, "backend process is recycled");
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoCompressionTest(IISConfigUtility.AppPoolBitness appPoolBitness, bool useCompressionMiddleWare, bool enableIISCompression)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoCompressionTest"))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    string startupClass = "StartupCompressionCaching";
                    if (!useCompressionMiddleWare)
                    {
                        startupClass = "StartupNoCompressionCaching";
                    }

                    // set startup class
                    iisConfig.SetANCMConfig(
                        testSite.SiteName,
                        testSite.AspNetCoreApp.Name,
                        "environmentVariable",
                        new string[] { "ANCMTestStartupClassName", startupClass }
                        );

                    // enable or IIS compression
                    // Note: IIS compression, however, will be ignored if AspnetCore compression middleware is enabled.
                    iisConfig.SetCompression(testSite.SiteName, enableIISCompression);

                    // prepare static contents
                    testSite.AspNetCoreApp.CreateDirectory("wwwroot");
                    testSite.AspNetCoreApp.CreateDirectory(@"wwwroot\pdir");

                    testSite.AspNetCoreApp.CreateFile(new string[] { "foohtm" }, @"wwwroot\foo.htm");
                    testSite.AspNetCoreApp.CreateFile(new string[] { "barhtm" }, @"wwwroot\pdir\bar.htm");
                    testSite.AspNetCoreApp.CreateFile(new string[] { "defaulthtm" }, @"wwwroot\default.htm");

                    string result = string.Empty;
                    if (!useCompressionMiddleWare && !enableIISCompression)
                    {
                        result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri("foo.htm"), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                        Assert.True(result.Contains("foohtm"), "verify response body");
                        Assert.False(result.Contains("Content-Encoding"), "verify response header");

                        result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri("pdir/bar.htm"), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                        Assert.True(result.Contains("barhtm"), "verify response body");
                        Assert.False(result.Contains("Content-Encoding"), "verify response header");

                        result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri(), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                        Assert.True(result.Contains("defaulthtm"), "verify response body");
                        Assert.False(result.Contains("Content-Encoding"), "verify response header");
                    }
                    else
                    {
                        result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri("foo.htm"), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                        Assert.True(result.Contains("foohtm"), "verify response body");
                        Assert.Equal("gzip", GetHeaderValue(result, "Content-Encoding"));

                        result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri("pdir/bar.htm"), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                        Assert.True(result.Contains("barhtm"), "verify response body");
                        Assert.Equal("gzip", GetHeaderValue(result, "Content-Encoding"));

                        result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri(), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                        Assert.True(result.Contains("defaulthtm"), "verify response body");
                        Assert.Equal("gzip", GetHeaderValue(result, "Content-Encoding"));
                    }
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoCachingTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoCachingTest"))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    string startupClass = "StartupCompressionCaching";

                    // set startup class
                    iisConfig.SetANCMConfig(
                        testSite.SiteName,
                        testSite.AspNetCoreApp.Name,
                        "environmentVariable",
                        new string[] { "ANCMTestStartupClassName", startupClass }
                        );

                    // enable IIS compression
                    // Note: IIS compression, however, will be ignored if AspnetCore compression middleware is enabled.
                    iisConfig.SetCompression(testSite.SiteName, true);

                    // prepare static contents
                    testSite.AspNetCoreApp.CreateDirectory("wwwroot");
                    testSite.AspNetCoreApp.CreateDirectory(@"wwwroot\pdir");

                    testSite.AspNetCoreApp.CreateFile(new string[] { "foohtm" }, @"wwwroot\foo.htm");
                    testSite.AspNetCoreApp.CreateFile(new string[] { "barhtm" }, @"wwwroot\pdir\bar.htm");
                    testSite.AspNetCoreApp.CreateFile(new string[] { "defaulthtm" }, @"wwwroot\default.htm");

                    string result = string.Empty;

                    const int retryCount = 3;
                    string headerValue = string.Empty;
                    string headerValue2 = string.Empty;
                    for (int i = 0; i < retryCount; i++)
                    {
                        result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri("foo.htm"), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                        headerValue = GetHeaderValue(result, "MyCustomHeader");
                        Assert.True(result.Contains("foohtm"), "verify response body");
                        Assert.Equal("gzip", GetHeaderValue(result, "Content-Encoding"));
                        Thread.Sleep(1500);

                        result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri("foo.htm"), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                        headerValue2 = GetHeaderValue(result, "MyCustomHeader");
                        Assert.True(result.Contains("foohtm"), "verify response body");
                        Assert.Equal("gzip", GetHeaderValue(result, "Content-Encoding"));
                        if (headerValue == headerValue2)
                        {
                            break;
                        }
                    }
                    Assert.Equal(headerValue, headerValue2);

                    Thread.Sleep(12000);
                    result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri("foo.htm"), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                    Assert.True(result.Contains("foohtm"), "verify response body");
                    Assert.Equal("gzip", GetHeaderValue(result, "Content-Encoding"));
                    string headerValue3 = GetHeaderValue(result, "MyCustomHeader");
                    Assert.NotEqual(headerValue2, headerValue3);
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoSendHTTPSRequestTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoSendHTTPSRequestTest", startIISExpress: false))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    string hostName = "";
                    string subjectName = "localhost";
                    string ipAddress = "*";
                    string hexIPAddress = "0x00";
                    int sslPort = InitializeTestMachine.SiteId + 6300;

                    // Add https binding and get https uri information
                    iisConfig.AddBindingToSite(testSite.SiteName, ipAddress, sslPort, hostName, "https");

                    // Create a self signed certificate
                    string thumbPrint = iisConfig.CreateSelfSignedCertificate(subjectName);

                    // Export the self signed certificate to rootCA
                    iisConfig.ExportCertificateTo(thumbPrint, sslStoreTo: @"Cert:\LocalMachine\Root");

                    // Configure http.sys ssl certificate mapping to IP:Port endpoint with the newly created self signed certificage
                    iisConfig.SetSSLCertificate(sslPort, hexIPAddress, thumbPrint);

                    // starting IISExpress was deffered after creating test applications and now it is ready to start it 
                    testSite.StartIISExpress();

                    // Verify http request
                    string result = string.Empty;
                    result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri(), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                    Assert.True(result.Contains("Running"), "verify response body");

                    // Verify https request
                    Uri targetHttpsUri = testSite.AspNetCoreApp.GetUri(null, sslPort, protocol: "https");
                    result = await GetResponseAndHeaders(targetHttpsUri, new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                    Assert.True(result.Contains("Running"), "verify response body");

                    // Remove the SSL Certificate mapping
                    iisConfig.RemoveSSLCertificate(sslPort, hexIPAddress);

                    // Remove the newly created self signed certificate
                    iisConfig.DeleteCertificate(thumbPrint);

                    // Remove the exported self signed certificate on rootCA
                    iisConfig.DeleteCertificate(thumbPrint, @"Cert:\LocalMachine\Root");
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoFilterOutMSRequestHeadersTest(IISConfigUtility.AppPoolBitness appPoolBitness, string requestHeader, string requestHeaderValue)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoSendHTTPSRequestTest", startIISExpress: false))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    string hostName = "";
                    string subjectName = "localhost";
                    string ipAddress = "*";
                    string hexIPAddress = "0x00";
                    int sslPort = InitializeTestMachine.SiteId + 6300;

                    // Add https binding and get https uri information
                    iisConfig.AddBindingToSite(testSite.SiteName, ipAddress, sslPort, hostName, "https");

                    // Create a self signed certificate
                    string thumbPrint = iisConfig.CreateSelfSignedCertificate(subjectName);

                    // Export the self signed certificate to rootCA
                    iisConfig.ExportCertificateTo(thumbPrint, sslStoreTo: @"Cert:\LocalMachine\Root");

                    // Configure http.sys ssl certificate mapping to IP:Port endpoint with the newly created self signed certificage
                    iisConfig.SetSSLCertificate(sslPort, hexIPAddress, thumbPrint);

                    // starting IISExpress was deffered after creating test applications and now it is ready to start it 
                    testSite.StartIISExpress();

                    // Verify http request
                    string requestHeaders = string.Empty;
                    requestHeaders = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri("DumpRequestHeaders"), new string[] { "Accept-Encoding", "gzip", requestHeader, requestHeaderValue }, HttpStatusCode.OK);
                    requestHeaders = requestHeaders.Replace(" ", "");
                    Assert.DoesNotContain(requestHeader.ToLower() + ":", requestHeaders.ToUpper());

                    // Verify https request
                    Uri targetHttpsUri = testSite.AspNetCoreApp.GetUri(null, sslPort, protocol: "https");
                    requestHeader = await GetResponseAndHeaders(targetHttpsUri, new string[] { "Accept-Encoding", "gzip", requestHeader, requestHeaderValue }, HttpStatusCode.OK);
                    requestHeaders = requestHeaders.Replace(" ", "");
                    Assert.DoesNotContain(requestHeader.ToLower() + ":", requestHeaders.ToUpper());

                    // Remove the SSL Certificate mapping
                    iisConfig.RemoveSSLCertificate(sslPort, hexIPAddress);

                    // Remove the newly created self signed certificate
                    iisConfig.DeleteCertificate(thumbPrint);

                    // Remove the exported self signed certificate on rootCA
                    iisConfig.DeleteCertificate(thumbPrint, @"Cert:\LocalMachine\Root");
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoClientCertificateMappingTest(IISConfigUtility.AppPoolBitness appPoolBitness, bool useHTTPSMiddleWare)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoClientCertificateMappingTest", startIISExpress: false))
            {
                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    string hostName = "";
                    string rootCN = "ANCMTest" + testSite.PostFix;
                    string webServerCN = "localhost";
                    string kestrelServerCN = "localhost";
                    string clientCN = "ANCMClient-" + testSite.PostFix;

                    string ipAddress = "*";
                    string hexIPAddress = "0x00";
                    int sslPort = InitializeTestMachine.SiteId + 6300;

                    // Add https binding and get https uri information
                    iisConfig.AddBindingToSite(testSite.SiteName, ipAddress, sslPort, hostName, "https");
                    
                    // Create a root certificate
                    string thumbPrintForRoot = iisConfig.CreateSelfSignedCertificateWithMakeCert(rootCN);

                    // Create a certificate for web server setting its issuer with the root certificate subject name
                    string thumbPrintForWebServer = iisConfig.CreateSelfSignedCertificateWithMakeCert(webServerCN, rootCN, extendedKeyUsage: "1.3.6.1.5.5.7.3.1");
                    string thumbPrintForKestrel = null;

                    // Create a certificate for client authentication setting its issuer with the root certificate subject name
                    string thumbPrintForClientAuthentication = iisConfig.CreateSelfSignedCertificateWithMakeCert(clientCN, rootCN, extendedKeyUsage: "1.3.6.1.5.5.7.3.2");

                    // Configure http.sys ssl certificate mapping to IP:Port endpoint with the newly created self signed certificage
                    iisConfig.SetSSLCertificate(sslPort, hexIPAddress, thumbPrintForWebServer);

                    // Create a new local administrator user
                    string userName = "tempuser" + TestUtility.RandomString(5);
                    string password = "AncmTest123!";
                    string temp;
                    temp = TestUtility.RunPowershellScript("net localgroup IIS_IUSRS /Delete " + userName);
                    temp = TestUtility.RunPowershellScript("net user " + userName + " /Delete");
                    temp = TestUtility.RunPowershellScript("net user " + userName + " " + password + " /ADD");
                    temp = TestUtility.RunPowershellScript("net localgroup IIS_IUSRS /Add " + userName);

                    // Get public key of the client certificate and Configure OnetToOneClientCertificateMapping the public key and disable anonymous authentication and set SSL flags for Client certificate authentication
                    string publicKey = iisConfig.GetCertificatePublicKey(thumbPrintForClientAuthentication, @"Cert:\CurrentUser\My");

                    bool setPasswordSeperately = false;
                    if (testSite.IisServerType == ServerType.IISExpress)
                    {
                        setPasswordSeperately = true;
                        iisConfig.EnableOneToOneClientCertificateMapping(testSite.SiteName, ".\\" + userName, null, publicKey);
                    }
                    else
                    {
                        iisConfig.EnableOneToOneClientCertificateMapping(testSite.SiteName, ".\\" + userName, password, publicKey);
                    }

                    // IISExpress uses a differnt encryption from full IIS version's and it is not easy to override the encryption methong with MWA. 
                    // As a work-around, password is set with updating the config file directly.
                    if (setPasswordSeperately)
                    {
                        // Search userName property and replace it with userName + password
                        string text = File.ReadAllText(testSite.IisExpressConfigPath);
                        text = text.Replace(userName + "\"", userName + "\"" + " " + "password=" + "\"" + password + "\"");
                        File.WriteAllText(testSite.IisExpressConfigPath, text);
                    }

                    // Configure kestrel SSL test environment
                    if (useHTTPSMiddleWare)
                    {
                        // set startup class
                        string startupClass = "StartupHTTPS";
                        iisConfig.SetANCMConfig(
                            testSite.SiteName,
                            testSite.AspNetCoreApp.Name,
                            "environmentVariable",
                            new string[] { "ANCMTestStartupClassName", startupClass }
                            );

                        // Create a certificate for Kestrel web server and export to TestResources\testcert.pfx
                        // NOTE: directory name "TestResources", file name "testcert.pfx" and password "testPassword" should be matched to AspnetCoreModule.TestSites.Standard web application
                        thumbPrintForKestrel = iisConfig.CreateSelfSignedCertificateWithMakeCert(kestrelServerCN, rootCN, extendedKeyUsage: "1.3.6.1.5.5.7.3.1");
                        testSite.AspNetCoreApp.CreateDirectory("TestResources");
                        string pfxFilePath = Path.Combine(testSite.AspNetCoreApp.GetDirectoryPathWith("TestResources"), "testcert.pfx");
                        iisConfig.ExportCertificateTo(thumbPrintForKestrel, sslStoreFrom: "Cert:\\LocalMachine\\My", sslStoreTo: pfxFilePath, pfxPassword: "testPassword");
                        Assert.True(File.Exists(pfxFilePath));
                    }

                    // starting IISExpress was deffered after creating test applications and now it is ready to start it 
                    Uri rootHttpsUri = testSite.RootAppContext.GetUri(null, sslPort, protocol: "https");
                    testSite.StartIISExpress();
                    TestUtility.RunPowershellScript("( invoke-webrequest " + rootHttpsUri.OriginalString + " -CertificateThumbprint " + thumbPrintForClientAuthentication + ").StatusCode", "200");

                    // Verify http request with using client certificate
                    Uri targetHttpsUri = testSite.AspNetCoreApp.GetUri(null, sslPort, protocol: "https");
                    string statusCode = TestUtility.RunPowershellScript("( invoke-webrequest " + targetHttpsUri.OriginalString + " -CertificateThumbprint " + thumbPrintForClientAuthentication + ").StatusCode");
                    Assert.Equal("200", statusCode);

                    // Verify https request with client certificate includes the certificate header "MS-ASPNETCORE-CLIENTCERT"
                    Uri targetHttpsUriForDumpRequestHeaders = testSite.AspNetCoreApp.GetUri("DumpRequestHeaders", sslPort, protocol: "https");
                    string outputRawContent = TestUtility.RunPowershellScript("( invoke-webrequest " + targetHttpsUriForDumpRequestHeaders.OriginalString + " -CertificateThumbprint " + thumbPrintForClientAuthentication + ").RawContent.ToString()");

                    Assert.Contains("MS-ASPNETCORE-CLIENTCERT", outputRawContent);

                    // Get the value of MS-ASPNETCORE-CLIENTCERT request header again and verify it is matched to its configured public key
                    Uri targetHttpsUriForCLIENTCERTRequestHeader = testSite.AspNetCoreApp.GetUri("GetRequestHeaderValueMS-ASPNETCORE-CLIENTCERT", sslPort, protocol: "https");
                    outputRawContent = TestUtility.RunPowershellScript("( invoke-webrequest " + targetHttpsUriForCLIENTCERTRequestHeader.OriginalString + " -CertificateThumbprint " + thumbPrintForClientAuthentication + ").RawContent.ToString()");
                    Assert.Contains(publicKey, outputRawContent);

                    // Verify non-https request returns 403.4 error
                    string result = string.Empty;
                    result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri(), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.Forbidden);
                    Assert.Contains("403.4", result);

                    // Verify https request without using client certificate returns 403.7
                    result = await GetResponseAndHeaders(targetHttpsUri, new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.Forbidden);
                    Assert.Contains("403.7", result);

                    // Clean up user
                    temp = TestUtility.RunPowershellScript("net localgroup IIS_IUSRS /Delete " + userName);
                    temp = TestUtility.RunPowershellScript("net user " + userName + " /Delete");

                    // Remove the SSL Certificate mapping
                    iisConfig.RemoveSSLCertificate(sslPort, hexIPAddress);

                    // Clean up certificates
                    iisConfig.DeleteCertificate(thumbPrintForRoot, @"Cert:\LocalMachine\Root");
                    iisConfig.DeleteCertificate(thumbPrintForWebServer, @"Cert:\LocalMachine\My");
                    if (useHTTPSMiddleWare)
                    {
                        iisConfig.DeleteCertificate(thumbPrintForKestrel, @"Cert:\LocalMachine\My");
                    }
                    iisConfig.DeleteCertificate(thumbPrintForClientAuthentication, @"Cert:\CurrentUser\My");
                }
                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoWebSocketTest(IISConfigUtility.AppPoolBitness appPoolBitness, string testData)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoWebSocketTest"))
            {
                string appDllFileName = testSite.AspNetCoreApp.GetArgumentFileName();

                using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                {
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "shutdownTimeLimit", 10);
                }

                DateTime startTime = DateTime.Now;

                await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);

                // Get Process ID
                string backendProcessId_old = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                
                // Verify WebSocket without setting subprotocol
                await VerifyResponseBodyContain(testSite.WebSocketApp.GetUri("echo.aspx"), new string[] { "Socket Open" }, HttpStatusCode.OK); // echo.aspx has hard coded path for the websocket server

                // Verify WebSocket subprotocol
                await VerifyResponseBodyContain(testSite.WebSocketApp.GetUri("echoSubProtocol.aspx"), new string[] { "Socket Open", "mywebsocketsubprotocol" }, HttpStatusCode.OK); // echoSubProtocol.aspx has hard coded path for the websocket server

                // Verify websocket 
                using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                {
                    var frameReturned = websocketClient.Connect(testSite.AspNetCoreApp.GetUri("websocket"), true, true);
                    Assert.Contains("Connection: Upgrade", frameReturned.Content);
                    Assert.Contains("HTTP/1.1 101 Switching Protocols", frameReturned.Content);
                    Thread.Sleep(500);

                    VerifySendingWebSocketData(websocketClient, testData);
                    Thread.Sleep(500);

                    frameReturned = websocketClient.Close();
                    Thread.Sleep(500);

                    Assert.True(frameReturned.FrameType == FrameType.Close, "Closing Handshake");
                }

                // send a simple request and verify the response body
                await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);

                Thread.Sleep(500);
                string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                Assert.Equal(backendProcessId_old, backendProcessId);

                // Verify server side websocket disconnection
                using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                {
                    for (int jj = 0; jj < 3; jj++)
                    {
                        var frameReturned = websocketClient.Connect(testSite.AspNetCoreApp.GetUri("websocket"), true, true);
                        Assert.Contains("Connection: Upgrade", frameReturned.Content);
                        Assert.Contains("HTTP/1.1 101 Switching Protocols", frameReturned.Content);
                        Thread.Sleep(500);

                        Assert.True(websocketClient.IsOpened, "Check active connection before starting");

                        // Send a special string to initiate the server side connection closing
                        websocketClient.SendTextData("CloseFromServer");
                        bool connectionClosedFromServer = websocketClient.WaitForWebSocketState(WebSocketState.ConnectionClosed);

                        // Verify server side connection closing is done successfully
                        Assert.True(connectionClosedFromServer, "Closing Handshake initiated from Server");

                        // extract text data from the last frame, which is the close frame
                        int lastIndex = websocketClient.Connection.DataReceived.Count - 1;

                        // Verify text data is matched to the string sent by server
                        Assert.Contains("ClosingFromServer", websocketClient.Connection.DataReceived[lastIndex].TextData);
                    }
                }

                // send a simple request and verify the response body
                await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);

                Thread.Sleep(500);
                backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                Assert.Equal(backendProcessId_old, backendProcessId);

                // Verify websocket with app_offline.htm
                using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                {
                    for (int jj = 0; jj < 3; jj++)
                    {
                        testSite.AspNetCoreApp.DeleteFile("App_Offline.Htm");
                        Thread.Sleep(1000);

                        var frameReturned = websocketClient.Connect(testSite.AspNetCoreApp.GetUri("websocket"), true, true);
                        Assert.Contains("Connection: Upgrade", frameReturned.Content);
                        Assert.Contains("HTTP/1.1 101 Switching Protocols", frameReturned.Content);
                        Thread.Sleep(500);

                        VerifySendingWebSocketData(websocketClient, testData);
                        Thread.Sleep(500);

                        // put app_offline
                        testSite.AspNetCoreApp.CreateFile(new string[] { "test" }, "App_Offline.Htm");
                        Thread.Sleep(1000);

                        // ToDo: This should be replaced when the server can handle this automaticially
                        // send a websocket data to invoke the server side websocket disconnection after the app_offline
                        websocketClient.SendTextData("test");
                        bool connectionClosedFromServer = websocketClient.WaitForWebSocketState(WebSocketState.ConnectionClosed);
                        
                        // Verify server side connection closing is done successfully
                        Assert.True(connectionClosedFromServer, "Closing Handshake initiated from Server");

                        // extract text data from the last frame, which is the close frame
                        int lastIndex = websocketClient.Connection.DataReceived.Count - 1;

                        // Verify text data is matched to the string sent by server
                        Assert.Contains("ClosingFromServer", websocketClient.Connection.DataReceived[lastIndex].TextData);

                        // Verify the application file can be removed under app_offline mode
                        testSite.AspNetCoreApp.BackupFile(appDllFileName);
                        testSite.AspNetCoreApp.DeleteFile(appDllFileName);
                        testSite.AspNetCoreApp.RestoreFile(appDllFileName);
                    }
                }

                // remove app_offline.htm
                testSite.AspNetCoreApp.DeleteFile("App_Offline.Htm");
                Thread.Sleep(1000);

                /*
                BugBug!!! configuration change does not invoke the shutdown message 
                because IIS does not trigger the change notification event until all websocket connection is gone.
                This scenario should be added back when the issue is resolved.

                // Verify websocket with configuration change notification
                using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                {
                    for (int jj = 0; jj < 10; jj++)
                    {
                        var frameReturned = websocketClient.Connect(testSite.AspNetCoreApp.GetUri("websocket"), true, true);
                        Assert.Contains("Connection: Upgrade", frameReturned.Content);
                        Assert.Contains("HTTP/1.1 101 Switching Protocols", frameReturned.Content);
                        Thread.Sleep(500);

                        VerifySendingWebSocketData(websocketClient, testData);
                        Thread.Sleep(500);

                        using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                        {
                            iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "startupTimeLimit", 11 + jj);
                            Thread.Sleep(1000);
                        }
                        
                        // ToDo: This should be replaced when the server can handle this automaticially
                        // send a websocket data to invoke the server side websocket disconnection after the app_offline
                        websocketClient.SendTextData("test");
                        bool connectionClosedFromServer = websocketClient.WaitForWebSocketState(WebSocketState.ConnectionClosed);
                        
                        // Verify server side connection closing is done successfully
                        Assert.True(connectionClosedFromServer, "Closing Handshake initiated from Server");

                        // extract text data from the last frame, which is the close frame
                        int lastIndex = websocketClient.Connection.DataReceived.Count - 1;

                        // Verify text data is matched to the string sent by server
                        Assert.Contains("ClosingFromServer", websocketClient.Connection.DataReceived[lastIndex].TextData);
                    }
                }
                */

                // send a simple request and verify the response body
                await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);
            }
        }
        
        public static async Task DoWebSocketErrorhandlingTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            try
            {
                using (var testSite = new TestWebSite(appPoolBitness, "DoWebSocketErrorhandlingTest"))
                {
                    // Verify websocket returns 404 when websocket module is not registered
                    using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
                    {
                        // Remove websocketModule
                        IISConfigUtility.BackupAppHostConfig("DoWebSocketErrorhandlingTest", true);
                        iisConfig.RemoveModule("WebSocketModule");

                        using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                        {
                            var frameReturned = websocketClient.Connect(testSite.AspNetCoreApp.GetUri("websocket"), true, true, waitForConnectionOpen:false);
                            Assert.DoesNotContain("Connection: Upgrade", frameReturned.Content);

                            //BugBug: Currently we returns 101 here.
                            //Assert.DoesNotContain("HTTP/1.1 101 Switching Protocols", frameReturned.Content);
                        }
                    }

                    // send a simple request again and verify the response body
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);

                    // roback configuration 
                    IISConfigUtility.RestoreAppHostConfig("DoWebSocketErrorhandlingTest", true);
                }
            }
            catch
            {
                // roback configuration 
                IISConfigUtility.RestoreAppHostConfig("DoWebSocketErrorhandlingTest", true);
                throw;
            }
        }

        public enum DoAppVerifierTest_ShutDownMode
        {
            RecycleAppPool,
            CreateAppOfflineHtm,
            StopAndStartAppPool,
            RestartW3SVC,
            ConfigurationChangeNotification
        }

        public enum DoAppVerifierTest_StartUpMode
        {
            UseGracefulShutdown,
            DontUseGracefulShutdown
        }

        public static async Task DoAppVerifierTest(IISConfigUtility.AppPoolBitness appPoolBitness, bool verifyTimeout, DoAppVerifierTest_StartUpMode startUpMode, DoAppVerifierTest_ShutDownMode shutDownMode, int repeatCount = 2)
        {
            TestWebSite testSite = null;
            bool testResult = false;
                        
            testSite = new TestWebSite(appPoolBitness, "DoAppVerifierTest", startIISExpress: false);
            if (testSite.IisServerType == ServerType.IISExpress)
            {
                TestUtility.LogInformation("This test is not valid for IISExpress server type because of IISExpress bug; Once it is resolved, we should activate this test for IISExpress as well");
                return;
            }

            // enable AppVerifier 
            testSite.AttachAppverifier();

            using (var iisConfig = new IISConfigUtility(testSite.IisServerType, testSite.IisExpressConfigPath))
            {
                // Prepare https binding
                string hostName = "";
                string subjectName = "localhost";
                string ipAddress = "*";
                string hexIPAddress = "0x00";
                int sslPort = InitializeTestMachine.SiteId + 6300;

                // Add https binding and get https uri information
                iisConfig.AddBindingToSite(testSite.SiteName, ipAddress, sslPort, hostName, "https");

                // Create a self signed certificate
                string thumbPrint = iisConfig.CreateSelfSignedCertificate(subjectName);

                // Export the self signed certificate to rootCA
                iisConfig.ExportCertificateTo(thumbPrint, sslStoreTo: @"Cert:\LocalMachine\Root");

                // Configure http.sys ssl certificate mapping to IP:Port endpoint with the newly created self signed certificage
                iisConfig.SetSSLCertificate(sslPort, hexIPAddress, thumbPrint);

                // Set shutdownTimeLimit with 3 seconds and use 5 seconds for delay time to make the shutdownTimeout happen
                iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "shutdownTimeLimit", 3);

                int timeoutValue = 3;
                if (verifyTimeout)
                {
                    // set requestTimeout
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "requestTimeout", TimeSpan.Parse("00:01:00")); // 1 minute

                    // set startupTimeout
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "startupTimeLimit", timeoutValue);

                    // Set shutdownTimeLimit with 3 seconds and use 5 seconds for delay time to make the shutdownTimeout happen
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "shutdownTimeLimit", timeoutValue);
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "environmentVariable", new string[] { "ANCMTestShutdownDelay", "10" });
                }

                // starting IISExpress was deffered after creating test applications and now it is ready to start it 
                testSite.StartIISExpress();

                if (verifyTimeout)
                {
                    Thread.Sleep(500);

                    // initial request which requires more than startup timeout should fails
                    await VerifyResponseStatus(testSite.AspNetCoreApp.GetUri("DoSleep5000"), HttpStatusCode.BadGateway, timeout: 10);
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri("DoSleep5000"), "Running", HttpStatusCode.OK, timeout: 10);

                    // request which requires more than request timeout should fails
                    await VerifyResponseStatus(testSite.AspNetCoreApp.GetUri("DoSleep65000"), HttpStatusCode.BadGateway, timeout: 70);
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri("DoSleep50000"), "Running", HttpStatusCode.OK, timeout: 70);                        
                }

                ///////////////////////////////////
                // Start test sceanrio
                ///////////////////////////////////
                if (startUpMode == DoAppVerifierTest_StartUpMode.DontUseGracefulShutdown)
                {
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "environmentVariable", new string[] { "GracefulShutdown", "disabled" });
                }

                // reset existing worker process process
                TestUtility.ResetHelper(ResetHelperMode.KillWorkerProcess);
                                
                for (int i = 0; i < repeatCount; i++)
                {
                    // reset worker process id to refresh
                    testSite.WorkerProcessID = 0;

                    // send a startup request to start a new worker process
                    TestUtility.RunPowershellScript("( invoke-webrequest http://localhost:" + testSite.TcpPort + " ).StatusCode", "200", retryCount: 5);

                    // attach debugger to the worker process
                    testSite.AttachWinDbg(testSite.WorkerProcessID);
                    TestUtility.RunPowershellScript("( invoke-webrequest http://localhost:" + testSite.TcpPort + " ).StatusCode", "200", retryCount: 30);

                    // verify windbg process is started
                    TestUtility.RunPowershellScript("(get-process -name windbg 2> $null).count", "1", retryCount: 5);

                    DateTime startTime = DateTime.Now;

                    // Verify http request
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);

                    // Get Process ID
                    string backendProcessId_old = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                        
                    // Verify WebSocket without setting subprotocol
                    await VerifyResponseBodyContain(testSite.WebSocketApp.GetUri("echo.aspx"), new string[] { "Socket Open" }, HttpStatusCode.OK); // echo.aspx has hard coded path for the websocket server

                    // Verify WebSocket subprotocol
                    await VerifyResponseBodyContain(testSite.WebSocketApp.GetUri("echoSubProtocol.aspx"), new string[] { "Socket Open", "mywebsocketsubprotocol" }, HttpStatusCode.OK); // echoSubProtocol.aspx has hard coded path for the websocket server

                    string testData = "test";

                    // Verify websocket 
                    using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                    {
                        var frameReturned = websocketClient.Connect(testSite.AspNetCoreApp.GetUri("websocket"), true, true);
                        Assert.Contains("Connection: Upgrade", frameReturned.Content);
                        Assert.Contains("HTTP/1.1 101 Switching Protocols", frameReturned.Content);
                        Thread.Sleep(500);

                        VerifySendingWebSocketData(websocketClient, testData);
                        Thread.Sleep(500);

                        frameReturned = websocketClient.Close();
                        Thread.Sleep(500);

                        Assert.True(frameReturned.FrameType == FrameType.Close, "Closing Handshake");
                    }

                    // send a simple request and verify the response body
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);

                    Thread.Sleep(500);
                    string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.Equal(backendProcessId_old, backendProcessId);

                    // Verify server side websocket disconnection
                    using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                    {
                        var frameReturned = websocketClient.Connect(testSite.AspNetCoreApp.GetUri("websocket"), true, true);
                        Assert.Contains("Connection: Upgrade", frameReturned.Content);
                        Assert.Contains("HTTP/1.1 101 Switching Protocols", frameReturned.Content);
                        Thread.Sleep(500);

                        Assert.True(websocketClient.IsOpened, "Check active connection before starting");

                        // Send a special string to initiate the server side connection closing
                        websocketClient.SendTextData("CloseFromServer");
                        bool connectionClosedFromServer = websocketClient.WaitForWebSocketState(WebSocketState.ConnectionClosed);

                        // Verify server side connection closing is done successfully
                        Assert.True(connectionClosedFromServer, "Closing Handshake initiated from Server");

                        // extract text data from the last frame, which is the close frame
                        int lastIndex = websocketClient.Connection.DataReceived.Count - 1;

                        // Verify text data is matched to the string sent by server
                        Assert.Contains("ClosingFromServer", websocketClient.Connection.DataReceived[lastIndex].TextData);
                    }

                    // send a simple request and verify the response body
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);

                    Thread.Sleep(500);
                    backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.Equal(backendProcessId_old, backendProcessId);

                    if (startUpMode != DoAppVerifierTest_StartUpMode.DontUseGracefulShutdown)
                    {
                        // Verify websocket with app_offline.htm
                        using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                        {
                            for (int jj = 0; jj < 10; jj++)
                            {
                                testSite.AspNetCoreApp.DeleteFile("App_Offline.Htm");
                                Thread.Sleep(1000);

                                var frameReturned = websocketClient.Connect(testSite.AspNetCoreApp.GetUri("websocket"), true, true);
                                Assert.Contains("Connection: Upgrade", frameReturned.Content);
                                Assert.Contains("HTTP/1.1 101 Switching Protocols", frameReturned.Content);
                                Thread.Sleep(500);

                                VerifySendingWebSocketData(websocketClient, testData);
                                Thread.Sleep(500);

                                // put app_offline
                                testSite.AspNetCoreApp.CreateFile(new string[] { "test" }, "App_Offline.Htm");
                                Thread.Sleep(500);

                                // ToDo: remove this when server can handle this automatically
                                // send a websocket data to invoke the server side websocket disconnection after the app_offline
                                websocketClient.SendTextData("test");
                                bool connectionClosedFromServer = websocketClient.WaitForWebSocketState(WebSocketState.ConnectionClosed);

                                // Verify server side connection closing is done successfully
                                Assert.True(connectionClosedFromServer, "Closing Handshake initiated from Server");
                            }
                        }

                        // remove app_offline.htm
                        testSite.AspNetCoreApp.DeleteFile("App_Offline.Htm");
                        Thread.Sleep(500);
                    }
                    
                    // Verify websocket again
                    using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                    {
                        var frameReturned = websocketClient.Connect(testSite.AspNetCoreApp.GetUri("websocket"), true, true);
                        Assert.Contains("Connection: Upgrade", frameReturned.Content);
                        Assert.Contains("HTTP/1.1 101 Switching Protocols", frameReturned.Content);
                        Thread.Sleep(500);

                        VerifySendingWebSocketData(websocketClient, testData);
                        Thread.Sleep(500);

                        frameReturned = websocketClient.Close();
                        Thread.Sleep(500);

                        Assert.True(frameReturned.FrameType == FrameType.Close, "Closing Handshake");
                    }

                    // send a simple request and verify the response body
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);

                    // Verify https request
                    Uri targetHttpsUri = testSite.AspNetCoreApp.GetUri(null, sslPort, protocol: "https");
                    var result = await GetResponseAndHeaders(targetHttpsUri, new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                    Assert.True(result.Contains("Running"), "verify response body");
                                                
                    switch (shutDownMode)
                    {
                        case DoAppVerifierTest_ShutDownMode.StopAndStartAppPool:
                            iisConfig.StopAppPool(testSite.AspNetCoreApp.AppPoolName);
                            Thread.Sleep(5000);
                            iisConfig.StartAppPool(testSite.AspNetCoreApp.AppPoolName);                            
                            break;
                        case DoAppVerifierTest_ShutDownMode.RestartW3SVC:
                            TestUtility.ResetHelper(ResetHelperMode.StopWasStartW3svc);
                            break;
                        case DoAppVerifierTest_ShutDownMode.CreateAppOfflineHtm:
                            testSite.AspNetCoreApp.DeleteFile("App_Offline.Htm");
                            testSite.AspNetCoreApp.CreateFile(new string[] { "test" }, "App_Offline.Htm");
                            break;
                        case DoAppVerifierTest_ShutDownMode.ConfigurationChangeNotification:
                            iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "startupTimeLimit", timeoutValue + 1);
                            iisConfig.RecycleAppPool(testSite.AspNetCoreApp.AppPoolName);
                            break;
                        case DoAppVerifierTest_ShutDownMode.RecycleAppPool:
                            iisConfig.RecycleAppPool(testSite.AspNetCoreApp.AppPoolName);
                            break;
                    }                        
                    Thread.Sleep(2000);

                    if (verifyTimeout)
                    {
                        // Wait for shutdown delay additionally
                        Thread.Sleep(timeoutValue * 1000);
                    }

                    switch (shutDownMode)
                    {
                        case DoAppVerifierTest_ShutDownMode.CreateAppOfflineHtm:
                            // verify app_offline.htm file works
                            await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "test" + "\r\n", HttpStatusCode.ServiceUnavailable);

                            // remove app_offline.htm file and then recycle apppool
                            testSite.AspNetCoreApp.MoveFile("App_Offline.Htm", "_App_Offline.Htm");
                            iisConfig.RecycleAppPool(testSite.AspNetCoreApp.AppPoolName);
                            Thread.Sleep(2000);
                            break;
                    }

                    // verify windbg process is gone, which means there was no unexpected error
                    TestUtility.RunPowershellScript("(get-process -name windbg 2> $null).count", "0", retryCount: 5);
                }

                // clean up https test environment

                // Remove the SSL Certificate mapping
                iisConfig.RemoveSSLCertificate(sslPort, hexIPAddress);

                // Remove the newly created self signed certificate
                iisConfig.DeleteCertificate(thumbPrint);

                // Remove the exported self signed certificate on rootCA
                iisConfig.DeleteCertificate(thumbPrint, @"Cert:\LocalMachine\Root");
            }
            
            // cleanup
            if (testSite != null)
            {
                testSite.DetachAppverifier();
            }
            TestUtility.ResetHelper(ResetHelperMode.KillWorkerProcess);

            // cleanup windbg process incase it is still running
            if (!testResult)
            {
                TestUtility.RunPowershellScript("stop-process -Name windbg -Force -Confirm:$false 2> $null");
            }
        }

        private static string GetHeaderValue(string inputData, string headerName)
        {
            string result = string.Empty;
            foreach (string item in inputData.Split(new char[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (item.Contains(headerName))
                {
                    var tokens = item.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length == 2)
                    {
                        result = tokens[1].Trim();
                        break;
                    }
                }
            }
            return result;
        }

        private static bool VerifySendingWebSocketData(WebSocketClientHelper websocketClient, string testData)
        {
            bool result = false;

            //
            // send complete or partial text data and ping multiple times
            //
            websocketClient.SendTextData(testData);
            websocketClient.SendPing();
            websocketClient.SendTextData(testData);
            websocketClient.SendPing();
            websocketClient.SendPing();
            websocketClient.SendTextData(testData, 0x01);  // 0x01: start of sending partial data
            websocketClient.SendPing();
            websocketClient.SendTextData(testData, 0x80);  // 0x80: end of sending partial data
            websocketClient.SendPing();
            websocketClient.SendPing();
            websocketClient.SendTextData(testData);
            websocketClient.SendTextData(testData);
            websocketClient.SendTextData(testData);
            websocketClient.SendPing();
            Thread.Sleep(3000);

            // Verify test result
            for (int i = 0; i < 3; i++)
            {
                if (!DoVerifyDataSentAndReceived(websocketClient))
                {
                    // retrying after 1 second sleeping
                    Thread.Sleep(1000);
                }
                else
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        private static bool DoVerifyDataSentAndReceived(WebSocketClientHelper websocketClient)
        {
            var result = true;
            var sentString = new StringBuilder();
            var recString = new StringBuilder();
            var pingString = new StringBuilder();
            var pongString = new StringBuilder();

            foreach (Frame frame in websocketClient.Connection.DataSent.ToArray())
            {
                if (frame.FrameType == FrameType.Continuation
                    || frame.FrameType == FrameType.SegmentedText
                        || frame.FrameType == FrameType.Text
                            || frame.FrameType == FrameType.ContinuationFrameEnd)
                {
                    sentString.Append(frame.Content);
                }

                if (frame.FrameType == FrameType.Ping)
                {
                    pingString.Append(frame.Content);
                }
            }

            foreach (Frame frame in websocketClient.Connection.DataReceived.ToArray())
            {
                if (frame.FrameType == FrameType.Continuation
                    || frame.FrameType == FrameType.SegmentedText
                        || frame.FrameType == FrameType.Text
                            || frame.FrameType == FrameType.ContinuationFrameEnd)
                {
                    recString.Append(frame.Content);
                }

                if (frame.FrameType == FrameType.Pong)
                {
                    pongString.Append(frame.Content);
                }
            }

            if (sentString.Length == recString.Length && pongString.Length == pingString.Length)
            {
                if (sentString.Length != recString.Length)
                {
                    result = false;
                    TestUtility.LogInformation("Same size of data sent(" + sentString.Length + ") and received(" + recString.Length + ")");
                }

                if (sentString.ToString() != recString.ToString())
                {
                    result = false;
                    TestUtility.LogInformation("Not matched string in sent and received");
                }
                if (pongString.Length != pingString.Length)
                {
                    result = false;
                    TestUtility.LogInformation("Ping received; Ping (" + pingString.Length + ") and Pong (" + pongString.Length + ")");
                }
                websocketClient.Connection.DataSent.Clear();
                websocketClient.Connection.DataReceived.Clear();
            }
            else
            {
                TestUtility.LogInformation("Retrying...  so far data sent(" + sentString.Length + ") and received(" + recString.Length + ")");
                result = false;
            }
            return result;
        }

        private static async Task CheckChunkedAsync(HttpClient client, TestWebApplication webApp)
        {
            var response = await client.GetAsync(webApp.GetUri("chunked"));
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal("Chunked", responseText);
                Assert.True(response.Headers.TransferEncodingChunked, "/chunked, chunked?");
                Assert.Null(response.Headers.ConnectionClose);
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException ex)
            {
                TestUtility.LogInformation(response.ToString());
                TestUtility.LogInformation(responseText);
                throw ex;
            }
        }

        private static string GetContentLength(HttpResponseMessage response)
        {
            // Don't use response.Content.Headers.ContentLength, it will dynamically calculate the value if it can.
            IEnumerable<string> values;
            return response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out values) ? values.FirstOrDefault() : null;
        }

        private static bool VerifyANCMStartEvent(DateTime startFrom, string includeThis)
        {
            return VerifyEventLog(1001, startFrom, includeThis);
        }

        private static bool VerifyANCMGracefulShutdownEvent(DateTime startFrom, string includeThis)
        {
            return VerifyEventLog(1006, startFrom, includeThis);
        }

        private static bool VerifyANCMGracefulShutdownFailureEvent(DateTime startFrom, string includeThis)
        {
            return VerifyEventLog(1005, startFrom, includeThis);
        }

        private static bool VerifyApplicationEventLog(int eventID, DateTime startFrom, string includeThis)
        {
            return VerifyEventLog(eventID, startFrom, includeThis);
        }

        private static bool VerifyEventLog(int eventId, DateTime startFrom, string includeThis = null)
        {
            var events = TestUtility.GetApplicationEvent(eventId, startFrom);
            Assert.True(events.Count > 0, "Verfiy expected event logs");
            bool findEvent = false;
            foreach (string item in events)
            {
                if (item.Contains(includeThis))
                {
                    findEvent = true;
                    break;
                }
            }
            return findEvent;
        }

        private static async Task VerifyResponseStatus(Uri uri, HttpStatusCode expectedResponseStatus, int timeout = 5, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, null, null, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, postData: null, timeout: timeout);
        }

        private static async Task VerifyResponseBody(Uri uri, string expectedResponseBody, HttpStatusCode expectedResponseStatus, int timeout = 5, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, null, expectedResponseBody, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, postData:null, timeout:timeout);
        }

        private static async Task VerifyPostResponseBody(Uri uri, KeyValuePair<string, string>[] postData, string expectedResponseBody, HttpStatusCode expectedResponseStatus, int timeout = 5, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, null, expectedResponseBody, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, postData, timeout);
        }

        private static async Task VerifyResponseBodyContain(Uri uri, string[] expectedStrings, HttpStatusCode expectedResponseStatus, int timeout = 5, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, null, null, expectedStrings, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, postData: null, timeout: timeout);
        }

        private static async Task<string> GetResponse(Uri uri, HttpStatusCode expectedResponseStatus, ReturnValueType returnValueType = ReturnValueType.ResponseBody, int timeout = 5, int numberOfRetryCount = 1, bool verifyResponseFlag = true)
        {
            return await SendReceive(uri, null, null, null, expectedResponseStatus, returnValueType, numberOfRetryCount, verifyResponseFlag, postData:null, timeout:timeout);
        }

        private static async Task<string> GetResponseAndHeaders(Uri uri, string[] requestHeaders, HttpStatusCode expectedResponseStatus, ReturnValueType returnValueType = ReturnValueType.ResponseBodyAndHeaders, int timeout = 5, int numberOfRetryCount = 1, bool verifyResponseFlag = true)
        {
            return await SendReceive(uri, requestHeaders, null, null, expectedResponseStatus, returnValueType, numberOfRetryCount, verifyResponseFlag, postData: null, timeout: timeout);
        }

        private static async Task<string> GetResponseStatusCode(Uri uri)
        {
            return await SendReceive(uri, null, null, null, HttpStatusCode.OK, ReturnValueType.ResponseStatus, numberOfRetryCount:1, verifyResponseFlag:false, postData:null, timeout:5);
        }
        
        private static async Task<string> ReadContent(HttpResponseMessage response)
        {
            bool unZipContent = false;
            string result = String.Empty;

            IEnumerable<string> values;
            if (response.Headers.TryGetValues("Vary", out values))                    
            {
                unZipContent = true;
            }

            if (unZipContent)
            {
                var inputStream = await response.Content.ReadAsStreamAsync();

                // for debugging purpose
                //byte[] temp = new byte[inputStream.Length];
                //inputStream.Read(temp, 0, (int) inputStream.Length);
                //inputStream.Position = 0;

                using (var gzip = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    var outputStream = new MemoryStream();
                    try
                    {
                        await gzip.CopyToAsync(outputStream);
                    }
                    catch (Exception ex)
                    {
                        // Even though "Vary" response header exists, the content is not actually compressed.
                        // We should ignore this execption until we find a proper way to determine if the body is compressed or not.
                        if (ex.Message.IndexOf("gzip", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            result = await response.Content.ReadAsStringAsync();
                            return result;
                        }
                        throw ex;
                    }
                    gzip.Close();
                    inputStream.Close();
                    outputStream.Position = 0;
                    using (StreamReader reader = new StreamReader(outputStream, Encoding.UTF8))
                    {
                        result = reader.ReadToEnd();
                        outputStream.Close();
                    }
                }
            }
            else
            {
                result = await response.Content.ReadAsStringAsync();
            }
            return result;
        }

        private static async Task<string> SendReceive(Uri uri, string[] requestHeaders, string expectedResponseBody, string[] expectedStringsInResponseBody, HttpStatusCode expectedResponseStatus, ReturnValueType returnValueType, int numberOfRetryCount, bool verifyResponseFlag, KeyValuePair < string, string>[] postData, int timeout)
        {
            string result = null;
            string responseText = "NotInitialized";
            string responseStatus = "NotInitialized";

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.UseDefaultCredentials = true;
            httpClientHandler.AutomaticDecompression = DecompressionMethods.None;

            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = uri,
                Timeout = TimeSpan.FromSeconds(timeout),                
            };
            
            if (requestHeaders != null)
            {
                for (int i = 0; i < requestHeaders.Length; i=i+2)
                {
                    httpClient.DefaultRequestHeaders.Add(requestHeaders[i], requestHeaders[i+1]);
                }
            }

            HttpResponseMessage response = null;
            try
            {
                FormUrlEncodedContent postHttpContent = null;
                if (postData != null)
                {
                    postHttpContent = new FormUrlEncodedContent(postData);
                }
                
                if (numberOfRetryCount > 1 && expectedResponseStatus == HttpStatusCode.OK)
                {
                    if (postData == null)
                    {
                        response = await TestUtility.RetryRequest(() =>
                        {
                            return httpClient.GetAsync(string.Empty);
                        }, TestUtility.Logger, retryCount: numberOfRetryCount);
                    }
                    else
                    {
                        response = await TestUtility.RetryRequest(() =>
                        {                            
                            return httpClient.PostAsync(string.Empty, postHttpContent);
                        }, TestUtility.Logger, retryCount: numberOfRetryCount);
                    }
                }
                else
                {
                    if (postData == null)
                    {
                        response = await httpClient.GetAsync(string.Empty);
                    }
                    else
                    {
                        response = await httpClient.PostAsync(string.Empty, postHttpContent);
                    }
                }

                if (response != null)
                {
                    responseStatus = response.StatusCode.ToString();
                    if (verifyResponseFlag)
                    {
                        if (expectedResponseBody != null)
                        {
                            if (responseText == "NotInitialized")
                            {
                                responseText = await ReadContent(response);
                            }
                            Assert.Equal(expectedResponseBody, responseText);
                        }

                        if (expectedStringsInResponseBody != null)
                        {
                            if (responseText == "NotInitialized")
                            {
                                responseText = await ReadContent(response);
                            }
                            foreach (string item in expectedStringsInResponseBody)
                            {
                                Assert.Contains(item, responseText);
                            }
                        }
                        Assert.Equal(expectedResponseStatus, response.StatusCode);
                    }

                    switch (returnValueType)
                    {
                        case ReturnValueType.ResponseBody:
                        case ReturnValueType.ResponseBodyAndHeaders:
                            if (responseText == "NotInitialized")
                            {
                                responseText = await ReadContent(response);
                            }
                            result = responseText;                            
                            if (returnValueType == ReturnValueType.ResponseBodyAndHeaders)
                            {
                                result += ", " + response.ToString();
                            }
                            break;
                        case ReturnValueType.ResponseStatus:
                            result = response.StatusCode.ToString();
                            break;
                    }
                }
            }
            catch (XunitException)
            {
                if (response != null)
                {
                    TestUtility.LogInformation(response.ToString());
                }
                TestUtility.LogInformation(responseText);
                TestUtility.LogInformation(responseStatus);
            }
            return result;
        }
    }
    
}
