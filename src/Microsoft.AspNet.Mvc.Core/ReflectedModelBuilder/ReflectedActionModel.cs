// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc.ReflectedModelBuilder
{
    public class ReflectedActionModel
    {
        public ReflectedActionModel([NotNull] MethodInfo actionMethod)
        {
            ActionMethod = actionMethod;

            // CoreCLR returns IEnumerable<Attribute> from GetCustomAttributes - the OfType<object>
            // is needed to so that the result of ToList() is List<object>
            Attributes = actionMethod.GetCustomAttributes(inherit: true).OfType<object>().ToList();

            Filters = Attributes
                .OfType<IFilter>()
                .ToList();

            var routeTemplateAttribute = Attributes.OfType<IRouteTemplateProvider>().FirstOrDefault();
            if (routeTemplateAttribute != null)
            {
                AttributeRouteModel = new ReflectedAttributeRouteModel(routeTemplateAttribute);
            }

            var apiExplorerNameAttribute = Attributes.OfType<IApiDescriptionGroupNameProvider>().FirstOrDefault();
            if (apiExplorerNameAttribute != null)
            {
                ApiExplorerGroupName = apiExplorerNameAttribute.GroupName;
            }

            var apiExplorerVisibilityAttribute = Attributes.OfType<IApiDescriptionVisibilityProvider>().FirstOrDefault();
            if (apiExplorerVisibilityAttribute != null)
            {
                ApiExplorerIsVisible = !apiExplorerVisibilityAttribute.IgnoreApi;
            }

            HttpMethods = new List<string>();
            Parameters = new List<ReflectedParameterModel>();
        }

        public MethodInfo ActionMethod { get; private set; }

        public string ActionName { get; set; }

        public List<object> Attributes { get; private set; }

        public ReflectedControllerModel Controller { get; set; }

        public List<IFilter> Filters { get; private set; }

        public List<string> HttpMethods { get; private set; }

        public bool IsActionNameMatchRequired { get; set; }

        public List<ReflectedParameterModel> Parameters { get; private set; }

        public ReflectedAttributeRouteModel AttributeRouteModel { get; set; }

        /// <summary>
        /// If <c>true</c>, <see cref="ApiDescription"/> objects will be created for this action. If <c>null</c>
        /// then the value of <see cref="ReflectedControllerModel.ApiExplorerIsVisible"/> will be used.
        /// </summary>
        public bool? ApiExplorerIsVisible { get; set; }

        /// <summary>
        /// The value for <see cref="ApiDescription.GroupName"/> of <see cref="ApiDescription"/> objects created
        /// for actions defined by this controller. If <c>null</c> then the value of 
        /// <see cref="ReflectedControllerModel.ApiExplorerGroupName"/> will be used.
        /// </summary>
        public string ApiExplorerGroupName { get; set; }
    }
}
