// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

// TODO: eventually want: public partial class Assert : Xunit.Assert
public static class ExceptionAssert
{
    /// <summary>
    /// Verifies that an exception of the given type (or optionally a derived type) is thrown.
    /// </summary>
    /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static TException Throws<TException>(Action testCode)
        where TException : Exception
    {
        return VerifyException<TException>(RecordException(testCode));
    }

    /// <summary>
    /// Verifies that an exception of the given type is thrown.
    /// Also verifies that the exception message matches.
    /// </summary>
    /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="exceptionMessage">The exception message to verify</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static TException Throws<TException>(Action testCode, string exceptionMessage)
        where TException : Exception
    {
        var ex = Throws<TException>(testCode);
        VerifyExceptionMessage(ex, exceptionMessage);
        return ex;
    }

    /// <summary>
    /// Verifies that an exception of the given type is thrown.
    /// Also verifies that the exception message matches.
    /// </summary>
    /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="exceptionMessage">The exception message to verify</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static async Task<TException> ThrowsAsync<TException>(Func<Task> testCode, string exceptionMessage)
        where TException : Exception
    {
        // The 'testCode' Task might execute asynchronously in a different thread making it hard to enforce the thread culture.
        // The correct way to verify exception messages in such a scenario would be to run the task synchronously inside of a
        // culture enforced block.
        var ex = await Assert.ThrowsAsync<TException>(testCode).ConfigureAwait(false);
        VerifyExceptionMessage(ex, exceptionMessage);
        return ex;
    }

    /// <summary>
    /// Verifies that an exception of the given type is thrown.
    /// Also verified that the exception message matches.
    /// </summary>
    /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="exceptionMessage">The exception message to verify</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static TException Throws<TException>(Func<object> testCode, string exceptionMessage)
        where TException : Exception
    {
        return Throws<TException>(() => { testCode(); }, exceptionMessage);
    }

    /// <summary>
    /// Verifies that the code throws an <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="paramName">The name of the parameter that should throw the exception</param>
    /// <param name="exceptionMessage">The exception message to verify</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static ArgumentException ThrowsArgument(Action testCode, string paramName, string exceptionMessage)
    {
        return ThrowsArgumentInternal<ArgumentException>(testCode, paramName, exceptionMessage);
    }

    private static TException ThrowsArgumentInternal<TException>(
        Action testCode,
        string paramName,
        string exceptionMessage)
        where TException : ArgumentException
    {
        var ex = Throws<TException>(testCode);
        if (paramName != null)
        {
            Assert.Equal(paramName, ex.ParamName);
        }
        VerifyExceptionMessage(ex, exceptionMessage, partialMatch: true);
        return ex;
    }

    /// <summary>
    /// Verifies that the code throws an <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="paramName">The name of the parameter that should throw the exception</param>
    /// <param name="exceptionMessage">The exception message to verify</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static Task<ArgumentException> ThrowsArgumentAsync(Func<Task> testCode, string paramName, string exceptionMessage)
    {
        return ThrowsArgumentAsyncInternal<ArgumentException>(testCode, paramName, exceptionMessage);
    }

    private static async Task<TException> ThrowsArgumentAsyncInternal<TException>(
        Func<Task> testCode,
        string paramName,
        string exceptionMessage)
        where TException : ArgumentException
    {
        var ex = await Assert.ThrowsAsync<TException>(testCode).ConfigureAwait(false);
        if (paramName != null)
        {
            Assert.Equal(paramName, ex.ParamName);
        }
        VerifyExceptionMessage(ex, exceptionMessage, partialMatch: true);
        return ex;
    }

    /// <summary>
    /// Verifies that the code throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="paramName">The name of the parameter that should throw the exception</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static ArgumentNullException ThrowsArgumentNull(Action testCode, string paramName)
    {
        var ex = Throws<ArgumentNullException>(testCode);
        if (paramName != null)
        {
            Assert.Equal(paramName, ex.ParamName);
        }
        return ex;
    }

