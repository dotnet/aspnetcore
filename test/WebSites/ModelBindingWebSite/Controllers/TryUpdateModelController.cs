// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    public class TryUpdateModelController : Controller
    {
        public async Task<Person> GetPerson()
        {
            // Person has a property of type Person. Only Top level should be updated.
            var person = new Person();
            await TryUpdateModelAsync(
                person,
                prefix: string.Empty,
                includeExpressions: m => m.Parent);

            return person;
        }

        public async Task<User> GetUserAsync_IncludeAllByDefault(int id)
        {
            var user = GetUser(id);

            await TryUpdateModelAsync<User>(user, prefix: string.Empty);
            return user;
        }

        public async Task<User> GetUserAsync_ExcludeSpecificProperties(int id)
        {
            var user = GetUser(id);
            await TryUpdateModelAsync(
                user,
                prefix: string.Empty,
                predicate:
                (context, modelName) =>
                    !string.Equals(modelName, nameof(Models.User.Id), StringComparison.Ordinal) &&
                    !string.Equals(modelName, nameof(Models.User.Key), StringComparison.Ordinal));

            return user;
        }

        public async Task<bool> CreateAndUpdateUser()
        {
            // don't update the id.
            var user = new User();
            return await TryUpdateModelAsync(user,
                                             prefix: string.Empty,
                                             includeExpressions: model => model.RegisterationMonth);
        }

        public async Task<User> GetUserAsync_IncludeSpecificProperties(int id)
        {
            var user = GetUser(id);
            await TryUpdateModelAsync(user,
                                      prefix: string.Empty,
                                      includeExpressions: model => model.RegisterationMonth);

            return user;
        }

        public async Task<bool> TryUpdateModelFails(int id)
        {
            var user = GetUser(id);
            return await TryUpdateModelAsync(user,
                                             prefix: string.Empty,
                                             valueProvider: new CustomValueProvider());
        }

        public async Task<User> GetUserAsync_IncludeAndExcludeListNull(int id)
        {
            var user = GetUser(id);
            await TryUpdateModelAsync(user);

            return user;
        }

        public async Task<User> GetUserAsync_IncludesAllSubProperties(int id)
        {
            var user = GetUser(id);

            await TryUpdateModelAsync(user, prefix: string.Empty, includeExpressions: model => model.Address);

            return user;
        }

        public async Task<User> GetUserAsync_WithChainedProperties(int id)
        {
            var user = GetUser(id);

            // Since this is a chained expression this would throw
            await TryUpdateModelAsync(user, prefix: string.Empty, includeExpressions: model => model.Address.Country);

            return user;
        }

        public async Task<Employee> GetEmployeeAsync_BindToBaseDeclaredType()
        {
            var backingStore = new QueryCollection(
            new Dictionary<string, StringValues>
            {
                { "Parent.Name", new[] { "fatherName"} },
                { "Parent.Parent.Name", new[] {"grandFatherName" } },
                { "Department", new[] {"Sales" } }
            });

            Person employee = new Employee();
            await TryUpdateModelAsync(
                employee,
                employee.GetType(),
                prefix: string.Empty,
                valueProvider: new QueryStringValueProvider(
                    BindingSource.Query,
                    backingStore,
                    CultureInfo.CurrentCulture),
                predicate: (content, propertyName) => true);

            return (Employee)employee;
        }

        public async Task<User> GetUserAsync_ModelType_IncludeAll(int id)
        {
            var backingStore = new QueryCollection(
            new Dictionary<string, StringValues>
            {
                { "Key", new[] { "123"} },
                { "RegisterationMonth", new[] {"March" } },
                { "UserName", new[] {"SomeName" } }
            });

            var user = GetUser(id);

            await TryUpdateModelAsync(user,
                typeof(User),
                prefix: string.Empty,
                valueProvider: new QueryStringValueProvider(
                    BindingSource.Query,
                    backingStore,
                    CultureInfo.CurrentCulture),
                predicate: (content, propertyName) => true);

            return user;
        }

        public async Task<User> GetUserAsync_ModelType_IncludeAllByDefault(int id)
        {
            var user = GetUser(id);

            await TryUpdateModelAsync(user, user.GetType(), prefix: string.Empty);
            return user;
        }

        public async Task<IActionResult> TryUpdateModel_ClearsModelStateEntries()
        {
            var result = new ObjectResult(null);

            // Invalid model.
            var model = new MyModel
            {
                Id = 1,
                Price = -1
            };

            // Validate model first and subsequent TryUpdateModel should remove
            //modelstate entries for model and re-validate.
            TryValidateModel(model);

            // Update Name to a valid value and call TryUpdateModel
            model.Price = 1;
            await TryUpdateModelAsync<MyModel>(model);

            if (ModelState.IsValid)
            {
                result.StatusCode = StatusCodes.Status204NoContent;
            }
            else
            {
                result.StatusCode = StatusCodes.Status500InternalServerError;
            } 

            return result;
        }

        private User GetUser(int id)
        {
            return new User
            {
                UserName = "User_" + id,
                Id = id,
                Key = id + 20,
            };
        }

        private class MyModel
        {
            public int Id { get; set; }

            [Range(0,10)]
            public double Price { get; set; }
        }

        public class CustomValueProvider : IValueProvider
        {
            public bool ContainsPrefix(string prefix)
            {
                return false;
            }

            public ValueProviderResult GetValue(string key)
            {
                return ValueProviderResult.None;
            }
        }
    }
}