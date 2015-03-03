// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    public class FromBodyControllerProperty : Controller
    {
        [FromBody]
        public User SiteUser { get; set; }

        public User GetSiteUser(int id)
        {
            return SiteUser;
        }

        // Will throw as Customer reads body.
        public Customer GetCustomer(Customer customer)
        {
            return customer;
        }

        // Will throw as a controller property and a parameter name are being read from body.
        public void AddUser([FromBody] User user)
        {
        }
    }
}