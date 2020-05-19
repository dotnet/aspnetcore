// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// A descriptor for a view component.
    /// </summary>
    [DebuggerDisplay("{DisplayName}")]
    public class ViewComponentDescriptor
    {
        private string _displayName;

        /// <summary>
        /// Creates a new <see cref="ViewComponentDescriptor"/>.
        /// </summary>
        public ViewComponentDescriptor()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Gets or sets the display name of the view component.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (_displayName == null)
                {
                    _displayName = TypeInfo?.FullName;
                }

                return _displayName;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _displayName = value;
            }
        }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The full name is defaulted to the full namespace of the view component class, prepended to
        /// the class name with a '.' character as the separator. If the view component class uses
        /// <c>ViewComponent</c> as a suffix, the suffix will be omitted from the <see cref="FullName"/>.
        /// </para>
        /// <example>
        ///     Class Name: Contoso.Products.LoginViewComponent
        ///     View Component FullName: Contoso.Products.Login
        /// </example>
        /// <example>
        ///     Class Name: Contoso.Blog.Tags
        ///     View Component FullName: Contoso.Blog.Tags
        /// </example>
        /// <para>
        /// If <see cref="ViewComponentAttribute.Name"/> is used to set a name, then this will be used as
        /// the <see cref="FullName"/>.
        /// </para>
        /// <example>
        ///     [ViewComponent(Name = "Contoso.Forum.UsersOnline")]
        ///     public class OnlineUsersViewComponent
        ///     {
        ///     }
        ///     View Component FullName: Contoso.Forum.UsersOnline
        /// </example>
        /// </remarks>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or set the generated unique identifier for this <see cref="ViewComponentDescriptor"/>.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the short name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The short name is defaulted to the name of the view component class. If the view component class uses
        /// <c>ViewComponent</c> as a suffix, the suffix will be omitted from the <see cref="ShortName"/>.
        /// </para>
        /// <example>
        ///     Class Name: Contoso.Products.LoginViewComponent
        ///     View Component ShortName: Login
        /// </example>
        /// <example>
        ///     Class Name: Contoso.Blog.Tags
        ///     View Component ShortName: Tags
        /// </example>
        /// <para>
        /// If <see cref="ViewComponentAttribute.Name"/> is used to set a name, then the last segment of the 
        /// value (using '.' as a separate) will be used as the <see cref="ShortName"/>.
        /// </para>
        /// <example>
        ///     [ViewComponent(Name = "Contoso.Forum.UsersOnline")]
        ///     public class OnlineUsersViewComponent
        ///     {
        ///     }
        ///     View Component ShortName: UsersOnline
        /// </example>
        /// </remarks>
        public string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="System.Reflection.TypeInfo"/>.
        /// </summary>
        public TypeInfo TypeInfo { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="System.Reflection.MethodInfo"/> to invoke.
        /// </summary>
        public MethodInfo MethodInfo { get; set; }

        /// <summary>
        /// Gets or sets the parameters associated with the method described by <see cref="MethodInfo"/>.
        /// </summary>
        public IReadOnlyList<ParameterInfo> Parameters { get; set; }
    }
}