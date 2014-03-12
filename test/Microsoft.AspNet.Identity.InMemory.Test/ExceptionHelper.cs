using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.InMemory.Test
{
    public static class ExceptionHelper
    {
        public static async Task<TException> ThrowsWithError<TException>(Func<Task> act, string error)
            where TException : Exception
        {
            var e = await Assert.ThrowsAsync<TException>(act);
            if (e != null)
            {
                Assert.Equal(error, e.Message);
            }
            return e;
        }

        public static async Task<ArgumentException> ThrowsArgumentException(Func<Task> del, string exceptionMessage,
            string paramName)
        {
            var e = await Assert.ThrowsAsync<ArgumentException>(del);
            // Only check exception message on English build and OS, since some exception messages come from the OS
            // and will be in the native language.
            // TODO: needed? if (IdentityResultAssert.EnglishBuildAndOS)
            //{
                Assert.Equal(exceptionMessage, e.Message);
                Assert.Equal(paramName, e.ParamName);;
            //}
            return e;
        }

        public static Task<ArgumentException> ThrowsArgumentNullOrEmpty(Func<Task> del, string paramName)
        {
            return ThrowsArgumentException(del, "Value cannot be null or empty.\r\nParameter name: " + paramName,
                paramName);
        }

        public static async Task<ArgumentNullException> ThrowsArgumentNull(Func<Task> del, string paramName)
        {
            var e = await Assert.ThrowsAsync<ArgumentNullException>(del);
            Assert.Equal(paramName, e.ParamName);
            return e;
        }
    }
}