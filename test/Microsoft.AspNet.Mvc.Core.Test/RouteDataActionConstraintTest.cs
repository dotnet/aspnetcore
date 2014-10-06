using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class RouteDataActionConstraintTest
    {
        [Fact]
        public void RouteDataActionConstraint_DenyKeyByPassingEmptyString()
        {
            var routeDataConstraint = new RouteDataActionConstraint("key", string.Empty);

            Assert.Equal(routeDataConstraint.RouteKey, "key");
            Assert.Equal(routeDataConstraint.KeyHandling, RouteKeyHandling.DenyKey);
            Assert.Equal(routeDataConstraint.RouteValue, string.Empty);
        }

        [Fact]
        public void RouteDataActionConstraint_DenyKeyByPassingNull()
        {
            var routeDataConstraint = new RouteDataActionConstraint("key", null);

            Assert.Equal(routeDataConstraint.RouteKey, "key");
            Assert.Equal(routeDataConstraint.KeyHandling, RouteKeyHandling.DenyKey);
            Assert.Equal(routeDataConstraint.RouteValue, string.Empty);
        }

        [Fact]
        public void RouteDataActionConstraint_RequireKeyByPassingNonEmpty()
        {
            var routeDataConstraint = new RouteDataActionConstraint("key", "value");

            Assert.Equal(routeDataConstraint.RouteKey, "key");
            Assert.Equal(routeDataConstraint.KeyHandling, RouteKeyHandling.RequireKey);
            Assert.Equal(routeDataConstraint.RouteValue, "value");
        }

        [Fact]
        public void RouteDataActionConstraint_CatchAll()
        {
            var routeDataConstraint = RouteDataActionConstraint.CreateCatchAll("key");

            Assert.Equal(routeDataConstraint.RouteKey, "key");
            Assert.Equal(routeDataConstraint.KeyHandling, RouteKeyHandling.CatchAll);
            Assert.Equal(routeDataConstraint.RouteValue, string.Empty);
        }
    }
}