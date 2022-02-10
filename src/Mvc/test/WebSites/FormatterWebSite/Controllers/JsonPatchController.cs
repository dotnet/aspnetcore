// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers;

[Route("jsonpatch/[action]")]
public class JsonPatchController : Controller
{
    [HttpPatch]
    public IActionResult PatchProduct([FromBody] JsonPatchDocument<Product> patchDoc)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var product = CreateProduct();
        patchDoc.ApplyTo(product, ModelState);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Ok(product);
    }

    private Product CreateProduct()
    {
        return new Product
        {
            Name = "Book1",
            Reviews = new List<Review>()
                {
                    new Review
                    {
                         Rating = 4
                    },
                    new Review
                    {
                        Rating = 3
                    }
                }
        };
    }
}
