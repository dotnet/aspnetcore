using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.HeaderPropagation
{
    internal class HeaderPropagationMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly HeaderPropagationOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;

        public HeaderPropagationMessageHandlerBuilderFilter(IOptions<HeaderPropagationOptions> options, IHttpContextAccessor contextAccessor)
        {
            _options = options.Value;
            _contextAccessor = contextAccessor;
        }

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            return builder =>
            {
                builder.AdditionalHandlers.Add(new HeaderPropagationMessageHandler(_options, _contextAccessor));
                next(builder);
            };
        }
    }
}
