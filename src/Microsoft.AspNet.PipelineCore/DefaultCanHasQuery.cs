using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultCanHasQuery : ICanHasQuery
    {
        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpRequestInformation> _request = FeatureReference<IHttpRequestInformation>.Default;
        private string _queryString;
        private IReadableStringCollection _query;

        public DefaultCanHasQuery(IFeatureCollection features)
        {
            _features = features;
        }

        public IReadableStringCollection Query
        {
            get
            {
                var queryString = _request.Fetch(_features).QueryString;
                if (_query == null || _queryString != queryString)
                {
                    _queryString = queryString;
                    // TODO
                    _query = new ReadableStringCollection(new Dictionary<string, string[]>());
                }
                return _query;
            }
        }
    }
}