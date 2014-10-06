// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc.ApplicationModel
{
    public class ActionModel
    {
        public ActionModel([NotNull] MethodInfo actionMethod)
        {
            ActionMethod = actionMethod;

            Attributes = new List<object>();
            ActionConstraints = new List<IActionConstraintMetadata>();
            Filters = new List<IFilter>();
            HttpMethods = new List<string>();
            Parameters = new List<ParameterModel>();
        }

        public List<IActionConstraintMetadata> ActionConstraints { get; private set; }

        public MethodInfo ActionMethod { get; private set; }

        public string ActionName { get; set; }

        public List<object> Attributes { get; private set; }

        public ControllerModel Controller { get; set; }

        public List<IFilter> Filters { get; private set; }

        public List<string> HttpMethods { get; private set; }

        public bool IsActionNameMatchRequired { get; set; }

        public List<ParameterModel> Parameters { get; private set; }

        public AttributeRouteModel AttributeRouteModel { get; set; }

        /// <summary>
        /// If <c>true</c>, <see cref="ApiDescription"/> objects will be created for this action. If <c>null</c>
        /// then the value of <see cref="ControllerModel.ApiExplorerIsVisible"/> will be used.
        /// </summary>
        public bool? ApiExplorerIsVisible { get; set; }

        /// <summary>
        /// The value for <see cref="ApiDescription.GroupName"/> of <see cref="ApiDescription"/> objects created
        /// for actions defined by this controller. If <c>null</c> then the value of 
        /// <see cref="ControllerModel.ApiExplorerGroupName"/> will be used.
        /// </summary>
        public string ApiExplorerGroupName { get; set; }
    }
}
