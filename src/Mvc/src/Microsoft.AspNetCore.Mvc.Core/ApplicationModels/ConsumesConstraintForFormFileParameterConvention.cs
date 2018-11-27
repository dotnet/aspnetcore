// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// An <see cref="IActionModelConvention"/> that adds a <see cref="ConsumesAttribute"/> with <c>multipart/form-data</c>
    /// to controllers containing form file (<see cref="BindingSource.FormFile"/>) parameters.
    /// </summary>
    public class ConsumesConstraintForFormFileParameterConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (!ShouldApply(action))
            {
                return;
            }

            AddMultipartFormDataConsumesAttribute(action);
        }

        protected virtual bool ShouldApply(ActionModel action) => true;

        // Internal for unit testing
        internal void AddMultipartFormDataConsumesAttribute(ActionModel action)
        {
            // Add a ConsumesAttribute if the request does not explicitly specify one.
            if (action.Filters.OfType<IConsumesActionConstraint>().Any())
            {
                return;
            }

            foreach (var parameter in action.Parameters)
            {
                var bindingSource = parameter.BindingInfo?.BindingSource;
                if (bindingSource == BindingSource.FormFile)
                {
                    // If an controller accepts files, it must accept multipart/form-data.
                    action.Filters.Add(new ConsumesAttribute("multipart/form-data"));
                    return;
                }
            }
        }
    }
}
