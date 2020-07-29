using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using ProcessUtilities;

namespace UnitTestingDiagnostics
{
    [DataCollectorFriendlyName("execution")]
    [DataCollectorTypeUri("datacollector://Microsoft/TestPlatform/Extensions/execution/v1")]
    public class ExecutionCollector : DataCollector
    {
        public override void Initialize(XmlElement configurationElement, DataCollectionEvents events, DataCollectionSink dataSink, DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            var map = new ConcurrentDictionary<Guid, TestResult>();
            var timeout = TimeSpan.FromSeconds(20);
            var testHostProcessId = 0;
            Timer inactivityTimer = null;

            async void CaptureTestState(object state)
            {
                inactivityTimer.Change(-1, -1);
                inactivityTimer = null;

                Process process = null;

                try
                {
                    process = Process.GetProcessById(testHostProcessId);
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist..
                }
                catch (InvalidOperationException)
                {
                    // Process doesn't exist..
                }

                // Incomplete tests
                var incompleteTests = string.Join(Environment.NewLine, map.Values.Where(r => !r.IsCompleted).Select(r => "    - " + r.TestName));
                logger.LogWarning(environmentContext.SessionDataCollectionContext, "Incomplete Tests: \r\n" + incompleteTests);

                try
                {
                    // Attempt to capture a dump if the process hasn't crashed
                    if (process != null)
                    {
                        logger.LogWarning(environmentContext.SessionDataCollectionContext, $"Capturing dump the test run is hanging. It's been > {timeout} since the last set of test activity");

                        var dumpFilePath = Path.Combine(Path.GetTempPath(), $"dotnet.{testHostProcessId}.dmp");
                        await ProcessUtil.CaptureDumpAsync(testHostProcessId, dumpFilePath);
                        dataSink.SendFileAsync(environmentContext.SessionDataCollectionContext, dumpFilePath, true);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(environmentContext.SessionDataCollectionContext, ex);
                }
                finally
                {
                    if (process != null)
                    {
                        logger.LogWarning(environmentContext.SessionDataCollectionContext, $"Killing the test host process {testHostProcessId}");
                        process.Kill();
                    }
                }
            }

            void ResetTimer()
            {
                inactivityTimer?.Change(timeout, Timeout.InfiniteTimeSpan);
            }

            events.SessionEnd += (sender, e) =>
            {
                inactivityTimer?.Dispose();
                inactivityTimer = null;
            };

            events.TestHostLaunched += (sender, e) =>
            {
                inactivityTimer = new Timer(CaptureTestState, null, timeout, Timeout.InfiniteTimeSpan);

                testHostProcessId = e.TestHostProcessId;
            };

            events.TestCaseStart += (sender, e) =>
            {
                ResetTimer();

                map[e.TestCaseId] = new TestResult
                {
                    TestName = e.TestCaseName
                };

                logger.LogWarning(environmentContext.SessionDataCollectionContext, e.TestCaseName + " [Start]");
            };

            events.TestCaseEnd += (sender, e) =>
            {
                ResetTimer();

                var result = map[e.TestCaseId];

                result.IsCompleted = true;
                result.TestOutcome = e.TestOutcome;

                logger.LogWarning(environmentContext.SessionDataCollectionContext, e.TestCaseName + " [Ended]");
            };
        }

        private class TestResult
        {
            public string TestName { get; set; }
            public TestOutcome TestOutcome { get; set; }
            public bool IsCompleted { get; set; }
        }
    }
}
