using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpContext : HttpContext
    {
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;

        private FeatureReference<ICanHasItems> _canHasItems;
        private FeatureReference<ICanHasServiceProviders> _canHasServiceProviders;
        private IFeatureCollection _features;

        public DefaultHttpContext(IFeatureCollection features)
        {
            _features = features;
            _request = new DefaultHttpRequest(this, features);
            _response = new DefaultHttpResponse(this, features);

            _canHasItems = FeatureReference<ICanHasItems>.Default;
            _canHasServiceProviders = FeatureReference<ICanHasServiceProviders>.Default;
        }

        ICanHasItems CanHasItems
        {
            get { return _canHasItems.Fetch(_features) ?? _canHasItems.Update(_features, new DefaultCanHasItems()); }
        }

        ICanHasServiceProviders CanHasServiceProviders
        {
            get { return _canHasServiceProviders.Fetch(_features) ?? _canHasServiceProviders.Update(_features, new DefaultCanHasServiceProviders()); }
        }

        public override HttpRequest Request { get { return _request; } }

        public override HttpResponse Response { get { return _response; } }

        public override IDictionary<object, object> Items
        {
            get { return CanHasItems.Items; }
        }

        public override IServiceProvider ApplicationServices
        {
            get { return CanHasServiceProviders.ApplicationServices; }
            set { CanHasServiceProviders.ApplicationServices = value; }
        }

        public override IServiceProvider RequestServices
        {
            get { return CanHasServiceProviders.RequestServices; }
            set { CanHasServiceProviders.RequestServices = value; }
        }

        public int Revision { get { return _features.Revision; } }

        public override void Dispose()
        {
            // REVIEW: is this necessary? is the environment "owned" by the context?
            _features.Dispose();
        }

        public override object GetFeature(Type type)
        {
            object value;
            return _features.TryGetValue(type, out value) ? value : null;
        }

        public override void SetFeature(Type type, object instance)
        {
            _features[type] = instance;
        }
    }
}
