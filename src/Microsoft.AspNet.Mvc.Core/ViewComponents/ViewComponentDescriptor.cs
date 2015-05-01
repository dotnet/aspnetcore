// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    /// <summary>
    /// A descriptor for a View Component.
    /// </summary>
    public class ViewComponentDescriptor
    {
        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The full name is defaulted to the full namespace of the View Component class, prepended to
        /// the the class name with a '.' character as the separator. If the View Component class uses
        /// <code>ViewComponent</code> as a suffix, the suffix will be omitted from the <see cref="FullName"/>.
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
        /// Gets or sets the short name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The short name is defaulted to the name of the View Component class. If the View Component class uses
        /// <code>ViewComponent</code> as a suffix, the suffix will be omitted from the <see cref="ShortName"/>.
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
        /// Gets or sets the <see cref="Type"/>.
        /// </summary>
        public Type Type { get; set; }
    }
}