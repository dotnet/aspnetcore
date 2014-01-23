using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpContext : HttpContext
    {
        private readonly IInterfaceDictionary _environment;
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;

        public DefaultHttpContext(IInterfaceDictionary environment)
        {
            _environment = environment;
            _request = new DefaultHttpRequest(this);
            _response = new DefaultHttpResponse(this);
        }

        public override HttpRequest Request { get { return _request; } }
        public override HttpResponse Response { get { return _response; } }

        public int Revision { get { return _environment.Revision; } }

        public override void Dispose()
        {
            // REVIEW: is this necessary? is the environment "owned" by the context?
            _environment.Dispose();
        }

        public override object GetInterface(Type type)
        {
            object value;
            return _environment.TryGetValue(type, out value) ? value : null;
        }

        public override void SetInterface(Type type, object instance)
        {
            _environment[type] = instance;
        }
    }
}
