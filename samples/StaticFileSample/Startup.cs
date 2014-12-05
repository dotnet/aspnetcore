using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.StaticFiles;

namespace StaticFilesSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseFileServer(new FileServerOptions()
            {
                EnableDirectoryBrowsing = true,
            });
        }
    }
}