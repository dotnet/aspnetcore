using System;

namespace SampleApp
{
    public class Program
    {
        private IServiceProvider _services;

        public Program(IServiceProvider services)
        {
            _services = services;
        }

        public void Main(string[] args)
        {
            new Microsoft.AspNet.Hosting.Program(_services).Main(new[] {
                "--server","kestrel"
            });
        }
    }
}
