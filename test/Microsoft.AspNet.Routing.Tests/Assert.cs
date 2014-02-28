using System;

namespace Microsoft.AspNet.Routing
{
    // Placeholder until we get our 'real' rich support for these asserts.
    public class Assert : Xunit.Assert
    {
        public static T Throws<T>(Assert.ThrowsDelegate action, string message) where T : Exception
        {
            T exception = Assert.Throws<T>(action);
            Assert.Equal<string>(message, exception.Message);
            return exception;
        }

        public static T Throws<T>(Assert.ThrowsDelegateWithReturn action, string message) where T : Exception
        {
            T exception = Assert.Throws<T>(action);
            Assert.Equal<string>(message, exception.Message);
            return exception;
        }
    }
}
