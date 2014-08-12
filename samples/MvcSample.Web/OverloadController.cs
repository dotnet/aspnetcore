using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using MvcSample.Web.Models;

namespace MvcSample.Web
{
    public class OverloadController
    {
        // All results implement IActionResult so it can be safely returned.
        public IActionResult Get()
        {
            return Content("Get()");
        }

        [Overload]
        public ActionResult Get(int id)
        {
            return Content("Get(id)");
        }

        [Overload]
        public ActionResult Get(int id, string name)
        {
            return Content("Get(id, name)");
        }

        [Overload]
        public ActionResult Get(string bleh)
        {
            return Content("Get(bleh)");
        }

        public ActionResult WithUser()
        {
            return Content("WithUser()");
        }

        // Called for all posts regardless of values provided
        [HttpPost]
        public ActionResult WithUser(User user)
        {
            return Content("WithUser(User)");
        }

        public ActionResult WithUser(int projectId, User user)
        {
            return Content("WithUser(int, User)");
        }

        private ContentResult Content(string content)
        {
            var result = new ContentResult
            {
                Content = content,
            };

            return result;
        }

        private class OverloadAttribute : Attribute, IActionConstraint
        {
            public int Order { get; } = Int32.MaxValue;

            public bool Accept(ActionConstraintContext context)
            {
                var candidates = context.Candidates.Select(a => new
                {
                    Action = a,
                    Parameters = GetOverloadableParameters(a.Action),
                });

                var valueProviderFactory = context.RouteContext.HttpContext.RequestServices
                    .GetService<ICompositeValueProviderFactory>();

                var factoryContext = new ValueProviderFactoryContext(
                    context.RouteContext.HttpContext, 
                    context.RouteContext.RouteData.Values);
                var valueProvider = valueProviderFactory.GetValueProvider(factoryContext);

                foreach (var group in candidates.GroupBy(c => c.Parameters.Count).OrderByDescending(g => g.Key))
                {
                    var foundMatch = false;
                    foreach (var candidate in group)
                    {
                        var allFound = true;
                        foreach (var parameter in candidate.Parameters)
                        {
                            if (!(valueProvider.ContainsPrefixAsync(parameter.ParameterBindingInfo.Prefix).Result))
                            {
                                if (candidate.Action.Action == context.CurrentCandidate.Action)
                                {
                                    return false;
                                }

                                allFound = false;
                                break;
                            }
                        }

                        if (allFound)
                        {
                            foundMatch = true;
                        }
                    }

                    if (foundMatch)
                    {
                        return group.Any(c => c.Action.Action == context.CurrentCandidate.Action);
                    }
                }

                return false;
            }

            private List<ParameterDescriptor> GetOverloadableParameters(ActionDescriptor action)
            {
                if (action.Parameters == null)
                {
                    return new List<ParameterDescriptor>();
                }

                return action.Parameters.Where(
                    p => 
                        p.ParameterBindingInfo != null && 
                        !p.IsOptional && 
                        ValueProviderResult.CanConvertFromString(p.ParameterBindingInfo.ParameterType))
                    .ToList();
            }
        }
    }
}
