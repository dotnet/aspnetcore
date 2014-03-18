using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Collections;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultCanHasResponseCookies : ICanHasResponseCookies
    {
        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpResponseInformation> _request = FeatureReference<IHttpResponseInformation>.Default;
        private IResponseCookiesCollection _cookiesCollection;

        public DefaultCanHasResponseCookies(IFeatureCollection features)
        {
            _features = features;
        }

        public IResponseCookiesCollection Cookies
        {
            get
            {
                var headers = _request.Fetch(_features).Headers;
                if (_cookiesCollection == null)
                {
                    _cookiesCollection = new ResponseCookiesCollection(new HeaderDictionary(headers));
                }

                return _cookiesCollection;
            }
        }
    }
}