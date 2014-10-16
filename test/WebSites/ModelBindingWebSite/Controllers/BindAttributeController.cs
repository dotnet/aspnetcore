// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Controllers
{
    public class BindAttributeController : Controller
    {
        public Dictionary<string, string> 
           BindAtParamterLevelAndBindAtTypeLevelAreBothEvaluated_BlackListingAtEitherLevelDoesNotBind(
            [Bind(Exclude = "IncludedExplicitlyAtTypeLevel")] TypeWithIncludedPropertyAtBindAttribute param1,
            [Bind(Include = "ExcludedExplicitlyAtTypeLevel")] TypeWithExcludedPropertyAtBindAttribute param2)
        {
            return new Dictionary<string, string>()
            { 
                // The first one should not be included because the parameter level bind attribute filters it out.
                { "param1.IncludedExplicitlyAtTypeLevel", param1.IncludedExplicitlyAtTypeLevel },

                // The second one should not be included because the type level bind attribute filters it out.
                { "param2.ExcludedExplicitlyAtTypeLevel", param2.ExcludedExplicitlyAtTypeLevel },
            };
        }

        public Dictionary<string, string> 
          BindAtParamterLevelAndBindAtTypeLevelAreBothEvaluated_WhiteListingAtBothLevelBinds(
          [Bind(Include = "IncludedExplicitlyAtTypeLevel")] TypeWithIncludedPropertyAtBindAttribute param1)
        {
            return new Dictionary<string, string>()
            {
                // The since this is included at both level it is bound.
                { "param1.IncludedExplicitlyAtTypeLevel", param1.IncludedExplicitlyAtTypeLevel },
            };
        }

        public Dictionary<string, string> 
          BindAtParamterLevelAndBindAtTypeLevelAreBothEvaluated_WhiteListingAtOnlyOneLevelDoesNotBind(
          [Bind(Include = "IncludedExplicitlyAtParameterLevel")]
          TypeWithIncludedPropertyAtParameterAndTypeUsingBindAttribute param1)
        {
            return new Dictionary<string, string>()
            {
                // The since this is included at only type level it is not bound.
                { "param1.IncludedExplicitlyAtParameterLevel", param1.IncludedExplicitlyAtParameterLevel },
                { "param1.IncludedExplicitlyAtTypeLevel", param1.IncludedExplicitlyAtTypeLevel },
            };
        }

        public string BindParameterUsingParameterPrefix([Bind(Prefix = "randomPrefix")] ParameterPrefix param)
        {
            return param.Value;
        }

        // This will use param to try to bind and not the value specified at TypePrefix.
        public string TypePrefixIsNeverUsed([Bind] TypePrefix param)
        {
            return param.Value;
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

    [Bind(Include = nameof(IncludedExplicitlyAtTypeLevel))]
    public class TypeWithIncludedPropertyAtParameterAndTypeUsingBindAttribute
    {
        public string IncludedExplicitlyAtTypeLevel { get; set; }
        public string IncludedExplicitlyAtParameterLevel { get; set; }
    }

    [Bind(Include = nameof(IncludedExplicitlyAtTypeLevel))]
    public class TypeWithIncludedPropertyAtBindAttribute
    {
        public string IncludedExplicitlyAtTypeLevel { get; set; }
    }

    [Bind(Exclude = nameof(ExcludedExplicitlyAtTypeLevel))]
    public class TypeWithExcludedPropertyAtBindAttribute
    {
        public string ExcludedExplicitlyAtTypeLevel { get; set; }
    }
}