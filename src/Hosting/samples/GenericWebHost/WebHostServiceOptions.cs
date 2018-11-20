using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace GenericWebHost
{
    public class WebHostServiceOptions
    {
        public Action<HostBuilderContext, IApplicationBuilder> ConfigureApp { get; internal set; }
    }
}