// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Keys passed to <see cref="ModelBinderAttribute"/> constructors that indicate the value of the
    /// <see cref="ModelBinderAttribute.BindingSource"/> property.
    /// </summary>
    public enum BindingSourceKey
    {
        /// <summary>
        /// Key for <see cref="BindingSource.Body"/>. This <see cref="BindingSource"/> indicates value should be bound
        /// from the request body.
        /// </summary>
        Body,
        /// <summary>
        /// Key for <see cref="BindingSource.Custom"/>. This <see cref="BindingSource"/> indicates value should be
        /// bound from an unknown data source related to the current request and has
        /// <see cref="BindingSource.IsFromRequest"/> <see langword="true"/> and <see cref="BindingSource.IsGreedy"/>
        /// <see langword="true"/>.
        /// </summary>
        Custom,
        /// <summary>
        /// Key for <see cref="BindingSource.Form"/>. This <see cref="BindingSource"/> indicates value should be bound
        /// from request form data.
        /// </summary>
        Form,
        /// <summary>
        /// Key for <see cref="BindingSource.FormFile"/>. This <see cref="BindingSource"/> indicates value should be
        /// bound from one or more request file attachments.
        /// </summary>
        FormFile,
        /// <summary>
        /// Key for <see cref="BindingSource.Header"/>. This <see cref="BindingSource"/> indicates value should be
        /// bound from one or more request headers.
        /// </summary>
        Header,
        /// <summary>
        /// Key for <see cref="BindingSource.ModelBinding"/>. This <see cref="BindingSource"/> indicates value should
        /// be bound using the default <see cref="IValueProvider"/>s and includes all of
        /// <see cref="BindingSource.Form"/>, <see cref="BindingSource.Path"/> and <see cref="BindingSource.Query"/>.
        /// </summary>
        ModelBinding,
        /// <summary>
        /// Key for <see cref="BindingSource.Path"/>. This <see cref="BindingSource"/> indicates value should be bound
        /// from the request URL's path, also known as route values.
        /// </summary>
        Path,
        /// <summary>
        /// Key for <see cref="BindingSource.Query"/>. This <see cref="BindingSource"/> indicates value should be bound
        /// from the request URL's query string.
        /// </summary>
        Query,
        /// <summary>
        /// Key for <see cref="BindingSource.Services"/>. This <see cref="BindingSource"/> indicates value should be
        /// bound from services, either application or per-request services.
        /// </summary>
        Services,
        /// <summary>
        /// Key for <see cref="BindingSource.Special"/>. This <see cref="BindingSource"/> indicates value should be
        /// bound from an unknown data source and has <see cref="BindingSource.IsFromRequest"/> <see langword="false"/>
        /// and <see cref="BindingSource.IsGreedy"/> <see langword="true"/>.
        /// </summary>
        Special
    }
}
