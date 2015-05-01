// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;

namespace ViewComponentWebSite
{
    public class EnumerableViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string linqQueryType)
        {
            var modelList = new List<SampleModel>()
            {
                new SampleModel { Prop1 = "Hello", Prop2 = "World" },
                new SampleModel { Prop1 = linqQueryType, Prop2 = "Test" },
            };

            switch (linqQueryType) {
                case "Where":
                    return View(modelList.Where(e => e != null));

                case "Take":
                    return View(modelList.Take(2));

                case "TakeWhile":
                    return View(modelList.TakeWhile(a => a != null));

                case "Union":
                    return View(modelList.Union(modelList));

                case "SelectMany":
                    var selectManySampleModelList = new List<SelectManySampleModel>
                    {
                        new SelectManySampleModel {
                            TestModel =
                                new List<SampleModel> { new SampleModel { Prop1 = "Hello", Prop2 = "World" } } },
                        new SelectManySampleModel {
                            TestModel = 
                                new List<SampleModel> { new SampleModel{ Prop1 = linqQueryType, Prop2 = "Test" } } }
                    };

                    return View(selectManySampleModelList.SelectMany(a => a.TestModel));
            };

            return View(modelList.Select(e => e));
        }
    }
}