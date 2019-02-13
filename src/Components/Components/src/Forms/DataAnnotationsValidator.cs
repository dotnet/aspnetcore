// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Adds Data Annotations validation support to an <see cref="EditContext"/>.
    /// </summary>
    public class DataAnnotationsValidator : ComponentBase
    {
        [CascadingParameter] EditContext EditContext { get; set; }

        /// <inheritdoc />
        protected override void OnInit()
        {
            if (EditContext == null)
            {
                throw new InvalidOperationException($"{nameof(DataAnnotationsValidator)} requires a cascading " +
                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(DataAnnotationsValidator)} " +
                    $"inside an {nameof(EditForm)}.");
            }

            EditContext.AddDataAnnotationsValidation();
        }
    }
}
