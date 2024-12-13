// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests.Internal;

public class ReflectionHelperTests
{
    [Theory]
    [MemberData(nameof(TypesToCheck))]
    public void IsIAsyncEnumerableTests(Type type, bool expectedOutcome)
    {
        Assert.Equal(expectedOutcome, ReflectionHelper.IsIAsyncEnumerable(type));
    }

    public static IEnumerable<object[]> TypesToCheck()
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

        yield return new object[]
        {
                typeof(CustomAsyncEnumerable),
                true
        };

        yield return new object[]
        {
                typeof(CustomAsyncEnumerableOfT<object>),
                true
        };
    }

    private class CustomAsyncEnumerable : IAsyncEnumerable<object>
    {
        public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private class CustomAsyncEnumerableOfT<T> : IAsyncEnumerable<object>
    {
        public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
