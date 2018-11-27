// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using XmlFormattersWebSite.Models;

namespace XmlFormattersWebSite.Controllers
{
    public class IQueryableController : Controller
    {
        public IQueryable<int> ValueTypes()
        {
            return Enumerable.Range(1, 2).Select(i => i * 10).AsQueryable();
        }

        public IQueryable<string> NonWrappedTypes()
        {
            return Enumerable.Range(1, 2).Select(i => "value" + i).AsQueryable();
        }

        public IQueryable<Person> WrappedTypes()
        {
            return new[] {
                new Person() { Id = 10, Name = "Mike" },
                new Person() { Id = 11, Name = "Jimmy" }
            }.AsQueryable();
        }

        public IQueryable<Person> WrappedTypes_Empty()
        {
            return (new Person[] { }).AsQueryable();
        }

        public IQueryable<string> NonWrappedTypes_Empty()
        {
            return (new string[] { }).AsQueryable();
        }

        public IQueryable<string> NonWrappedTypes_NullInstance()
        {
            return null;
        }

        public IQueryable<Person> WrappedTypes_NullInstance()
        {
            return null;
        }
    }
}