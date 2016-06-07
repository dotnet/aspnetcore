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
            if (maxWidth > MaxDimension || maxHeight > MaxDimension || (maxHeight <= 0 && maxWidth <= 0))
            {
                return BadRequest("Invalid dimensions");
            }

            // Locate source image on disk
            var fileInfo = _environment.WebRootFileProvider.GetFileInfo(imagePath);
            if (!fileInfo.Exists)
            {
                return NotFound();
            }

            // Invoke Node and pipe the result to the response
            var mimeType = GetContentType(imagePath);
            var imageStream = await _nodeServices.Invoke<Stream>("./Node/resizeImage", fileInfo.PhysicalPath, mimeType, maxWidth, maxHeight);
            return File(imageStream, mimeType);
        }

        private string GetContentType(string path)
        {
            string result;
            if (!new FileExtensionContentTypeProvider().TryGetContentType(path, out result))
            {
                result = "application/octet-stream";
            }

            return result;
        }

        private class ResizeImageResult
        {
            public string Base64 { get; set; }
        }
    }
}
