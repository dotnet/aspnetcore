using System;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelError
    {
        public ModelError([NotNull]Exception exception)
            : this(exception, errorMessage: null)
        {
        }

        public ModelError([NotNull]Exception exception, string errorMessage)
            : this(errorMessage)
        {
            Exception = exception;
        }

        public ModelError(string errorMessage)
        {
            ErrorMessage = errorMessage ?? String.Empty;
        }

        public Exception Exception { get; private set; }

        public string ErrorMessage { get; private set; }
    }
}
