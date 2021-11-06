// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

// This controller contains actions mapped with a single controller-level route.
[Route("Blog/[action]/{postId?}")]
public class BlogController
{
    private readonly TestResponseGenerator _generator;

    public BlogController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    public IActionResult ShowPosts()
    {
        return _generator.Generate("/Blog/ShowPosts");
    }

    public IActionResult Edit(int postId)
    {
        return _generator.Generate("/Blog/Edit/" + postId);
    }
}
