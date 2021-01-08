using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.Twitter
{
    public class TwitterException : Exception
    {
        public TwitterException()
        {
        }

        public TwitterException(string message)
            : base(message)
        {
        }

        public TwitterException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public static TwitterException FromTwitterErrorResponse(TwitterErrorResponse twitterErrorResponse)
        {
            var errorMessage = "An error has occured while calling the Twitter API, error's returned:";

            errorMessage += twitterErrorResponse.Errors.Aggregate("", (currentString, nextError)
                => currentString + $"Code: {nextError.Code}, Message: '{nextError.Message}'" + Environment.NewLine);

            return new TwitterException(errorMessage);
        }
    }
}
