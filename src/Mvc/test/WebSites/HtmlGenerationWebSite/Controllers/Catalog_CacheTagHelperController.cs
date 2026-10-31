// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using HtmlGenerationWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace HtmlGenerationWebSite.Controllers;

public class Catalog_CacheTagHelperController : Controller
{
    [HttpGet("/catalog")]
    public IActionResult Splash(int categoryId, int correlationId, [FromHeader] string locale)
    {
        var category = categoryId == 1 ? "Laptops" : "Phones";
        ViewData["Category"] = category;
        ViewData["Locale"] = locale;
        ViewData["CorrelationId"] = correlationId;

        return View();
    }

    [HttpGet("/catalog/{id:int}")]
    public IActionResult Details(int id)
    {
        ViewData["ProductId"] = id;
        return View();
    }

    [HttpGet("/catalog/cart")]
    public IActionResult ShoppingCart(int correlationId)
    {
        ViewData["CorrelationId"] = correlationId;
        return View();
    }

    [HttpGet("/catalog/{region}/confirm-payment")]
    public IActionResult GuestConfirmPayment(string region, int confirmationId = 0)
    {
        ViewData["Message"] = "Welcome Guest. Your confirmation id is " + confirmationId;
        ViewData["Region"] = region;
        return View("ConfirmPayment");
    }

    [HttpGet("/catalog/{region}/{section}/confirm-payment")]
    public IActionResult ConfirmPayment(string region, string section, int confirmationId)
    {
        var message = "Welcome " + section + " member. Your confirmation id is " + confirmationId;
        ViewData["Message"] = message;
        ViewData["Region"] = region;

        return View();
    }

    [HttpGet("/catalog/past-purchases/{id}")]
    public IActionResult PastPurchases(string id, int correlationId)
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, id));

        HttpContext.User = new ClaimsPrincipal(identity);
        ViewData["CorrelationId"] = correlationId;
        return View();
    }

    [HttpGet("/categories/{category}")]
    public IActionResult ListCategories(string category, int correlationId)
    {
        ViewData["Category"] = category;
        ViewData["CorrelationId"] = correlationId;

        return View();
    }

    [HttpPost("/categories/{category}")]
    public IActionResult UpdateProducts(
        [FromServices] ProductsService productService,
        string category,
        [FromBody] List<Product> products)
    {
        productService.UpdateProducts(category, products);
        return Ok();
    }

    [HttpGet("/catalog/GetDealPercentage/{dealPercentage}")]
    public IActionResult Deals(int dealPercentage, bool isEnabled)
    {
        ViewBag.ProductDealPercentage = dealPercentage;
        ViewBag.IsEnabled = isEnabled;
        return View();
    }
}
