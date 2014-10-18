// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Controllers
{
    public class WithoutMetadataController : Controller
    {
        public Person Person { get; set; }

        public Person GetPersonProperty()
        {
            return Person;
        }

        public Person GetPersonParameter(Person p)
        {
            return p;
        }

        public void SimpleTypes(int id, string name, bool isValid, DateTime lastUpdateTime)
        {
        }
    }
}