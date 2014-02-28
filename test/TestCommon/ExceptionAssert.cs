using System;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    internal static class ExceptionAssert
    {
        /// <summary>
        /// Verifies that an exception of the given type (or optionally a derived type) is thrown.
        /// </summary>
        /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static TException Throws<TException>(Action testCode)
            where TException : Exception
        {
            Type exceptionType = typeof(TException);
            Exception exception = RecordException(testCode);

            TargetInvocationException tie = exception as TargetInvocationException;
            if (tie != null)
            {
                exception = tie.InnerException;
            }

            Assert.NotNull(exception);
            return Assert.IsAssignableFrom<TException>(exception);
        }

        /// <summary>
        /// Verifies that an exception of the given type (or optionally a derived type) is thrown.
        /// Also verifies that the exception message matches.
        /// </summary>
        /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static TException Throws<TException>(Action testCode, string exceptionMessage)
            where TException : Exception
        {
            var ex = Throws<TException>(testCode);
            VerifyExceptionMessage(ex, exceptionMessage);
            return ex;
        }

        /// <summary>
        /// Verifies that an exception of the given type (or optionally a derived type) is thrown.
        /// Also verified that the exception message matches.
        /// </summary>
        /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static TException Throws<TException>(Func<object> testCode, string exceptionMessage)
            where TException : Exception
        {
            return Throws<TException>(() => { testCode(); }, exceptionMessage);
        }

        /// <summary>
        /// Verifies that the code throws an <see cref="ArgumentException"/> (or optionally any exception which derives from it).
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentException ThrowsArgument(Action testCode, string paramName, string exceptionMessage)
        {
            var ex = Throws<ArgumentException>(testCode);

            if (paramName != null)
            {
                Assert.Equal(paramName, ex.ParamName);
            }

            VerifyExceptionMessage(ex, exceptionMessage, partialMatch: true);

            return ex;
        }

        /// <summary>
        /// Verifies that the code throws an ArgumentNullException (or optionally any exception which derives from it).
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
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
        /// Verifies that the code throws an ArgumentNullException with the expected message that indicates that the value cannot
        /// be null or empty.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentException ThrowsArgumentNullOrEmpty(Action testCode, string paramName)
        {
            return Throws<ArgumentException>(testCode, "Value cannot be null or empty.\r\nParameter name: " + paramName);
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
            AggregateException aggEx = exception as AggregateException;
            if (aggEx != null)
            {
                return aggEx.GetBaseException();
            }
            return exception;
        }

        private static void VerifyException(Type exceptionType, Exception exception)
        {
            Assert.NotNull(exception);
            Assert.IsAssignableFrom(exceptionType, exception);
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
}