    /// <summary>
    /// Verifies that the code throws an ArgumentException with the expected message that indicates that the value cannot
    /// be null or empty.
    /// </summary>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="paramName">The name of the parameter that should throw the exception</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static ArgumentException ThrowsArgumentNullOrEmpty(Action testCode, string paramName)
    {
        return ThrowsArgumentInternal<ArgumentException>(testCode, paramName, "Value cannot be null or empty.");
    }

    /// <summary>
    /// Verifies that the code throws an ArgumentException with the expected message that indicates that the value cannot
    /// be null or empty.
    /// </summary>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="paramName">The name of the parameter that should throw the exception</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static Task<ArgumentException> ThrowsArgumentNullOrEmptyAsync(Func<Task> testCode, string paramName)
    {
        return ThrowsArgumentAsyncInternal<ArgumentException>(testCode, paramName, "Value cannot be null or empty.");
    }

    /// <summary>
    /// Verifies that the code throws an ArgumentNullException with the expected message that indicates that the value cannot
    /// be null or empty string.
    /// </summary>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="paramName">The name of the parameter that should throw the exception</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static ArgumentException ThrowsArgumentNullOrEmptyString(Action testCode, string paramName)
    {
        return ThrowsArgumentInternal<ArgumentException>(testCode, paramName, "Value cannot be null or an empty string.");
    }

    /// <summary>
    /// Verifies that the code throws an ArgumentNullException with the expected message that indicates that the value cannot
    /// be null or empty string.
    /// </summary>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="paramName">The name of the parameter that should throw the exception</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static Task<ArgumentException> ThrowsArgumentNullOrEmptyStringAsync(Func<Task> testCode, string paramName)
    {
        return ThrowsArgumentAsyncInternal<ArgumentException>(testCode, paramName, "Value cannot be null or an empty string.");
    }

    /// <summary>
    /// Verifies that the code throws an ArgumentOutOfRangeException (or optionally any exception which derives from it).
    /// </summary>
    /// <param name="testCode">A delegate to the code to be tested</param>
    /// <param name="paramName">The name of the parameter that should throw the exception</param>
    /// <param name="exceptionMessage">The exception message to verify</param>
    /// <param name="actualValue">The actual value provided</param>
    /// <returns>The exception that was thrown, when successful</returns>
    public static ArgumentOutOfRangeException ThrowsArgumentOutOfRange(Action testCode, string paramName, string exceptionMessage, object actualValue = null)
    {
        var ex = ThrowsArgumentInternal<ArgumentOutOfRangeException>(testCode, paramName, exceptionMessage);

        if (paramName != null)
        {
            Assert.Equal(paramName, ex.ParamName);
        }

        if (actualValue != null)
        {
            Assert.Equal(actualValue, ex.ActualValue);
        }

        return ex;
    }

    // We've re-implemented all the xUnit.net Throws code so that we can get this
    // updated implementation of RecordException which silently unwraps any instances
    // of AggregateException. In addition to unwrapping exceptions, this method ensures
    // that tests are executed in with a known set of Culture and UICulture. This prevents
    // tests from failing when executed on a non-English machine.
    private static Exception RecordException(Action testCode)
    {
        try
        {
            using (new CultureReplacer())
            {
                testCode();
            }
            return null;
        }
        catch (Exception exception)
        {
            return UnwrapException(exception);
        }
    }

    private static Exception UnwrapException(Exception exception)
    {
        return exception is AggregateException aggEx ? aggEx.GetBaseException() : exception;
    }

    private static TException VerifyException<TException>(Exception exception)
    {
        if (exception is TargetInvocationException tie)
        {
            exception = tie.InnerException;
        }
        Assert.NotNull(exception);
        return Assert.IsAssignableFrom<TException>(exception);
    }

    private static void VerifyExceptionMessage(Exception exception, string expectedMessage, bool partialMatch = false)
    {
        if (expectedMessage != null)
        {
            if (!partialMatch)
            {
                Assert.Equal(expectedMessage, exception.Message);
            }
            else
            {
                Assert.Contains(expectedMessage, exception.Message);
            }
        }
    }
}
