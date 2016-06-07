using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.StaticFiles;

namespace NodeServicesExamples.Controllers
{
    public class ResizeImageController : Controller
    {
        private const int MaxDimension = 1000;
        private static string[] AllowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };

        private IHostingEnvironment _environment;
        private INodeServices _nodeServices;

        public ResizeImageController(IHostingEnvironment environment, INodeServices nodeServices)
        {
            _environment = environment;
            _nodeServices = nodeServices;
        }

        [Route("resize/{*imagePath}")]
        public async Task<IActionResult> Index(string imagePath, int maxWidth, int maxHeight)
        {
            // Validate incoming params
            if (maxWidth < 0 || maxHeight < 0 || maxWidth > MaxDimension || maxHeight > MaxDimension
                || (maxWidth + maxHeight) == 0)
            {
                return BadRequest("Invalid dimensions");
            }

            var mimeType = GetContentType(imagePath);
            if (Array.IndexOf(AllowedMimeTypes, mimeType) < 0)
            {
                return BadRequest("Disallowed image format");
            }

            // Locate source image on disk
            var fileInfo = _environment.WebRootFileProvider.GetFileInfo(imagePath);
            if (!fileInfo.Exists)
            {
                return NotFound();
            }

            // Invoke Node and pipe the result to the response
            var imageStream = await _nodeServices.Invoke<Stream>(
                "./Node/resizeImage",
                fileInfo.PhysicalPath,
                mimeType,
                maxWidth,
                maxHeight);
            return File(imageStream, mimeType);
        }

        private string GetContentType(string path)
        {
            string result;
            return new FileExtensionContentTypeProvider().TryGetContentType(path, out result) ? result : null;
        }
    }
}
