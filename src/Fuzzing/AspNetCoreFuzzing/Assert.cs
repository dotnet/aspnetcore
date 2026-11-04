// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace AspNetCoreFuzzing;

internal static class Assert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Throw(expected, actual);
        }

        static void Throw(T expected, T actual) =>
            throw new Exception($"Expected={expected} Actual={actual}");
    }

    public static void True([DoesNotReturnIf(false)] bool actual) =>
        Equal(true, actual);

    public static void False([DoesNotReturnIf(true)] bool actual) =>
        Equal(false, actual);

    public static void NotNull<T>(T value)
    {
        if (value is null)
        {
            ThrowNull();
        }

        static void ThrowNull() =>
            throw new Exception("Value is null");
    }

    public static void SequenceEqual<T>(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual)
    {
        if (!expected.SequenceEqual(actual))
        {
            Throw(expected, actual);
        }

        static void Throw(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual)
        {
            Equal(expected.Length, actual.Length);

            int diffIndex = expected.CommonPrefixLength(actual);

            throw new Exception($"Expected={expected[diffIndex]} Actual={actual[diffIndex]} at index {diffIndex}");
        }
    }

    public static TException Throws<TException, TState>(Action<TState> action, TState state)
        where TException : Exception
        where TState : allows ref struct
    {
        try
        {
            action(state);
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new Exception($"Expected exception of type {typeof(TException).Name} but got {ex.GetType().Name}");
        }

        throw new Exception($"Expected exception of type {typeof(TException).Name} but no exception was thrown");
    }
}
