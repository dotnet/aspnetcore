using System;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Hosting.Builder
{
    public class BuilderFactory : IBuilderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public BuilderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IBuilder CreateBuilder()
        {
            return new PipelineCore.Builder(_serviceProvider);
        }
    }
}
