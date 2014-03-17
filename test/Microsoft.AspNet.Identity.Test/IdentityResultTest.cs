using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class IdentityResultTest
    {
        [Fact]
        public void VerifyDefaultConstructor()
        {
            var result = new IdentityResult();
            Assert.False(result.Succeeded);
            Assert.Equal(1, result.Errors.Count());
            Assert.Equal("An unknown failure has occured.", result.Errors.First());
        }
    }
}