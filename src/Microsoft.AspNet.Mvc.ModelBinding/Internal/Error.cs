using System;
using System.Globalization;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    internal static class Error
    {
        internal static ArgumentException ArgumentNull(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        internal static Exception InvalidOperation(string message, params object[] args)
        {
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, message, args));
        }

        internal static Exception InvalidOperation(Exception ex, string message, params object[] args)
        {
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, message, args), ex);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> with the provided properties.
        /// </summary>
        /// <param name="messageFormat">A composite format string explaining the reason for the exception.</param>
        /// <param name="messageArgs">An object array that contains zero or more objects to format.</param>
        /// <returns>The logged <see cref="Exception"/>.</returns>
        internal static ArgumentException Argument(string messageFormat, params object[] messageArgs)
        {
            return new ArgumentException(String.Format(CultureInfo.CurrentCulture, messageFormat, messageArgs));
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> with the provided properties.
        /// </summary>
        /// <param name="parameterName">The name of the parameter that caused the current exception.</param>
        /// <param name="messageFormat">A composite format string explaining the reason for the exception.</param>
        /// <param name="messageArgs">An object array that contains zero or more objects to format.</param>
        /// <returns>The logged <see cref="Exception"/>.</returns>
        internal static ArgumentException Argument(string parameterName, string messageFormat, params object[] messageArgs)
        {
            return new ArgumentException(String.Format(CultureInfo.CurrentCulture, messageFormat, messageArgs), parameterName);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> with a default message.
        /// </summary>
        /// <param name="parameterName">The name of the parameter that caused the current exception.</param>
        /// <returns>The logged <see cref="Exception"/>.</returns>
        internal static ArgumentException ArgumentNullOrEmpty(string parameterName)
        {
            return Error.Argument(parameterName, Resources.ArgumentNullOrEmpty, parameterName);
        }
    }
}
