using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.StaticFiles;

namespace StaticFilesSample
{
    public class Startup
    {
        public void Configuration(IApplicationBuilder app)
        {
            app.UseFileServer(new FileServerOptions()
            {
                EnableDirectoryBrowsing = true,
                FileSystem = new PhysicalFileSystem(@"c:\temp")
            });
        }
    }
}