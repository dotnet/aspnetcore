#if NET45
using System;
using System.IO;
using Microsoft.AspNet.Abstractions;
using Microsoft.Owin.FileSystems;
using Microsoft.AspNet;
using Owin;
using Microsoft.AspNet.StaticFiles;

namespace StaticFilesSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();

            // Temporary bridge from katana to Owin
            app.UseBuilder(ConfigurePK);
        }

        private void ConfigurePK(IBuilder builder)
        {
            builder.UseFileServer(new FileServerOptions()
            {
                EnableDirectoryBrowsing = true,
                FileSystem = new PhysicalFileSystem(@"c:\temp")
            });
        }
    }
}
#endif