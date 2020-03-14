// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ReusableQueueTests
    {
        [Fact]
        public void TryEnqueueExisting_NeverUsed_ReturnFalse()
        {
            var queue = new ReusableQueue<string>();

            Assert.False(queue.TryEnqueueExisting(out _));
        }

        [Fact]
        public void TryEnqueueExisting_HasItemToReuse_ReturnTrue()
        {
            var queue = new ReusableQueue<string>();
            queue.Enqueue("1");

            queue.Dequeue();

            Assert.True(queue.TryEnqueueExisting(out var existing));
            Assert.Equal("1", existing);

            Assert.False(queue.TryEnqueueExisting(out _));
        }

        [Fact]
        public void TryEnqueueExisting_ArrayFull_ReturnFalse()
        {
            var queue = new ReusableQueue<string>();
            queue.Enqueue("1");

            queue.Dequeue();

            // Fill all 4 array slots
            queue.Enqueue("2");
            queue.Enqueue("3");
            queue.Enqueue("4");
            queue.Enqueue("5");

            // No existing left
            Assert.False(queue.TryEnqueueExisting(out _));
        }

        [Fact]
        public void TryEnqueueExisting_FullThenEmpty_ReturnTrue()
        {
            var queue = new ReusableQueue<string>();
            queue.Enqueue("1");
            queue.Enqueue("2");
            queue.Enqueue("3");
            queue.Enqueue("4");

            Assert.Equal("1", queue.Dequeue());
            Assert.Equal("2", queue.Dequeue());
            Assert.Equal("3", queue.Dequeue());
            Assert.Equal("4", queue.Dequeue());

            string s;
            Assert.True(queue.TryEnqueueExisting(out s));
            Assert.Equal("1", s);

            Assert.True(queue.TryEnqueueExisting(out s));
            Assert.Equal("2", s);

            Assert.True(queue.TryEnqueueExisting(out s));
            Assert.Equal("3", s);

            Assert.True(queue.TryEnqueueExisting(out s));
            Assert.Equal("4", s);

            Assert.False(queue.TryEnqueueExisting(out _));

            Assert.Equal("1", queue.Dequeue());
            Assert.Equal("2", queue.Dequeue());
            Assert.Equal("3", queue.Dequeue());
            Assert.Equal("4", queue.Dequeue());
        }

        [Fact]
        public void TryEnqueueExisting_ArrayResize_ReturnFalse()
        {
            var queue = new ReusableQueue<string>();
            queue.Enqueue("1");
            queue.Enqueue("2");
            queue.Enqueue("3");
            queue.Enqueue("4");

            Assert.Equal("1", queue.Dequeue());

            queue.Enqueue("5");
            queue.Enqueue("6");

            // Because the array was resized there are no existing items
            Assert.False(queue.TryEnqueueExisting(out _));

            Assert.Equal("2", queue.Dequeue());
            Assert.Equal("3", queue.Dequeue());
            Assert.Equal("4", queue.Dequeue());
            Assert.Equal("5", queue.Dequeue());
            Assert.Equal("6", queue.Dequeue());
        }
    }
}
