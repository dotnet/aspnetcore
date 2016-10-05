using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;

public class EndPointFormatters
{
    private IServiceProvider _serviceProvider;
    private Dictionary<string, Dictionary<Type, Type>> _formatters = new Dictionary<string, Dictionary<Type, Type>>();

    public EndPointFormatters(IServiceProvider serviceProvider)
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

        throw new InvalidOperationException($"No formatter register for format '{format}' and type '{typeof(T).GetType().FullName}'");
    }
}
