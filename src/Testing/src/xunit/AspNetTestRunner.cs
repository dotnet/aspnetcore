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
            var invoker = new AspNetTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource);
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
