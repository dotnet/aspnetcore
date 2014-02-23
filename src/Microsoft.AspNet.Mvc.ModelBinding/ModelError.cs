using System;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelError
    {
        public ModelError(Exception exception)
            : this(exception, errorMessage: null)
        {
        }

        public ModelError(Exception exception, string errorMessage)
            : this(errorMessage)
        {
            if (exception == null)
            {
                throw Error.ArgumentNull("exception");
            }

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
