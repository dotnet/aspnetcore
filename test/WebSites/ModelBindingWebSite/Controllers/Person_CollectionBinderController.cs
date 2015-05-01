// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    public class Person_CollectionBinderController : Controller
    {
        public PersonAddress CollectionType(PersonAddress address)
        {
            return address;
        }

        public UserWithAddress NestedCollectionType(UserWithAddress user)
        {
            return user;
        }

        public PeopleModel NestedCollectionOfRecursiveTypes(PeopleModel model)
        {
            return model;
        }

        public bool PostCheckBox(bool isValid)
        {
            return isValid;
        }

        public IEnumerable<UserPreference> PostCheckBoxList(IEnumerable<UserPreference> userPreferences)
        {
            return userPreferences;
        }
    }
}