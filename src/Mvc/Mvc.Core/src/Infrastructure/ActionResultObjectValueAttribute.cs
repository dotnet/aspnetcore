// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Attribute annoted on ActionResult constructor, helper method parameters, and properties to indicate
    /// that the parameter or property is used to set the "value" for ActionResult.
    /// <para>
    /// Analyzers match this parameter by type name. This allows users to annotate custom results \ custom helpers
    /// with a user defined attribute without having to expose this type.
    /// </para>
    /// <para>
    /// This attribute is intentionally marked Inherited=false since the analyzer does not walk the inheritance graph.
    /// </para>
    /// </summary>
    /// <example>
    /// BadObjectResult([ActionResultObjectValueAttribute] object value)
    /// ObjectResult { [ActionResultObjectValueAttribute] public object Value { get; set; } }
    /// </example>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ActionResultObjectValueAttribute : Attribute
    {
    }
}
