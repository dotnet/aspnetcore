// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests.Internal
{
    public class ReflectionHelperTests
    {
        [Theory]
        [MemberData(nameof(GetPersonFromDataGenerator))]
        public void ReflectionHelperTest(Type type, bool expectedOutcome)
        {
            Assert.Equal(expectedOutcome, ReflectionHelper.IsIAsyncEnumerable(type));
        }

        public static IEnumerable<object[]> GetPersonFromDataGenerator()
        {
            yield return new object[]
            {
                typeof(IAsyncEnumerable<object>),
                true
            };

            yield return new object[]
            {
                typeof(ChannelReader<object>),
                false
            };

            async IAsyncEnumerable<int> Stream()
            {
                await Task.Delay(10);
                yield return 1;
            }

            object streamAsObject = Stream();
            yield return new object[]
            {
                streamAsObject.GetType(),
                true
            };

            yield return new object[]
            {
                typeof(string),
                false
            };
        }
    }
}
