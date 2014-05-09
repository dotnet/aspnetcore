using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.StaticFiles;

namespace StaticFilesSample
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            app.UseFileServer(new FileServerOptions()
            {
                EnableDirectoryBrowsing = true,
                FileSystem = new PhysicalFileSystem(@"c:\temp")
            });
        }
    }
}