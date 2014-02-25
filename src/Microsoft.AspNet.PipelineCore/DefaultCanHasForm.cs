using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultCanHasForm : ICanHasForm
    {
        private readonly IFeatureCollection _features;
        private readonly FeatureReference<IHttpRequestInformation> _request = FeatureReference<IHttpRequestInformation>.Default;
        private Stream _bodyStream;
        private IReadableStringCollection _form;

        public DefaultCanHasForm(IFeatureCollection features)
        {
            _features = features;
        }

        public async Task<IReadableStringCollection> GetFormAsync()
        {
            var body = _request.Fetch(_features).Body;

            if (_bodyStream == null || _bodyStream != body)
            {
                _bodyStream = body;
                using (var streamReader = new StreamReader(body, Encoding.UTF8,
                                                           detectEncodingFromByteOrderMarks: true,
                                                           bufferSize: 1024, leaveOpen: true))
                {
                    string formQuery = await streamReader.ReadToEndAsync();
                    _form = new ReadableStringCollection(ParsingHelpers.GetQuery(formQuery));
                }
            }
            return _form;
        }
    }
}
