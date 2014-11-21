// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents information used by an input formatter for
    /// deserializing the request body into an object.
    /// </summary>
    public class InputFormatterContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="InputFormatterContext"/>.
        /// </summary>
        public InputFormatterContext([NotNull] ActionContext actionContext,
                                     [NotNull] Type modelType)
        {
            ActionContext = actionContext;
            ModelType = modelType;
        }

        /// <summary>
        /// Action context associated with the current call.
        /// </summary>
        public ActionContext ActionContext { get; private set; }

        /// <summary>
        /// Represents the expected type of the model represented by the request body.
        /// </summary>
        public Type ModelType { get; private set; }
    }
}
