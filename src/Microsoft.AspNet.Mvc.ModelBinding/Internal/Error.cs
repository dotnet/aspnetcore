using System;
using System.Globalization;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    internal static class Error
    {
        internal static ArgumentException ArgumentNull(string paramName)
        {
            return new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> with the provided properties.
        /// </summary>
        /// <param name="parameterName">The name of the parameter that caused the current exception.</param>
        /// <param name="message">A composite message explaining the reason for the exception.</param>
        /// <returns>The logged <see cref="Exception"/>.</returns>
        internal static ArgumentException Argument(string parameterName, string message)
        {
            return new ArgumentException(message, parameterName);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> with a default message.
        /// </summary>
        /// <param name="parameterName">The name of the parameter that caused the current exception.</param>
        /// <returns>The logged <see cref="Exception"/>.</returns>
        internal static ArgumentException ArgumentNullOrEmpty(string parameterName)
        {
            return Error.Argument(parameterName, Resources.FormatArgumentNullOrEmpty(parameterName));
        }
    }
}
