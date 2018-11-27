// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApiCompatShimWebSite
{
    // This was ported from the WebAPI 5.2 codebase. Kept the same intentionally for compatibility.
    [ActionSelectionFilter]
    public class TestController : ApiController
    {
        public User GetUser(int id) { return null; }
        public List<User> GetUsers() { return null; }

        public List<User> GetUsersByName(string name) { return null; }

        [AcceptVerbs("PATCH")]
        public void PutUser(User user) { }

        public User GetUserByNameAndId(string name, int id) { return null; }
        public User GetUserByNameAndAge(string name, int age) { return null; }
        public User GetUserByNameAgeAndSsn(string name, int age, int ssn) { return null; }
        public User GetUserByNameIdAndSsn(string name, int id, int ssn) { return null; }
        public User GetUserByNameAndSsn(string name, int ssn) { return null; }
        public User PostUser(User user) { return null; }
        public User PostUserByNameAndAge(string name, int age) { return null; }
        public User PostUserByName(string name) { return null; }
        public User PostUserByNameAndAddress(string name, UserAddress address) { return null; }
        public User DeleteUserByOptName(string name = null) { return null; }
        public User DeleteUserByIdAndOptName(int id, string name = "DefaultName") { return null; }
        public User DeleteUserByIdNameAndAge(int id, string name, int age) { return null; }
        public User DeleteUserById_Email_OptName_OptPhone(int id, string email, string name = null, int phone = 0) { return null; }
        public User DeleteUserById_Email_Height_OptName_OptPhone(int id, string email, double height, string name = "DefaultName", int? phone = null) { return null; }
        public void Head_Id_OptSize_OptIndex(int id, int size = 10, int index = 0) { }
        public void Head() { }
    }
}