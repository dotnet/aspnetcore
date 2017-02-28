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

namespace AspNetCoreModule.Test
{
    public class FunctionalTestHelper
    {
        private const int _repeatCount = 3;

        public enum ReturnValueType
        {
            ResponseBody,
            ResponseBodyAndHeaders,
            ResponseStatus,
            None
        }

        public static async Task DoBasicTest(ServerType serverType, IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoBasicTest", serverType))
            {
                string backendProcessId_old = null;

                DateTime startTime = DateTime.Now;

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
                }
            }
        }

        public static async Task DoRecycleApplicationAfterWebConfigUpdated(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoRecycleApplicationAfterWebConfigUpdated"))
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
                    Thread.Sleep(500);

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

        public static async Task DoEnvironmentVariablesTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoEnvironmentVariablesTest"))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
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
                    Assert.True(expectedValue.ToString() == (await GetResponse(testSite.AspNetCoreApp.GetUri("GetEnvironmentVariables"), HttpStatusCode.OK)));
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "environmentVariable", new string[] { "ANCMTestBar", "bar" });
                    Thread.Sleep(500);

                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    expectedValue++;
                    Assert.True("foo" == (await GetResponse(testSite.AspNetCoreApp.GetUri("ExpandEnvironmentVariablesANCMTestFoo"), HttpStatusCode.OK)));
                    Assert.True("bar" == (await GetResponse(testSite.AspNetCoreApp.GetUri("ExpandEnvironmentVariablesANCMTestBar"), HttpStatusCode.OK)));
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
                testSite.AspNetCoreApp.CreateFile(new string[] { fileContent }, "App_Offline.Htm");

