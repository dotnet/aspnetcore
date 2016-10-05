using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Sockets
{
    public class SocketFormatters
    {
        private Dictionary<Type, EndPointFormatters> _formatters = new Dictionary<Type, EndPointFormatters>();

        private IServiceProvider _serviceProvider;

        public SocketFormatters(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public EndPointFormatters GetEndPointFormatters<TEndPoint>()
        {
            return GetEndPointFormatters(typeof(TEndPoint));
        }

        public EndPointFormatters GetEndPointFormatters(Type endPointType)
        {
            EndPointFormatters endPointFormatters;
            if (_formatters.TryGetValue(endPointType, out endPointFormatters))
            {
                return endPointFormatters;
            }

            return _formatters[endPointType] = new EndPointFormatters(_serviceProvider);
        }
    }
}
