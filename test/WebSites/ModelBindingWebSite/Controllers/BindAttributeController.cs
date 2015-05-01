// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    public class BindAttributeController : Controller
    {
        public User EchoUser([Bind(typeof(ExcludeUserPropertiesAtParameter))] User user)
        {
            return user;
        }

        public User EchoUserUsingServices([Bind(typeof(ExcludeUserPropertiesUsingService))] User user)
        {
            return user;
        }

        public Dictionary<string, string>
           UpdateUserId_BlackListingAtEitherLevelDoesNotBind(
            [Bind(typeof(ExcludeLastName))] User2 param1,
            [Bind("Id")] User2 param2)
        {
            return new Dictionary<string, string>()
            {
                // LastName is excluded at parameter level.
                { "param1.LastName", param1.LastName },

                // Id is excluded because it is not explicitly included by the bind attribute at type level.
                { "param2.Id", param2.Id.ToString() },
            };
        }

        public Dictionary<string, string> UpdateFirstName_IncludingAtBothLevelBinds(
            [Bind("FirstName")] User2 param1)
        {
            return new Dictionary<string, string>()
            {
                // The since FirstName is included at both level it is bound.
                { "param1.FirstName", param1.FirstName },
            };
        }

        public Dictionary<string, string> UpdateIsAdmin_IncludingAtOnlyOneLevelDoesNotBind(
          [Bind("IsAdmin" )] User2 param1)
        {
            return new Dictionary<string, string>()
            {
                // IsAdmin is not included because it is not explicitly included at type level.
                { "param1.IsAdmin", param1.IsAdmin.ToString() },

                // FirstName is not included because it is not explicitly included at parameter level.
                { "param1.FirstName", param1.FirstName },
            };
        }

        public string BindParameterUsingParameterPrefix([Bind(Prefix = "randomPrefix")] ParameterPrefix param)
        {
            return param.Value;
        }

        public string TypePrefixIsUsed([Bind] TypePrefix param)
        {
            return param.Value;
        }

        private class ExcludeUserPropertiesAtParameter : DefaultPropertyBindingPredicateProvider<User>
        {
            public override string Prefix
            {
                get
                {
                    return "user";
                }
            }

            public override IEnumerable<Expression<Func<User, object>>> PropertyIncludeExpressions
            {
                get
                {
                    yield return m => m.RegisterationMonth;
                    yield return m => m.UserName;
                }
            }
        }

        private class ExcludeUserPropertiesUsingService : ExcludeUserPropertiesAtParameter
        {
            private ITestService _testService;

            public ExcludeUserPropertiesUsingService(ITestService testService)
            {
                _testService = testService;
            }

            public override IEnumerable<Expression<Func<User, object>>> PropertyIncludeExpressions
            {
                get
                {
                    if (_testService.Test())
                    {
                        return base.PropertyIncludeExpressions;
                    }

                    return null;
                }
            }
        }
    }

    [Bind(Prefix = "TypePrefix")]
    public class TypePrefix
    {
        public string Value { get; set; }
    }

    public class ParameterPrefix
    {
        public string Value { get; set; }
    }

    [Bind(nameof(FirstName), nameof(LastName))]
    public class User2
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool IsAdmin { get; set; }
    }

    public class ExcludeLastName : IPropertyBindingPredicateProvider
    {
        public Func<ModelBindingContext, string, bool> PropertyFilter
        {
            get
            {
                return (context, propertyName) => 
                    !string.Equals("LastName", propertyName, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}