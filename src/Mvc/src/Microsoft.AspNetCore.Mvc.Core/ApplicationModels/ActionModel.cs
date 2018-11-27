// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    [DebuggerDisplay("{DisplayName}")]
    public class ActionModel : ICommonModel, IFilterModel, IApiExplorerModel
    {
        public ActionModel(
            MethodInfo actionMethod,
            IReadOnlyList<object> attributes)
        {
            if (actionMethod == null)
            {
                throw new ArgumentNullException(nameof(actionMethod));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            ActionMethod = actionMethod;

            ApiExplorer = new ApiExplorerModel();
            Attributes = new List<object>(attributes);
            Filters = new List<IFilterMetadata>();
            Parameters = new List<ParameterModel>();
            RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Properties = new Dictionary<object, object>();
            Selectors = new List<SelectorModel>();
        }

        public ActionModel(ActionModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            ActionMethod = other.ActionMethod;
            ActionName = other.ActionName;
            RouteParameterTransformer = other.RouteParameterTransformer;

            // Not making a deep copy of the controller, this action still belongs to the same controller.
            Controller = other.Controller;

            // These are just metadata, safe to create new collections
            Attributes = new List<object>(other.Attributes);
            Filters = new List<IFilterMetadata>(other.Filters);
            Properties = new Dictionary<object, object>(other.Properties);
            RouteValues = new Dictionary<string, string>(other.RouteValues, StringComparer.OrdinalIgnoreCase);

            // Make a deep copy of other 'model' types.
            ApiExplorer = new ApiExplorerModel(other.ApiExplorer);
            Parameters = new List<ParameterModel>(other.Parameters.Select(p => new ParameterModel(p) { Action = this }));
            Selectors = new List<SelectorModel>(other.Selectors.Select(s => new SelectorModel(s)));
        }

        public MethodInfo ActionMethod { get; }

        public string ActionName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ApiExplorerModel"/> for this action.
        /// </summary>
        /// <remarks>
        /// <see cref="ActionModel.ApiExplorer"/> allows configuration of settings for ApiExplorer
        /// which apply to the action.
        ///
        /// Settings applied by <see cref="ActionModel.ApiExplorer"/> override settings from
        /// <see cref="ApplicationModel.ApiExplorer"/> and <see cref="ControllerModel.ApiExplorer"/>.
        /// </remarks>
        public ApiExplorerModel ApiExplorer { get; set; }

        public IReadOnlyList<object> Attributes { get; }

        /// <summary>
        /// Gets or sets the <see cref="ControllerModel"/>.
        /// </summary>
        public ControllerModel Controller { get; set; }

        public IList<IFilterMetadata> Filters { get; }

        public IList<ParameterModel> Parameters { get; }

        /// <summary>
        /// Gets or sets an <see cref="IOutboundParameterTransformer"/> that will be used to transform 
        /// built-in route parameters such as <c>action</c>, <c>controller</c>, and <c>area</c> as well as
        /// additional parameters specified by <see cref="RouteValues"/> into static segments in the route template.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This feature only applies when using endpoint routing.
        /// </para>
        /// </remarks>
        public IOutboundParameterTransformer RouteParameterTransformer { get; set; }

        /// <summary>
        /// Gets a collection of route values that must be present in the 
        /// <see cref="RouteData.Values"/> for the corresponding action to be selected.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The value of <see cref="ActionName"/> is considered an implicit route value corresponding
        /// to the key <c>action</c> and the value of <see cref="ControllerModel.ControllerName"/> is
        /// considered an implicit route value corresponding to the key <c>controller</c>. These entries
        /// will be implicitly added to <see cref="ActionDescriptor.RouteValues"/> when the action
        /// descriptor is created, but will not be visible in <see cref="RouteValues"/>.
        /// </para>
        /// <para>
        /// Entries in <see cref="RouteValues"/> can override entries in
        /// <see cref="ControllerModel.RouteValues"/>.
        /// </para>
        /// </remarks>
        public IDictionary<string, string> RouteValues { get; }

        /// <summary>
        /// Gets a set of properties associated with the action.
        /// These properties will be copied to <see cref="Abstractions.ActionDescriptor.Properties"/>.
        /// </summary>
        /// <remarks>
        /// Entries will take precedence over entries with the same key in
        /// <see cref="ApplicationModel.Properties"/> and <see cref="ControllerModel.Properties"/>.
        /// </remarks>
        public IDictionary<object, object> Properties { get; }

        MemberInfo ICommonModel.MemberInfo => ActionMethod;

        string ICommonModel.Name => ActionName;

        /// <summary>
        /// Gets the <see cref="SelectorModel"/> instances.
        /// </summary>
        public IList<SelectorModel> Selectors { get; }

        public string DisplayName
        {
            get
            {
                if (Controller == null)
                {
                    return ActionMethod.Name;
                }

                var controllerType = TypeNameHelper.GetTypeDisplayName(Controller.ControllerType);
                var controllerAssembly = Controller?.ControllerType.Assembly.GetName().Name;
                return $"{controllerType}.{ActionMethod.Name} ({controllerAssembly})";
            }
        }
    }
}
