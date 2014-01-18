// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.TestCommon
{
    public partial class Assert
    {
        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// </summary>
        /// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static T Throws<T>(Action testCode)
            where T : Exception
        {
            return (T)Throws(typeof(T), testCode);
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// Generally used to test property accessors.
        /// </summary>
        /// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static T Throws<T>(Func<object> testCode)
            where T : Exception
        {
            return (T)Throws(typeof(T), testCode);
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// </summary>
        /// <param name="exceptionType">The type of the exception expected to be thrown</param>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static Exception Throws(Type exceptionType, Action testCode)
        {
            Exception exception = RecordException(testCode);
            return VerifyException(exceptionType, exception);
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// Generally used to test property accessors.
        /// </summary>
        /// <param name="exceptionType">The type of the exception expected to be thrown</param>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static Exception Throws(Type exceptionType, Func<object> testCode)
        {
            return Throws(exceptionType, () => { testCode(); });
        }

        /// <summary>
        /// Verifies that an exception of the given type (or optionally a derived type) is thrown.
        /// </summary>
        /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static TException Throws<TException>(Action testCode, bool allowDerivedExceptions)
            where TException : Exception
        {
            Type exceptionType = typeof(TException);
            Exception exception = RecordException(testCode);

            TargetInvocationException tie = exception as TargetInvocationException;
            if (tie != null)
            {
                exception = tie.InnerException;
            }

            if (exception == null)
            {
                throw new ThrowsException(exceptionType);
            }

            var typedException = exception as TException;
            if (typedException == null || (!allowDerivedExceptions && typedException.GetType() != typeof(TException)))
            {
                throw new ThrowsException(exceptionType, exception);
            }

            return typedException;
        }

        /// <summary>
        /// Verifies that an exception of the given type (or optionally a derived type) is thrown.
        /// Generally used to test property accessors.
        /// </summary>
        /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static TException Throws<TException>(Func<object> testCode, bool allowDerivedExceptions)
            where TException : Exception
        {
            return Throws<TException>(() => { testCode(); }, allowDerivedExceptions);
        }

        /// <summary>
        /// Verifies that an exception of the given type (or optionally a derived type) is thrown.
        /// Also verifies that the exception message matches.
        /// </summary>
        /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static TException Throws<TException>(Action testCode, string exceptionMessage, bool allowDerivedExceptions = false)
            where TException : Exception
        {
            var ex = Throws<TException>(testCode, allowDerivedExceptions);
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
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static TException Throws<TException>(Func<object> testCode, string exceptionMessage, bool allowDerivedExceptions = false)
            where TException : Exception
        {
            return Throws<TException>(() => { testCode(); }, exceptionMessage, allowDerivedExceptions);
        }

        /// <summary>
        /// Verifies that the code throws an <see cref="ArgumentException"/> (or optionally any exception which derives from it).
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentException ThrowsArgument(Action testCode, string paramName, bool allowDerivedExceptions = false)
        {
            var ex = Throws<ArgumentException>(testCode, allowDerivedExceptions);

            if (paramName != null)
            {
                Equal(paramName, ex.ParamName);
            }

            return ex;
        }

        /// <summary>
        /// Verifies that the code throws an <see cref="ArgumentException"/> (or optionally any exception which derives from it).
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentException ThrowsArgument(Action testCode, string paramName, string exceptionMessage, bool allowDerivedExceptions = false)
        {
            var ex = Throws<ArgumentException>(testCode, allowDerivedExceptions);

            if (paramName != null)
            {
                Equal(paramName, ex.ParamName);
            }

            VerifyExceptionMessage(ex, exceptionMessage, partialMatch: true);

            return ex;
        }

        /// <summary>
        /// Verifies that the code throws an ArgumentException (or optionally any exception which derives from it).
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentException ThrowsArgument(Func<object> testCode, string paramName, bool allowDerivedExceptions = false)
        {
            var ex = Throws<ArgumentException>(testCode, allowDerivedExceptions);

            if (paramName != null)
            {
                Equal(paramName, ex.ParamName);
            }

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
            var ex = Throws<ArgumentNullException>(testCode, allowDerivedExceptions: false);

            if (paramName != null)
            {
                Equal(paramName, ex.ParamName);
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
            return Throws<ArgumentException>(testCode, "Value cannot be null or empty.\r\nParameter name: " + paramName, allowDerivedExceptions: false);
        }

        /// <summary>
        /// Verifies that the code throws an ArgumentNullException with the expected message that indicates that the value cannot
        /// be null or empty string.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentException ThrowsArgumentNullOrEmptyString(Action testCode, string paramName)
        {
            return ThrowsArgument(testCode, paramName, "Value cannot be null or an empty string.", allowDerivedExceptions: true);
        }

        /// <summary>
        /// Verifies that the code throws an ArgumentOutOfRangeException (or optionally any exception which derives from it).
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <param name="actualValue">The actual value provided</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentOutOfRangeException ThrowsArgumentOutOfRange(Action testCode, string paramName, string exceptionMessage, bool allowDerivedExceptions = false, object actualValue = null)
        {
            if (exceptionMessage != null)
            {
                exceptionMessage = exceptionMessage + "\r\nParameter name: " + paramName;
                if (actualValue != null)
                {
                    exceptionMessage += String.Format(CultureReplacer.DefaultCulture, "\r\nActual value was {0}.", actualValue);
                }
            }

            var ex = Throws<ArgumentOutOfRangeException>(testCode, exceptionMessage, allowDerivedExceptions);

            if (paramName != null)
            {
                Equal(paramName, ex.ParamName);
            }

            return ex;
        }

        /// <summary>
        /// Verifies that the code throws an <see cref="ArgumentOutOfRangeException"/> with the expected message that indicates that
        /// the value must be greater than the given <paramref name="value"/>.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="actualValue">The actual value provided.</param>
        /// <param name="value">The expected limit value.</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentOutOfRangeException ThrowsArgumentGreaterThan(Action testCode, string paramName, string value, object actualValue = null)
        {
            return ThrowsArgumentOutOfRange(
                        testCode,
                        paramName,
                        String.Format(CultureReplacer.DefaultCulture, "Value must be greater than {0}.", value), false, actualValue);
        }

        /// <summary>
        /// Verifies that the code throws an <see cref="ArgumentOutOfRangeException"/> with the expected message that indicates that
        /// the value must be greater than or equal to the given value.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="value">The expected limit value.</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentOutOfRangeException ThrowsArgumentGreaterThanOrEqualTo(Action testCode, string paramName, string value, object actualValue = null)
        {
            return ThrowsArgumentOutOfRange(
                        testCode,
                        paramName,
                        String.Format(CultureReplacer.DefaultCulture, "Value must be greater than or equal to {0}.", value), false, actualValue);
        }

        /// <summary>
        /// Verifies that the code throws an <see cref="ArgumentOutOfRangeException"/> with the expected message that indicates that
        /// the value must be less than the given <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="actualValue">The actual value provided.</param>
        /// <param name="maxValue">The expected limit value.</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentOutOfRangeException ThrowsArgumentLessThan(Action testCode, string paramName, string maxValue, object actualValue = null)
        {
            return ThrowsArgumentOutOfRange(
                        testCode,
                        paramName,
                        String.Format(CultureReplacer.DefaultCulture, "Value must be less than {0}.", maxValue), false, actualValue);
        }

        /// <summary>
        /// Verifies that the code throws an <see cref="ArgumentOutOfRangeException"/> with the expected message that indicates that
        /// the value must be less than or equal to the given <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="actualValue">The actual value provided.</param>
        /// <param name="maxValue">The expected limit value.</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentOutOfRangeException ThrowsArgumentLessThanOrEqualTo(Action testCode, string paramName, string maxValue, object actualValue = null)
        {
            return ThrowsArgumentOutOfRange(
                        testCode,
                        paramName,
                        String.Format(CultureReplacer.DefaultCulture, "Value must be less than or equal to {0}.", maxValue), false, actualValue);
        }

        /// <summary>
        /// Verifies that the code throws an HttpException (or optionally any exception which derives from it).
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <param name="httpCode">The expected HTTP status code of the exception</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static HttpException ThrowsHttpException(Action testCode, string exceptionMessage, int httpCode, bool allowDerivedExceptions = false)
        {
            var ex = Throws<HttpException>(testCode, exceptionMessage, allowDerivedExceptions);
            Equal(httpCode, ex.GetHttpCode());
            return ex;
        }

        /// <summary>
        /// Verifies that the code throws an InvalidEnumArgumentException (or optionally any exception which derives from it).
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="invalidValue">The expected invalid value that should appear in the message</param>
        /// <param name="enumType">The type of the enumeration</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static InvalidEnumArgumentException ThrowsInvalidEnumArgument(Action testCode, string paramName, int invalidValue, Type enumType, bool allowDerivedExceptions = false)
        {
            string message = String.Format(CultureReplacer.DefaultCulture,
                                           "The value of argument '{0}' ({1}) is invalid for Enum type '{2}'.{3}Parameter name: {0}",
                                           paramName, invalidValue, enumType.Name, Environment.NewLine);
            return Throws<InvalidEnumArgumentException>(testCode, message, allowDerivedExceptions);
        }

        /// <summary>
        /// Verifies that the code throws an HttpException (or optionally any exception which derives from it).
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="objectName">The name of the object that was dispose</param>
        /// <param name="allowDerivedExceptions">Pass true to allow exceptions which derive from TException; pass false, otherwise</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ObjectDisposedException ThrowsObjectDisposed(Action testCode, string objectName, bool allowDerivedExceptions = false)
        {
            var ex = Throws<ObjectDisposedException>(testCode, allowDerivedExceptions);

            if (objectName != null)
            {
                Equal(objectName, ex.ObjectName);
            }

            return ex;
        }

        /// <summary>
        /// Verifies that an exception of the given type is thrown.
        /// </summary>
        /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        /// <remarks>
        /// Unlike other Throws* methods, this method does not enforce running the exception delegate with a known Thread Culture.
        /// </remarks>
        public static async Task<TException> ThrowsAsync<TException>(Func<Task> testCode)
            where TException : Exception
        {
            Exception exception = null;
            try
            {
                // The 'testCode' Task might execute asynchronously in a different thread making it hard to enforce the thread culture.
                // The correct way to verify exception messages in such a scenario would be to run the task synchronously inside of a 
                // culture enforced block.
                await testCode();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            VerifyException(typeof(TException), exception);
            return (TException)exception;
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

        private static Exception VerifyException(Type exceptionType, Exception exception)
        {
            if (exception == null)
            {
                throw new ThrowsException(exceptionType);
            }
            else if (exceptionType != exception.GetType())
            {
                throw new ThrowsException(exceptionType, exception);
            }

            return exception;
        }

        private static void VerifyExceptionMessage(Exception exception, string expectedMessage, bool partialMatch = false)
        {
            if (expectedMessage != null)
            {
                if (!partialMatch)
                {
                    Equal(expectedMessage, exception.Message);
                }
                else
                {
                    Contains(expectedMessage, exception.Message);
                }
            }
        }

        // Custom ThrowsException so we can filter the stack trace.
        [Serializable]
        private class ThrowsException : Xunit.Sdk.ThrowsException
        {
            public ThrowsException(Type type) : base(type) { }

            public ThrowsException(Type type, Exception ex) : base(type, ex) { }

            protected override bool ExcludeStackFrame(string stackFrame)
            {
                if (stackFrame.StartsWith("at Microsoft.TestCommon.Assert.", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return base.ExcludeStackFrame(stackFrame);
            }
        }
    }
}
