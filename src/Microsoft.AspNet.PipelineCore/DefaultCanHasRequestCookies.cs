using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Infrastructure;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultCanHasRequestCookies : ICanHasRequestCookies
    {
        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpRequestInformation> _request = FeatureReference<IHttpRequestInformation>.Default;
        private string _cookiesHeader;
        private RequestCookiesCollection _cookiesCollection;
        private static readonly string[] ZeroHeaders = new string[0];

        public DefaultCanHasRequestCookies(IFeatureCollection features)
        {
            _features = features;
        }

        public IReadableStringCollection Cookies
        {
            get
            {
                var headers = _request.Fetch(_features).Headers;
                string cookiesHeader = ParsingHelpers.GetHeader(headers, Constants.Headers.Cookie) ?? "";

                if (_cookiesCollection == null)
                {
                    _cookiesCollection = new RequestCookiesCollection();
                    _cookiesCollection.Reparse(cookiesHeader);
                    _cookiesHeader = cookiesHeader;
                }
                else if (!string.Equals(_cookiesHeader, cookiesHeader, StringComparison.Ordinal))
                {
                    _cookiesCollection.Reparse(cookiesHeader);
                    _cookiesHeader = cookiesHeader;
                }

                return _cookiesCollection;
            }
        }
    }
}