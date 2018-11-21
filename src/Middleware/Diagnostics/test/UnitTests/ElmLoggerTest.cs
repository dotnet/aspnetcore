// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Diagnostics.Elm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Tests
{
    public class ElmLoggerTest
    {
        private const string _name = "test";
        private const string _state = "This is a test";
        private static Func<string, LogLevel, bool> _filter = (_, __) => true;

        private static Tuple<ElmLogger, ElmStore> SetUp(Func<string, LogLevel, bool> filter = null, string name = null)
        {
            // Arrange
            var store = new ElmStore();
            var options = new ElmOptions() { Filter = filter ?? _filter };
            var provider = new ElmLoggerProvider(store, Options.Create(options));
            var logger = (ElmLogger)provider.CreateLogger(name ?? _name);

            return new Tuple<ElmLogger, ElmStore>(logger, store);
        }

        [Fact]
        public void LogsWhenNullFormatterGiven()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Item1;
            var store = t.Item2;

            // Act
            logger.Log(LogLevel.Information, 0, _state, null, null);

            // Assert
            Assert.Single(store.GetActivities());
        }

        [Fact]
        public void DoesNotLogWithEmptyStateAndException()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Item1;
            var store = t.Item2;

            // Act
            logger.Log<object>(LogLevel.Information, 0, null, null, null);

            // Assert
            Assert.Empty(store.GetActivities());
        }

        [Fact]
        public void DefaultLogsForAllLogLevels()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Item1;
            var store = t.Item2;

            // Act
            logger.Log(LogLevel.Trace, 0, _state, null, null);
            logger.Log(LogLevel.Debug, 0, _state, null, null);
            logger.Log(LogLevel.Information, 0, _state, null, null);
            logger.Log(LogLevel.Warning, 0, _state, null, null);
            logger.Log(LogLevel.Error, 0, _state, null, null);
            logger.Log(LogLevel.Critical, 0, _state, null, null);

            // Assert
            Assert.Equal(6, (store.GetActivities().SelectMany(a => NodeLogs(a.Root, new List<LogInfo>()))).ToList().Count);
        }

        [Theory]
        [InlineData(LogLevel.Warning, "", 3)]
        [InlineData(LogLevel.Warning, "te", 3)]
        [InlineData(LogLevel.Warning, "bad", 0)]
        [InlineData(LogLevel.Critical, "", 1)]
        [InlineData(LogLevel.Critical, "test", 1)]
        [InlineData(LogLevel.Trace, "t", 6)]
        public void Filter_LogsWhenAppropriate(LogLevel minLevel, string prefix, int count)
        {
            // Arrange
            var t = SetUp((name, level) => (name.StartsWith(prefix, StringComparison.Ordinal) && level >= minLevel), _name);
            var logger = t.Item1;
            var store = t.Item2;

            // Act
            logger.Log(LogLevel.Trace, 0, _state, null, null);
            logger.Log(LogLevel.Debug, 0, _state, null, null);
            logger.Log(LogLevel.Information, 0, _state, null, null);
            logger.Log(LogLevel.Warning, 0, _state, null, null);
            logger.Log(LogLevel.Error, 0, _state, null, null);
            logger.Log(LogLevel.Critical, 0, _state, null, null);

            // Assert
            Assert.Equal(count, (store.GetActivities().SelectMany(a => NodeLogs(a.Root, new List<LogInfo>()))).ToList().Count);
        }

        [Fact]
        public void CountReturnsCorrectNumber()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Item1;
            var store = t.Item2;

            // Act
            using (logger.BeginScope("test14"))
            {
                for (var i = 0; i < 25; i++)
                {
                    logger.LogWarning("hello world");
                }
                using (logger.BeginScope("test15"))
                {
                    for (var i = 0; i < 25; i++)
                    {
                        logger.LogCritical("goodbye world");
                    }
                }
            }

            // Assert
            Assert.Equal(50, store.Count());
        }

        [Fact]
        public void ThreadsHaveSeparateActivityContexts()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Item1;
            var store = t.Item2;

            var testThread = new TestThread(logger);
            Thread workerThread = new Thread(testThread.Work);

            // Act
            workerThread.Start();
            using (logger.BeginScope("test1"))
            {
                logger.LogWarning("hello world");
                Thread.Sleep(1000);
                logger.LogCritical("goodbye world");
            }
            workerThread.Join();

            // Assert
            Assert.Equal(17, (store.GetActivities().SelectMany(a => NodeLogs(a.Root, new List<LogInfo>()))).ToList().Count);
            Assert.Equal(2, store.GetActivities().ToList().Count);
        }

        [Fact]
        public void ScopesHaveProperTreeStructure()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Item1;
            var store = t.Item2;

            var testThread = new TestThread(logger);
            Thread workerThread = new Thread(testThread.Work);

            // Act
            workerThread.Start();
            using (logger.BeginScope("test2"))
            {
                logger.LogWarning("hello world");
                Thread.Sleep(1000);
                logger.LogCritical("goodbye world");
            }
            workerThread.Join();

            // Assert
            // get the root of the activity for scope "test2"
            var root1 = (store.GetActivities())
                .Where(a => string.Equals(a.Root.State?.ToString(), "test2"))?
                .FirstOrDefault()?
                .Root;
            Assert.NotNull(root1);
            var root2 = (store.GetActivities())
                .Where(a => string.Equals(a.Root.State?.ToString(), "test12"))?
                .FirstOrDefault()?
                .Root;
            Assert.NotNull(root2);

            Assert.Empty(root1.Children);
            Assert.Equal(2, root1.Messages.Count);
            Assert.Single(root2.Children);
            Assert.Equal(12, root2.Messages.Count);
            Assert.Empty(root2.Children.First().Children);
            Assert.Equal(3, root2.Children.First().Messages.Count);
        }

        [Fact]
        public void CollapseTree_CollapsesWhenNoLogsInSingleScope()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Item1;
            var store = t.Item2;

            // Act
            using (logger.BeginScope("test3"))
            {
            }

            // Assert
            Assert.Empty(store.GetActivities());
        }

        [Fact]
        public void CollapseTree_CollapsesWhenNoLogsInNestedScope()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Item1;
            var store = t.Item2;

            // Act
            using (logger.BeginScope("test4"))
            {
                using (logger.BeginScope("test5"))
                {
                }
            }

            // Assert
            Assert.Empty(store.GetActivities());
        }

        [Fact]
        public void CollapseTree_DoesNotCollapseWhenLogsExist()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Item1;
            var store = t.Item2;

            // Act
            using (logger.BeginScope("test6"))
            {
                using (logger.BeginScope("test7"))
                {
                    logger.LogTrace("hi");
                }
            }

            // Assert
            Assert.Single(store.GetActivities());
        }

        [Fact]
        public void CollapseTree_CollapsesAppropriateNodes()
        {
            // Arrange
            var t = SetUp();
            var logger = t.Item1;
            var store = t.Item2;

            // Act
            using (logger.BeginScope("test8"))
            {
                logger.LogDebug("hi");
                using (logger.BeginScope("test9"))
                {
                }
            }

            // Assert
            Assert.Single(store.GetActivities());
            var context = store.GetActivities()
                .Where(a => string.Equals(a.Root.State?.ToString(), "test8"))
                .First();
            Assert.Empty(context.Root.Children);
        }

        [Fact]
        public void CollapseTree_WorksWithFilter()
        {
            // Arrange
            var t = SetUp((_, level) => level >= LogLevel.Warning, null);
            var logger = t.Item1;
            var store = t.Item2;

            // Act
            using (logger.BeginScope("test10"))
            {
                using (logger.BeginScope("test11"))
                {
                    logger.LogInformation("hi");
                }
            }

            // Assert
            Assert.Empty(store.GetActivities());
        }

        private List<LogInfo> NodeLogs(ScopeNode node, List<LogInfo> logs)
        {
            if (node != null)
            {
                logs.AddRange(node.Messages);
                foreach (var child in node.Children)
                {
                    NodeLogs(child, logs);
                }
            }
            return logs;
        }

        private class TestThread
        {
            private ILogger _logger;

            public TestThread(ILogger logger)
            {
                _logger = logger;
            }

            public void Work()
            {
                using (_logger.BeginScope("test12"))
                {
                    for (var i = 0; i < 5; i++)
                    {
                        _logger.LogDebug(string.Format("xxx {0}", i));
                        Thread.Sleep(5);
                    }
                    using (_logger.BeginScope("test13"))
                    {
                        for (var i = 0; i < 3; i++)
                        {
                            _logger.LogDebug(string.Format("yyy {0}", i));
                            Thread.Sleep(200);
                        }
                    }
                    for (var i = 0; i < 7; i++)
                    {
                        _logger.LogDebug(string.Format("zzz {0}", i));
                        Thread.Sleep(40);
                    }
                }
            }
        }
    }
}