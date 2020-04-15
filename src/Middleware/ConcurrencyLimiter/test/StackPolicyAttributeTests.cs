// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests
{
    public class StackPolicyAttributeTests
    {

        [Fact]
        public static void BaseFunctionality()
        {
            var stack = TestUtils.CreateStackPolicyAttribute(0, 2);

            var task1 = stack.TryEnterAsync();

            Assert.False(task1.IsCompleted);

            stack.OnExit();

            Assert.True(task1.IsCompleted && task1.Result);
        }

        [Fact]
        public static void OldestRequestOverwritten()
        {
            var stack = TestUtils.CreateStackPolicyAttribute(0, 3);

            var task1 = stack.TryEnterAsync();
            Assert.False(task1.IsCompleted);
            var task2 = stack.TryEnterAsync();
            Assert.False(task2.IsCompleted);
            var task3 = stack.TryEnterAsync();
            Assert.False(task3.IsCompleted);

            var task4 = stack.TryEnterAsync();

            Assert.True(task1.IsCompleted);
            Assert.False(task1.Result);

            Assert.False(task2.IsCompleted);
            Assert.False(task3.IsCompleted);
            Assert.False(task4.IsCompleted);
        }

        [Fact]
        public static void RespectsMaxConcurrency()
        {
            var stack = TestUtils.CreateStackPolicyAttribute(2, 2);

            var task1 = stack.TryEnterAsync();
            Assert.True(task1.IsCompleted);

            var task2 = stack.TryEnterAsync();
            Assert.True(task2.IsCompleted);

            var task3 = stack.TryEnterAsync();
            Assert.False(task3.IsCompleted);
        }

        [Fact]
        public static void ExitRequestsPreserveSemaphoreState()
        {
            var stack = TestUtils.CreateStackPolicyAttribute(1, 2);
            var task1 = stack.TryEnterAsync();
            Assert.True(task1.IsCompleted && task1.Result);

            var task2 = stack.TryEnterAsync();
            Assert.False(task2.IsCompleted);

            stack.OnExit();  // t1 exits, should free t2 to return
            Assert.True(task2.IsCompleted && task2.Result);

            stack.OnExit();  // t2 exists, there's now a free spot in server

            var task3 = stack.TryEnterAsync();
            Assert.True(task3.IsCompleted && task3.Result);
        }

        [Fact]
        public static void StaleRequestsAreProperlyOverwritten()
        {
            var stack = TestUtils.CreateStackPolicyAttribute(0, 4);

            var task1 = stack.TryEnterAsync();
            stack.OnExit();
            Assert.True(task1.IsCompleted);

            var task2 = stack.TryEnterAsync();
            stack.OnExit();
            Assert.True(task2.IsCompleted);
        }

        [Fact]
        public static async Task OneTryEnterAsyncOneOnExit()
        {
            var stack = TestUtils.CreateStackPolicyAttribute(1, 4);

            Assert.Throws<InvalidOperationException>(() => stack.OnExit());

            await stack.TryEnterAsync();

            stack.OnExit();

            Assert.Throws<InvalidOperationException>(() => stack.OnExit());
        }
    }
}