                for (int i = 0; i < _repeatCount; i++)
                {
                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(1100);

                    // verify 503 
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), fileContent + "\r\n", HttpStatusCode.ServiceUnavailable);

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
                testSite.AspNetCoreApp.CreateFile(new string[] { fileContent }, "App_Offline.Htm");

                for (int i = 0; i < _repeatCount; i++)
                {
                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    // verify 503 
                    string urlForUrlRewrite = testSite.URLRewriteApp.URL + "/Rewrite2/" + testSite.AspNetCoreApp.URL + "/GetProcessId";
                    await VerifyResponseBody(testSite.RootAppContext.GetUri(urlForUrlRewrite), fileContent + "\r\n", HttpStatusCode.ServiceUnavailable);

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

                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
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
                    Assert.True(responseBody.Contains("808681"));

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
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
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
                    Assert.True(rapidFailsTriggered, "Verify 503 error");

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
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    DateTime startTime = DateTime.Now;

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
                    Assert.Equal(1, processIDs.Count);
                }

                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoStartupTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness, int startupTimeLimit)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoStartupTimeLimitTest"))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
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
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "requestTimeout", TimeSpan.Parse(requestTimeout)); 
                    Thread.Sleep(500);

                    if (requestTimeout.ToString() == "00:02:00")
                    {
                        await VerifyResponseBody(testSite.AspNetCoreApp.GetUri("DoSleep65000"), "Running", HttpStatusCode.OK, timeout:70);                        
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

        public static async Task DoShutdownTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfshutdownTimeLimit, int expectedClosingTime)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoShutdownTimeLimitTest"))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    // Set new value (10 second) to make the backend process get the Ctrl-C signal and measure when the recycle happens
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "shutdownTimeLimit", valueOfshutdownTimeLimit);
                    iisConfig.SetANCMConfig(
                        testSite.SiteName, 
                        testSite.AspNetCoreApp.Name, 
                        "environmentVariable", 
                        new string[] { "ANCMTestShutdownDelay", "20000" }
                        );

                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);
                    string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));

                    // Set a new value such as 100 to make the backend process being recycled
                    DateTime startTime = DateTime.Now;
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "shutdownTimeLimit", 100);
                    backendProcess.WaitForExit(30000);
                    DateTime endTime = DateTime.Now;
                    var difference = endTime - startTime;
                    Assert.True(difference.Seconds >= expectedClosingTime);
                    Assert.True(difference.Seconds < expectedClosingTime + 3);
                    Assert.True(backendProcessId != await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK));
                    await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);
                }

                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }
        public static async Task DoStdoutLogEnabledTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoStdoutLogEnabledTest"))
            {
                testSite.AspNetCoreApp.DeleteDirectory("logs");

                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);
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
            using (var testSite = new TestWebSite(appPoolBitness, "DoProcessPathAndArgumentsTest"))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
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
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    string result = string.Empty;
                    iisConfig.SetANCMConfig(testSite.SiteName, testSite.AspNetCoreApp.Name, "forwardWindowsAuthToken", enabledForwardWindowsAuthToken);
                    string requestHeaders = await GetResponse(testSite.AspNetCoreApp.GetUri("DumpRequestHeaders"), HttpStatusCode.OK);
                    Assert.False(requestHeaders.ToUpper().Contains("MS-ASPNETCORE-WINAUTHTOKEN"));

                    iisConfig.EnableWindowsAuthentication(testSite.SiteName);

                    Thread.Sleep(500);

                    // check JitDebugger before continuing 
                    TestUtility.ResetHelper(ResetHelperMode.KillVSJitDebugger);
                    Thread.Sleep(500);

                    requestHeaders = await GetResponse(testSite.AspNetCoreApp.GetUri("DumpRequestHeaders"), HttpStatusCode.OK);
                    if (enabledForwardWindowsAuthToken)
                    {
                        string expectedHeaderName = "MS-ASPNETCORE-WINAUTHTOKEN";
                        Assert.True(requestHeaders.ToUpper().Contains(expectedHeaderName));

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
                        Assert.False(requestHeaders.ToUpper().Contains("MS-ASPNETCORE-WINAUTHTOKEN"));

                        result = await GetResponse(testSite.AspNetCoreApp.GetUri("ImpersonateMiddleware"), HttpStatusCode.OK);
                        Assert.True(result.Contains("ImpersonateMiddleware-UserName = NoAuthentication"));
                    }
                }

                testSite.AspNetCoreApp.RestoreFile("web.config");
            }
        }

        public static async Task DoRecylingAppPoolTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoRecylingAppPoolTest"))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
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
                    Assert.True(x == y && foundVSJit == false, "worker process is not recycled after 30 seconds");

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
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
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
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
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

                    result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri("foo.htm"), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                    string headerValue = GetHeaderValue(result, "MyCustomHeader");
                    Assert.True(result.Contains("foohtm"), "verify response body");
                    Assert.Equal("gzip", GetHeaderValue(result, "Content-Encoding"));
                    Thread.Sleep(2000);

                    result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri("foo.htm"), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.OK);
                    string headerValue2 = GetHeaderValue(result, "MyCustomHeader");
                    Assert.True(result.Contains("foohtm"), "verify response body");
                    Assert.Equal("gzip", GetHeaderValue(result, "Content-Encoding"));
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
            using (var testSite = new TestWebSite(appPoolBitness, "DoSendHTTPSRequestTest"))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    string hostName = "";
                    string subjectName = "localhost";
                    string ipAddress = "*";
                    string hexIPAddress = "0x00";
                    int sslPort = 46300;

                    // Add https binding and get https uri information
                    iisConfig.AddBindingToSite(testSite.SiteName, ipAddress, sslPort, hostName, "https");
                    
                    // Create a self signed certificate
                    string thumbPrint = iisConfig.CreateSelfSignedCertificate(subjectName);

                    // Export the self signed certificate to rootCA
                    iisConfig.ExportCertificateTo(thumbPrint, sslStoreTo:@"Cert:\LocalMachine\Root");

                    // Configure http.sys ssl certificate mapping to IP:Port endpoint with the newly created self signed certificage
                    iisConfig.SetSSLCertificate(sslPort, hexIPAddress, thumbPrint);

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

        public static async Task DoClientCertificateMappingTest(IISConfigUtility.AppPoolBitness appPoolBitness, bool useHTTPSMiddleWare)
        {
            using (var testSite = new TestWebSite(appPoolBitness, "DoClientCertificateMappingTest"))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    string hostName = "";
                    string rootCN = "ANCMTest" + testSite.PostFix;
                    string webServerCN = "localhost";
                    string kestrelServerCN = "localhost";
                    string clientCN = "ANCMClient-" + testSite.PostFix;

                    string ipAddress = "*";
                    string hexIPAddress = "0x00";
                    int sslPort = 46300;

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
                    iisConfig.EnableOneToOneClientCertificateMapping(testSite.SiteName, ".\\" + userName, password, publicKey);

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

                    // Verify http request with using client certificate
                    Uri targetHttpsUri = testSite.AspNetCoreApp.GetUri(null, sslPort, protocol: "https");
                    string statusCode = TestUtility.RunPowershellScript("( invoke-webrequest " + targetHttpsUri.OriginalString + " -CertificateThumbprint " + thumbPrintForClientAuthentication + ").StatusCode");                    
                    Assert.Equal("200", statusCode);

                    // Verify https request with client certificate includes the certificate header "MS-ASPNETCORE-CLIENTCERT"
                    Uri targetHttpsUriForDumpRequestHeaders = testSite.AspNetCoreApp.GetUri("DumpRequestHeaders", sslPort, protocol: "https");
                    string outputRawContent = TestUtility.RunPowershellScript("( invoke-webrequest " + targetHttpsUriForDumpRequestHeaders.OriginalString + " -CertificateThumbprint " + thumbPrintForClientAuthentication + ").RawContent.ToString()");
                    string expectedHeaderName = "MS-ASPNETCORE-CLIENTCERT";
                    Assert.True(outputRawContent.Contains(expectedHeaderName));

                    // Get the value of MS-ASPNETCORE-CLIENTCERT request header again and verify it is matched to its configured public key
                    Uri targetHttpsUriForCLIENTCERTRequestHeader = testSite.AspNetCoreApp.GetUri("GetRequestHeaderValueMS-ASPNETCORE-CLIENTCERT", sslPort, protocol: "https");
                    outputRawContent = TestUtility.RunPowershellScript("( invoke-webrequest " + targetHttpsUriForCLIENTCERTRequestHeader.OriginalString + " -CertificateThumbprint " + thumbPrintForClientAuthentication + ").RawContent.ToString()");
                    Assert.True(outputRawContent.Contains(publicKey));

                    // Verify non-https request returns 403.4 error
                    string result = string.Empty;
                    result = await GetResponseAndHeaders(testSite.AspNetCoreApp.GetUri(), new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.Forbidden);
                    Assert.True(result.Contains("403.4"));

                    // Verify https request without using client certificate returns 403.7
                    result = await GetResponseAndHeaders(targetHttpsUri, new string[] { "Accept-Encoding", "gzip" }, HttpStatusCode.Forbidden);
                    Assert.True(result.Contains("403.7"));

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
                DateTime startTime = DateTime.Now;

                await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);

                // Get Process ID
                string backendProcessId = await GetResponse(testSite.AspNetCoreApp.GetUri("GetProcessId"), HttpStatusCode.OK);

                // Verify WebSocket without setting subprotocol
                await VerifyResponseBodyContain(testSite.WebSocketApp.GetUri("echo.aspx"), new string[] { "Socket Open" }, HttpStatusCode.OK); // echo.aspx has hard coded path for the websocket server

                // Verify WebSocket subprotocol
                await VerifyResponseBodyContain(testSite.WebSocketApp.GetUri("echoSubProtocol.aspx"), new string[] { "Socket Open", "mywebsocketsubprotocol" }, HttpStatusCode.OK); // echoSubProtocol.aspx has hard coded path for the websocket server

                // Verify process creation ANCM event log
                Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                // Verify websocket 
                using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                {
                    var frameReturned = websocketClient.Connect(testSite.AspNetCoreApp.GetUri("websocket"), true, true);
                    Assert.True(frameReturned.Content.Contains("Connection: Upgrade"));
                    Assert.True(frameReturned.Content.Contains("HTTP/1.1 101 Switching Protocols"));
                    Thread.Sleep(500);

                    VerifySendingWebSocketData(websocketClient, testData);

                    Thread.Sleep(500);
                    frameReturned = websocketClient.Close();
                    Assert.True(frameReturned.FrameType == FrameType.Close, "Closing Handshake");
                }

                // send a simple request again and verify the response body
                await VerifyResponseBody(testSite.AspNetCoreApp.GetUri(), "Running", HttpStatusCode.OK);
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
                if (DoVerifyDataSentAndReceived(websocketClient) == false)
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
            catch (XunitException)
            {
                TestUtility.LogInformation(response.ToString());
                TestUtility.LogInformation(responseText);
                throw;
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
                var outputStream = new MemoryStream();
                using (var gzip = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    await gzip.CopyToAsync(outputStream);
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
                                Assert.True(responseText.Contains(item));
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
                throw;
            }
            return result;
        }
    }
    
}
