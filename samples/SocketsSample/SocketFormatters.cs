using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace SocketsSample
{
    public class SocketFormatters
    {
        private IServiceProvider _serviceProvider;
        private Dictionary<string, Dictionary<Type, Type>> _formatters = new Dictionary<string, Dictionary<Type, Type>>();
        private Dictionary<string, IInvocationAdapter> _invocationAdapters = new Dictionary<string, IInvocationAdapter>();

        public SocketFormatters(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void RegisterFormatter<T, TFormatterType>(string format)
            where TFormatterType : IFormatter<T>
        {
            Dictionary<Type, Type> formatFormatters;
            if (!_formatters.TryGetValue(format, out formatFormatters))
            {
                formatFormatters = _formatters[format] = new Dictionary<Type, Type>();
            }

            formatFormatters[typeof(T)] = typeof(TFormatterType);
        }

        public IFormatter<T> GetFormatter<T>(string format)
        {
            Dictionary<Type, Type> formatters;
            Type targetFormatterType;

            if (_formatters.TryGetValue(format, out formatters) && formatters.TryGetValue(typeof(T), out targetFormatterType))
            {
                return (IFormatter<T>)_serviceProvider.GetRequiredService(targetFormatterType);
            }

            return null;
            // throw new InvalidOperationException($"No formatter register for format '{format}' and type '{typeof(T).GetType().FullName}'");
        }

        public void RegisterInvocationAdapter(string format, IInvocationAdapter adapter)
        {
            _invocationAdapters[format] = adapter;
        }

        public IInvocationAdapter GetInvocationAdapter(string format)
        {
            IInvocationAdapter value;

            return _invocationAdapters.TryGetValue(format, out value) ? value : null;
        }
    }
}