// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;

namespace ViewComponentWebSite
{
    public class HomeController : Controller
    {
        private IEnumerable<SampleModel> ModelList { get; set; }

        public HomeController()
        {
            ModelList = new List<SampleModel>()
            {
                new SampleModel { Prop1 = "Hello", Prop2 = "World" },
                new SampleModel { Prop1 = "Sample", Prop2 = "Test" },
            };
        }

        public ViewResult ViewWithAsyncComponents()
        {
            return new ViewResult();
        }

        public ViewResult ViewWithSyncComponents()
        {
            return new ViewResult();
        }

        public ViewResult ViewWithIntegerViewComponent()
        {
            return new ViewResult();
        }

        public ViewResult ViewComponentWithEnumerableModelUsingWhere()
        {
            ViewBag.LinqQueryType = "Where";
            return View("ViewComponentWithEnumerableModel", ModelList.Where(a => a != null));
        }

        public ViewResult ViewComponentWithEnumerableModelUsingSelect()
        {
            ViewBag.LinqQueryType = "Select";
            return View("ViewComponentWithEnumerableModel", ModelList.Select(a => a));
        }
        
        public ViewResult ViewComponentWithEnumerableModelUsingTake()
        {
            ViewBag.LinqQueryType = "Take";
            return View("ViewComponentWithEnumerableModel", ModelList.Take(2));
        }

        public ViewResult ViewComponentWithEnumerableModelUsingTakeWhile()
        {
            ViewBag.LinqQueryType = "TakeWhile";
            return View("ViewComponentWithEnumerableModel", ModelList.TakeWhile(a => a != null));
        }

        public ViewResult ViewComponentWithEnumerableModelUsingUnion()
        {
            ViewBag.LinqQueryType = "Union";
            return View("ViewComponentWithEnumerableModel", ModelList.Union(ModelList));
        }

        public ViewResult ViewComponentWithEnumerableModelUsingSelectMany()
        {
            var selectManySampleModelList = new List<SelectManySampleModel>
                    {
                        new SelectManySampleModel {
                            TestModel =
                                new List<SampleModel> { ModelList.ElementAt(0) } },
                        new SelectManySampleModel {
                            TestModel =
                                new List<SampleModel> { ModelList.ElementAt(1) } }
                    };

            ViewBag.LinqQueryType = "SelectMany";
            return View("ViewComponentWithEnumerableModel", selectManySampleModelList.SelectMany(s => s.TestModel));
        }
    }
}