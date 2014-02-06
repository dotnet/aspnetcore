using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing.Template.Tests
{
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
