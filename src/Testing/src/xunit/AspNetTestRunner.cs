// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing
{
    internal class AspNetTestRunner : XunitTestRunner
    {
        private readonly TestOutputHelper _testOutputHelper;
        private readonly bool _ownsTestOutputHelper;

        public AspNetTestRunner(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            string skipReason,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            // Prioritize using ITestOutputHelper from constructor.
            if (ConstructorArguments != null)
            {
                foreach (var obj in ConstructorArguments)
                {
                    _testOutputHelper = obj as TestOutputHelper;
                    if (_testOutputHelper != null)
                    {
                        break;
                    }
                }
            }

            // No ITestOutputHelper in constructor so we'll create it ourselves.
            if (_testOutputHelper == null)
            {
                _testOutputHelper = new TestOutputHelper();
                _ownsTestOutputHelper = true;
            }
        }

        protected override async Task<Tuple<decimal, string>> InvokeTestAsync(ExceptionAggregator aggregator)
        {
            if (_ownsTestOutputHelper)
            {
                _testOutputHelper.Initialize(MessageBus, Test);
            }

            var result = await base.InvokeTestAsync(aggregator);

            if (_ownsTestOutputHelper)
            {
                // Update result with output if we created our own ITestOutputHelper.
                // The string returned from this method is what VS displays as the test output.
                result = new Tuple<decimal, string>(result.Item1, _testOutputHelper.Output);
                _testOutputHelper.Uninitialize();
            }

            return result;
        }

        protected override async Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            var repeatAttribute = GetRepeatAttribute(TestMethod);
            if (repeatAttribute == null)
            {
                return await InvokeTestMethodCoreAsync(aggregator);
            }

            var repeatContext = new RepeatContext(repeatAttribute.RunCount);
            RepeatContext.Current = repeatContext;

            var timeTaken = 0.0M;
            for (repeatContext.CurrentIteration = 0; repeatContext.CurrentIteration < repeatContext.Limit; repeatContext.CurrentIteration++)
            {
                timeTaken = await InvokeTestMethodCoreAsync(aggregator);
                if (aggregator.HasExceptions)
                {
                    return timeTaken;
                }
            }

            return timeTaken;
        }

        private Task<decimal> InvokeTestMethodCoreAsync(ExceptionAggregator aggregator)
        {
            var invoker = new AspNetTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource, _testOutputHelper);
            return invoker.RunAsync();
        }

        private RepeatAttribute GetRepeatAttribute(MethodInfo methodInfo)
        {
            var attributeCandidate = methodInfo.GetCustomAttribute<RepeatAttribute>();
            if (attributeCandidate != null)
            {
                return attributeCandidate;
            }

            attributeCandidate = methodInfo.DeclaringType.GetCustomAttribute<RepeatAttribute>();
            if (attributeCandidate != null)
            {
                return attributeCandidate;
            }

            return methodInfo.DeclaringType.Assembly.GetCustomAttribute<RepeatAttribute>();
        }
    }
}
