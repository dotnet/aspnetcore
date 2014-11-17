// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of an <see cref="ApiExplorerModel"/>.
    /// </summary>
    public class ApiExplorerModelValues : LoggerStructureBase
    {
        public ApiExplorerModelValues(ApiExplorerModel inner)
        {
            if (inner != null)
            {
                IsVisible = inner.IsVisible;
                GroupName = inner.GroupName;
            }
        }

        /// <summary>
        /// See <see cref="ApiExplorerModel.IsVisible"/>.
        /// </summary>
        public bool? IsVisible { get; }

        /// <summary>
        /// See <see cref="ApiExplorerModel.GroupName"/>.
        /// </summary>
        public string GroupName { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}