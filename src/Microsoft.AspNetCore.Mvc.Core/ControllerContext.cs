// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// The context associated with the current request for a controller.
    /// </summary>
    public class ControllerContext : ActionContext
    {
        private IList<IValueProvider> _valueProviders;

        /// <summary>
        /// Creates a new <see cref="ControllerContext"/>.
        /// </summary>
        /// <remarks>
        /// The default constructor is provided for unit test purposes only.
        /// </remarks>
        public ControllerContext()
            : base()
        {
        }

        /// <summary>
        /// Creates a new <see cref="ControllerContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/> associated with the current request.</param>
        public ControllerContext(ActionContext context)
            : base(context)
        {
            if (!(context.ActionDescriptor is ControllerActionDescriptor))
            {
                throw new ArgumentException(Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                    typeof(ControllerActionDescriptor)),
                    nameof(context));
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="ControllerActionDescriptor"/> associated with the current request.
        /// </summary>
        public new ControllerActionDescriptor ActionDescriptor
        {
            get { return (ControllerActionDescriptor)base.ActionDescriptor; }
            set { base.ActionDescriptor = value; }
        }

        /// <summary>
        /// Gets or sets the list of <see cref="IValueProvider"/> instances for the current request.
        /// </summary>
        public virtual IList<IValueProvider> ValueProviders
        {
            get
            {
                if (_valueProviders == null)
                {
                    _valueProviders = new List<IValueProvider>();
                }

                return _valueProviders;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _valueProviders = value;
            }
        }
    }
}
