// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite;

[Route("ApiExplorerApiController/[action]")]
[ApiController]
[EndpointGroupName("GroupNameOnController")]
public class ApiExplorerApiController : Controller
{
    public IActionResult ActionWithoutParameters() => Ok();

    [EndpointGroupName("GroupNameOnAction")]
    public void ActionWithSomeParameters(object input)
    {
    }

    public void ActionWithIdParameter(int id, string name)
    {
    }

    public void ActionWithIdSuffixParameter(int personId, string personName)
    {
    }

    public void ActionWithFormFileCollectionParameter(IFormFileCollection formFile)
    {
    }

    [Produces("application/pdf", Type = typeof(Stream))]
    public IActionResult ProducesWithUnsupportedContentType() => null;
}
