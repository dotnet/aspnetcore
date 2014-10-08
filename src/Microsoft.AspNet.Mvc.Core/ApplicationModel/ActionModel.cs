// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        public ActionModel([NotNull] ActionModel other)
        {
            ActionMethod = other.ActionMethod;
            ActionName = other.ActionName;
            ApiExplorerGroupName = other.ApiExplorerGroupName;
            ApiExplorerIsVisible = other.ApiExplorerIsVisible;
            IsActionNameMatchRequired = other.IsActionNameMatchRequired;

            // Not making a deep copy of the controller, this action still belongs to the same controller.
            Controller = other.Controller;

            // These are just metadata, safe to create new collections
            ActionConstraints = new List<IActionConstraintMetadata>(other.ActionConstraints);
            Attributes = new List<object>(other.Attributes);
            Filters = new List<IFilter>(other.Filters);
            HttpMethods = new List<string>(other.HttpMethods);

            // Make a deep copy of other 'model' types.
            Parameters = new List<ParameterModel>(other.Parameters.Select(p => new ParameterModel(p)));

            if (other.AttributeRouteModel != null)
            {
                AttributeRouteModel = new AttributeRouteModel(other.AttributeRouteModel);
            }
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
        /// If <c>true</c>, <see cref="Description.ApiDescription"/> objects will be created for this action. 
        /// If <c>null</c> then the value of <see cref="ControllerModel.ApiExplorerIsVisible"/> will be used.
        /// </summary>
        public bool? ApiExplorerIsVisible { get; set; }

        /// <summary>
        /// The value for <see cref="Description.ApiDescription.GroupName"/> of 
        /// <see cref="Description.ApiDescription"/> objects created for actions defined by this controller.
        /// If <c>null</c> then the value of <see cref="ControllerModel.ApiExplorerGroupName"/> will be used.
        /// </summary>
        public string ApiExplorerGroupName { get; set; }
    }
}
