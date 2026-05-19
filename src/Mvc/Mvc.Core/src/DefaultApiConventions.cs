// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Default api conventions.
/// </summary>
public static class DefaultApiConventions
{
    #region GET
    /// <summary>
    /// Get convention.
    /// </summary>
    /// <param name="id"></param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Get(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id)
    { }

    /// <summary>
    /// Find convention.
    /// </summary>
    /// <param name="id"></param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Find(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id)
    { }
    #endregion

    /// <summary>
    /// Post convention.
    /// </summary>
    /// <param name="model"></param>
    #region POST
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Post(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object model)
    { }

    /// <summary>
    /// Create convention.
    /// </summary>
    /// <param name="model"></param>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Create(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object model)
    { }
    #endregion

    #region PUT
    /// <summary>
    /// Put convention.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Put(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id,

        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object model)
    { }

    /// <summary>
    /// Edit convention.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Edit(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id,

        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object model)
    { }

    /// <summary>
    /// Update convention.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Update(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id,

        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object model)
    { }
    #endregion

    #region DELETE
    /// <summary>
    /// Delete convention.
    /// </summary>
    /// <param name="id"></param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Delete(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id)
    { }
    #endregion
}
