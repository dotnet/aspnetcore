// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.Extensions.Logging.Testing.Tests
{
    [RetryTest(nameof(RetryAllPredicate), "sample reason")]
    public class LoggedTestXunitRetryTests : LoggedTest
    {
        [Fact]
        public void CompletesWithoutRetryOnSuccess()
        {
            Assert.Equal(2, RetryContext.Limit);

            // This assert would fail on the second run
            Assert.Equal(0, RetryContext.CurrentIteration);
        }

        [Fact]
        public void RetriesUntilSuccess()
        {
            // This assert will fail the first time but pass on the second
            Assert.Equal(1, RetryContext.CurrentIteration);

            // This assert will ensure a message is logged for retried tests.
            Assert.Equal(1, TestSink.Writes.Count);
            var loggedMessage = TestSink.Writes.ToArray()[0];
            Assert.Equal(LogLevel.Warning, loggedMessage.LogLevel);
            Assert.Equal($"{nameof(RetriesUntilSuccess)} failed and retry conditions are met, re-executing. The reason for failure is sample reason.", loggedMessage.Message);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [RetryTest(nameof(RetryAllPredicate), "sample reason", OperatingSystems.Windows, 3)]
        public void RetryCountNotOverridenWhenOSDoesNotMatch()
        {
            Assert.Equal(2, RetryContext.Limit);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [RetryTest(nameof(RetryAllPredicate), "sample reason", OperatingSystems.Windows, 3)]
        public void RetryCountOverridenWhenOSMatches()
        {
            Assert.Equal(3, RetryContext.Limit);
        }

        [Fact]
        [RetryTest(nameof(RetryInvalidOperationExceptionPredicate), "sample reason")]
        public void RetryIfPredicateIsTrue()
        {
            if (RetryContext.CurrentIteration == 0)
            {
                Logger.LogWarning("Throw on first iteration");
                throw new Exception();
            }

            // This assert will ensure a message is logged for retried tests.
            Assert.Equal(1, TestSink.Writes.Count);
            var loggedMessage = TestSink.Writes.ToArray()[0];
            Assert.Equal(LogLevel.Warning, loggedMessage.LogLevel);
            Assert.Equal($"{nameof(RetryIfPredicateIsTrue)} failed and retry conditions are met, re-executing. The reason for failure is sample reason.", loggedMessage.Message);
        }

        // Static predicates are valid
        private static bool RetryAllPredicate(Exception e)
            => true;

        // Instance predicates are valid
        private bool RetryInvalidOperationExceptionPredicate(Exception e)
            => TestSink.Writes.Any(m => m.Message.Contains("Throw on first iteration"));
    }

    [RetryTest(nameof(RetryAllPredicate), "sample reason")]
    public class LoggedTestXunitRetryConstructorTest : LoggedTest
    {
        private static int _constructorInvocationCount;

        public LoggedTestXunitRetryConstructorTest()
        {
            _constructorInvocationCount++;
        }

        [Fact]
        public void RetriesUntilSuccess()
        {
            // The constructor is invoked before the test method but the current iteration is updated after
            Assert.Equal(_constructorInvocationCount, RetryContext.CurrentIteration + 1);

            // This assert will fail the first time but pass on the second
            Assert.Equal(1, RetryContext.CurrentIteration);
        }

        private static bool RetryAllPredicate(Exception e)
            => true;
    }
}
