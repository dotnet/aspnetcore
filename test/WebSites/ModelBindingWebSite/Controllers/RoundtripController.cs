// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    public class RoundtripController : Controller
    {
        private IHtmlHelper<Person> _personHelper;
        private bool _activated;

        [FromServices]
        public IHtmlHelper<Person> PersonHelper
        {
            get
            {
                if (!_activated)
                {
                    _activated = true;
                    var viewData = new ViewDataDictionary<Person>(ViewData);
                    var context = new ViewContext(ActionContext, new TestView(), viewData, null, TextWriter.Null);
                    ((ICanHasViewContext)PersonHelper).Contextualize(context);
                }

                return _personHelper;
            }
            set
            {
                _personHelper = value;
            }
        }

        public string GetPerson()
        {
            return PersonHelper.NameFor(p => p.Name);
        }

        public string GetPersonParentAge()
        {
            return PersonHelper.NameFor(p => p.Parent.Age);
        }

        public string GetPersonDependentAge()
        {
            return PersonHelper.NameFor(p => p.Dependents[0].Age);
        }

        public string GetDependentPersonName()
        {
            return PersonHelper.NameFor(p => p.Dependents[0].Dependents[0].Name);
        }

        public string GetPersonParentHeightAttribute()
        {
            return PersonHelper.NameFor(p => p.Parent.Attributes["height"]);
        }

        [HttpPost]
        public Person Person(Person boundPerson)
        {
            return boundPerson;
        }

        private sealed class TestView : IView
        {
            public string Path { get; set; }

            public Task RenderAsync(ViewContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
