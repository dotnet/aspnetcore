// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Http;
using Microsoft.Net.Http.Headers;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    public class FileUploadController : Controller
    {
        public FileDetails UploadSingle(IFormFile file)
        {
            FileDetails fileDetails;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var fileContent = reader.ReadToEnd();
                var parsedContentDisposition = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
                fileDetails = new FileDetails
                {
                    Filename = parsedContentDisposition.FileName,
                    Content = fileContent
                };
            }

            return fileDetails;
        }

        public FileDetails[] UploadMultiple(IEnumerable<IFormFile> files)
        {
            var fileDetailsList = new List<FileDetails>();
            foreach (var file in files)
            {
                var parsedContentDisposition = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    var fileContent = reader.ReadToEnd();
                    var fileDetails = new FileDetails
                    {
                        Filename = parsedContentDisposition.FileName,
                        Content = fileContent
                    };
                    fileDetailsList.Add(fileDetails);
                }
            }

            return fileDetailsList.ToArray();
        }

        public IDictionary<string, IList<FileDetails>> UploadMultipleList(IEnumerable<IFormFile> filelist1,
                                                                     IEnumerable<IFormFile> filelist2)
        {
            var fileDetailsDict = new Dictionary<string, IList<FileDetails>>
            {
                { "filelist1", new List<FileDetails>() },
                { "filelist2", new List<FileDetails>() }
            };
            var fileDetailsList = new List<FileDetails>();
            foreach (var file in filelist1.Concat(filelist2))
            {
                var parsedContentDisposition = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    var fileContent = reader.ReadToEnd();
                    var fileDetails = new FileDetails
                    {
                        Filename = parsedContentDisposition.FileName,
                        Content = fileContent
                    };
                    fileDetailsDict[parsedContentDisposition.Name].Add(fileDetails);
                }
            }

            return fileDetailsDict;
        }

        public KeyValuePair<string, FileDetails> UploadModelWithFile(Book book)
        {
           var file = book.File;
            var reader = new StreamReader(file.OpenReadStream());
            var fileContent = reader.ReadToEnd();
            var parsedContentDisposition = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
            var fileDetails = new FileDetails
            {
                Filename = parsedContentDisposition.FileName,
                Content = fileContent
            };

            return new KeyValuePair<string, FileDetails>(book.Name, fileDetails);
        }
    }
}
