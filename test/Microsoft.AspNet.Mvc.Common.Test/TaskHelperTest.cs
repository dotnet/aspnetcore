// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class TaskHelperTest
    {
        [Fact]
        public void WaitAndThrowIfFaulted_DoesNotThrowIfTaskIsNotFaulted()
        {
            // Arrange
            var task = Task.FromResult(0);

            // Act and Assert (does not throw)
            TaskHelper.WaitAndThrowIfFaulted(task);
        }

        [Fact]
        public void WaitAndThrowIfFaulted_ThrowsIfTaskIsFaulted()
        {
            // Arrange
            var message = "Exception message";
            var task = CreatingFailingTask(message);

            // Act and Assert
            var ex = Assert.Throws<Exception>(() => TaskHelper.WaitAndThrowIfFaulted(task));
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void WaitAndThrowIfFaulted_ThrowsFirstExceptionWhenAggregateTaskFails()
        {
            // Arrange
            var message = "Exception message";
            var task = Task.Run(async () =>
            {
                await Task.WhenAll(CreatingFailingTask(message),
                                   CreatingFailingTask("different message"));
            });

            // Act and Assert
            var ex = Assert.Throws<Exception>(() => TaskHelper.WaitAndThrowIfFaulted(task));
            Assert.Equal(message, ex.Message);
        }

        private static Task CreatingFailingTask(string message)
        {
            return Task.Run(() =>
            {
                throw new Exception(message);
            });
        }
    }
}