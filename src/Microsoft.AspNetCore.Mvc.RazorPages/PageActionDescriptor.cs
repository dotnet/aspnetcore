// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    [DebuggerDisplay("{DebuggerDisplayString,nq}")]
    public class PageActionDescriptor : ActionDescriptor
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PageActionDescriptor"/>.
        /// </summary>
        public PageActionDescriptor()
        {
        }

        /// <summary>
        /// A copy constructor for <see cref="PageActionDescriptor"/>.
        /// </summary>
        /// <param name="other">The <see cref="PageActionDescriptor"/> to copy from.</param>
        public PageActionDescriptor(PageActionDescriptor other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            RelativePath = other.RelativePath;
            ViewEnginePath = other.ViewEnginePath;
            AreaName = other.AreaName;
        }

        /// <summary>
        /// Gets or sets the application root relative path for the page.
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or sets the path relative to the base path for page discovery.
        /// <para>
        /// This value is the path of the file without extension, relative to the pages root directory.
        /// e.g. the <see cref="ViewEnginePath"/> for the file /Pages/Catalog/Antiques.cshtml is <c>/Catalog/Antiques</c>
        /// </para>
        /// <para>
        /// In an area, this value is the path of the file without extension, relative to the pages root directory for the specified area.
        /// e.g. the <see cref="ViewEnginePath"/>  for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage/Accounts</c>.
        /// </para>
        /// </summary>
        public string ViewEnginePath { get; set; }

        /// <summary>
        /// Gets or sets the area name for this page.
        /// This value will be <c>null</c> for non-area pages.
        /// </summary>
        public string AreaName { get; set; }

        /// <inheritdoc />
        public override string DisplayName
        {
            get
            {
                if (base.DisplayName == null && ViewEnginePath != null)
                {
                    base.DisplayName = ViewEnginePath;
                }

                return base.DisplayName;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                base.DisplayName = value;
            }
        }

        private string DebuggerDisplayString => $"{{ViewEnginePath = {nameof(ViewEnginePath)}, RelativePath = {nameof(RelativePath)}}}";
    }
}