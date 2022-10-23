#region Copyright notice and license

// Copyright 2015-2016 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Collections;

namespace InteropTestsClient;

internal static class Assert
{
    public static void IsTrue(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Expected true but got false.");
        }
    }

    public static void IsFalse(bool condition)
    {
        if (condition)
        {
            throw new InvalidOperationException("Expected false but got true.");
        }
    }

    public static void AreEqual(object expected, object actual)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
        }
    }

    public static void IsNotNull(object value)
    {
        if (value == null)
        {
            throw new InvalidOperationException("Expected not null but got null.");
        }
    }

    public static void Fail()
    {
        throw new InvalidOperationException("Failure assert.");
    }

    public static async Task<TException> ThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            if (ex.GetType() == typeof(TException))
            {
                return (TException)ex;
            }

            throw new InvalidOperationException($"Expected ${typeof(TException)} but got ${ex.GetType()}.");
        }

        throw new InvalidOperationException("No exception thrown.");
    }

    public static TException Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            if (ex.GetType() == typeof(TException))
            {
                return (TException)ex;
            }

            throw new InvalidOperationException($"Expected ${typeof(TException)} but got ${ex.GetType()}.");
        }

        throw new InvalidOperationException("No exception thrown.");
    }

    public static void Contains(object expected, ICollection actual)
    {
        foreach (var item in actual)
        {
            if (Equals(item, expected))
            {
                return;
            }
        }

        throw new InvalidOperationException($"Could not find {expected} in the collection.");
    }
}

internal static class CollectionAssert
{
    public static void AreEqual(IList expected, IList actual)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException($"Collection lengths differ. {expected.Count} but got {actual.Count}.");
        }

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.AreEqual(expected[i]!, actual[i]!);
        }
    }
}
