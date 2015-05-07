// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Controllers
{
    public class FormCollectionController : Controller
    {
        public IList<string> ReturnValuesAsList(IFormCollection form)
        {
            var valuesList = new List<string>();

            valuesList.Add(form["field1"]);
            valuesList.Add(form["field2"]);

            return valuesList;
        }

        public int ReturnCollectionCount(IFormCollection form)
        {
            return form.Count;
        }

        public ActionResult ReturnFileContent(FormCollection form)
        {
            var file = form.Files.GetFile("File");
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var fileContent = reader.ReadToEnd();

                return Content(fileContent);
            }
        }
    }
}
