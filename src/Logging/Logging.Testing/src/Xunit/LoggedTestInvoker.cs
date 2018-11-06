// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedTestInvoker : XunitTestInvoker
    {
        private readonly ITestOutputHelper _output;
        private readonly RetryContext _retryContext;

        public LoggedTestInvoker(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            ITestOutputHelper output,
            RetryContext retryContext)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            _output = output;
            _retryContext = retryContext;
        }

        protected override object CreateTestClass()
        {
            var testClass = base.CreateTestClass();

            (testClass as ILoggedTest).Initialize(
                TestMethod,
                TestMethodArguments,
                _output ?? ConstructorArguments.SingleOrDefault(a => typeof(ITestOutputHelper).IsAssignableFrom(a.GetType())) as ITestOutputHelper);

            if (testClass is LoggedTestBase loggedTestBase)
            {
                // Used for testing
                loggedTestBase.RetryContext = _retryContext;

                if (_retryContext != null)
                {
                    // Log retry attempt as warning
                    if (_retryContext.CurrentIteration > 0)
                    {
                        loggedTestBase.Logger.LogWarning($"{TestMethod.Name} failed and retry conditions are met, re-executing. The reason for failure is {_retryContext.Reason}.");
                    }

                    // Save the test class instance for non-static predicates
                    _retryContext.TestClassInstance = testClass;
                }
            }

            return testClass;
        }
    }
}
