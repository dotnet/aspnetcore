// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Web.Mvc.Properties;
using System.Web.Routing;

namespace System.Web.Mvc
{
    [AttributeUsage(AttributeTargets.Property)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "The constructor parameters are used to feed RouteData, which is public.")]
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "This attribute is designed to be a base class for other attributes.")]
    public class RemoteAttribute : ValidationAttribute, IClientValidatable
    {
        private string _additionalFields;
        private string[] _additonalFieldsSplit = new string[0];

        protected RemoteAttribute()
            : base(MvcResources.RemoteAttribute_RemoteValidationFailed)
        {
            RouteData = new RouteValueDictionary();
        }

        public RemoteAttribute(string routeName)
            : this()
        {
            if (String.IsNullOrWhiteSpace(routeName))
            {
                throw new ArgumentException(MvcResources.Common_NullOrEmpty, "routeName");
            }

            RouteName = routeName;
        }

        public RemoteAttribute(string action, string controller)
            :
                this(action, controller, null /* areaName */)
        {
        }

        public RemoteAttribute(string action, string controller, string areaName)
            : this()
        {
            if (String.IsNullOrWhiteSpace(action))
            {
                throw new ArgumentException(MvcResources.Common_NullOrEmpty, "action");
            }
            if (String.IsNullOrWhiteSpace(controller))
            {
                throw new ArgumentException(MvcResources.Common_NullOrEmpty, "controller");
            }

            RouteData["controller"] = controller;
            RouteData["action"] = action;

            if (!String.IsNullOrWhiteSpace(areaName))
            {
                RouteData["area"] = areaName;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteAttribute"/> class.
        /// </summary>
        /// <param name="action">The route name.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="areaReference">
        /// Find the controller in the root if <see cref="AreaReference.UseRoot"/>. Otherwise look in the current area.
        /// </param>
        public RemoteAttribute(string action, string controller, AreaReference areaReference)
            : this(action, controller)
        {
            if (areaReference == AreaReference.UseRoot)
            {
                RouteData["area"] = null;
            }
        }

        public string HttpMethod { get; set; }

        public string AdditionalFields
        {
            get { return _additionalFields ?? String.Empty; }
            set
            {
                _additionalFields = value;
                _additonalFieldsSplit = AuthorizeAttribute.SplitString(value);
            }
        }

        protected RouteValueDictionary RouteData { get; private set; }

        protected string RouteName { get; set; }

        protected virtual RouteCollection Routes
        {
            get { return RouteTable.Routes; }
        }

        public string FormatAdditionalFieldsForClientValidation(string property)
        {
            if (String.IsNullOrEmpty(property))
            {
                throw new ArgumentException(MvcResources.Common_NullOrEmpty, "property");
            }

            string delimitedAdditionalFields = FormatPropertyForClientValidation(property);

            foreach (string field in _additonalFieldsSplit)
            {
                delimitedAdditionalFields += "," + FormatPropertyForClientValidation(field);
            }

            return delimitedAdditionalFields;
        }

        public static string FormatPropertyForClientValidation(string property)
        {
            if (String.IsNullOrEmpty(property))
            {
                throw new ArgumentException(MvcResources.Common_NullOrEmpty, "property");
            }
            return "*." + property;
        }

        [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Justification = "The value is a not a regular URL since it may contain ~/ ASP.NET-specific characters")]
        protected virtual string GetUrl(ControllerContext controllerContext)
        {
            var pathData = Routes.GetVirtualPathForArea(controllerContext.RequestContext,
                                                        RouteName,
                                                        RouteData);

            if (pathData == null)
            {
                throw new InvalidOperationException(MvcResources.RemoteAttribute_NoUrlFound);
            }

            return pathData.VirtualPath;
        }

        public override string FormatErrorMessage(string name)
        {
            return String.Format(CultureInfo.CurrentCulture, ErrorMessageString, name);
        }

        public override bool IsValid(object value)
        {
            return true;
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            yield return new ModelClientValidationRemoteRule(FormatErrorMessage(metadata.GetDisplayName()), GetUrl(context), HttpMethod, FormatAdditionalFieldsForClientValidation(metadata.PropertyName));
        }
    }
}
