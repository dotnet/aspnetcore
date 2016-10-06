using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace SocketsSample
{
    public static class FormatterExtensions
    {
        public static IApplicationBuilder UseFormatters(this IApplicationBuilder app, Action<FormatterBuilder> registerFormatters)
        {
            var formatters = app.ApplicationServices.GetRequiredService<SocketFormatters>();
            registerFormatters(new FormatterBuilder(formatters));
            return app;
        }
    }

    public class FormatterBuilder
    {
        private SocketFormatters _socketFormatters;

        public FormatterBuilder(SocketFormatters socketFormatters)
        {
            _socketFormatters = socketFormatters;
        }

        public void MapFormatter<T, TFormatterType>(string format)
            where TFormatterType : IFormatter<T>
        {
            _socketFormatters.RegisterFormatter<T, TFormatterType>(format);
        }
    }
}
