using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Http.HeaderPropagation
{
    public class HeaderPropagationMessageHandler : DelegatingHandler
    {
        private readonly HeaderPropagationOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;

        public HeaderPropagationMessageHandler(HeaderPropagationOptions options, IHttpContextAccessor contextAccessor)
        {
            _options = options;
            _contextAccessor = contextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_contextAccessor.HttpContext != null)
            {
                foreach (var header in _options.Headers)
                {
                    if (!_contextAccessor.HttpContext.Request.Headers.TryGetValue(header.InputName, out var values)
                        || StringValues.IsNullOrEmpty(values))
                    {
                        if (header.DefaultValuesGenerator != null)
                        {
                            values = header.DefaultValuesGenerator(request, _contextAccessor.HttpContext);
                            if (StringValues.IsNullOrEmpty(values)) continue;
                        }
                        else if (!StringValues.IsNullOrEmpty(header.DefaultValues))
                        {
                            values = header.DefaultValues;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (header.AlwaysAdd || !request.Headers.Contains(header.OutputName))
                    {
                        request.Headers.TryAddWithoutValidation(header.OutputName, (string[])values);
                    }
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
