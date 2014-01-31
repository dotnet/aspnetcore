using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Owin;
using Xunit;

namespace Microsoft.AspNet.AppBuilderSupport.Tests
{
    public class OwinHttpEnvironmentTests
    {
        private T Get<T>(IFeatureCollection features)
        {
            object value;
            return features.TryGetValue(typeof(T), out value) ? (T)value : default(T);
        }

        [Fact]
        public void OwinHttpEnvironmentCanBeCreated()
        {
            var env = new Dictionary<string, object>
            {
                {"owin.RequestMethod", "POST"}
            };
            var features = new FeatureObject( new OwinHttpEnvironment(env));

            Assert.Equal(Get<IHttpRequestInformation>(features).Method, "POST");
        }

        [Fact]
        public void ImplementedInterfacesAreEnumerated()
        {
            var env = new Dictionary<string, object>
            {
                {"owin.RequestMethod", "POST"}
            };
            var features = new FeatureObject(new OwinHttpEnvironment(env));

            var entries = features.ToArray();
            var keys = features.Keys.ToArray();
            var values = features.Values.ToArray();

            Assert.Contains(typeof(IHttpRequestInformation), keys);
            Assert.Contains(typeof(IHttpResponseInformation), keys);
        }
    }
}
