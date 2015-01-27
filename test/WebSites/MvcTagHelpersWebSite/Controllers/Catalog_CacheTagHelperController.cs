// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.Mvc;

namespace MvcTagHelpersWebSite.Controllers
{
    public class Catalog_CacheTagHelperController : Controller
    {
        [FromServices]
        public ProductsService ProductsService { get; set; }

        [HttpGet("/catalog")]
        public ViewResult Splash(int categoryId, int correlationId, [FromHeader]string locale)
        {
            var category = categoryId == 1 ? "Laptops" : "Phones";
            ViewData["Category"] = category;
            ViewData["Locale"] = locale;
            ViewData["CorrelationId"] = correlationId;

            return View();
        }

        [HttpGet("/catalog/{id:int}")]
        public ViewResult Details(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        [HttpGet("/catalog/cart")]
        public ViewResult ShoppingCart(int correlationId)
        {
            ViewData["CorrelationId"] = correlationId;
            return View();
        }

        [HttpGet("/catalog/{region}/confirm-payment")]
        public ViewResult GuestConfirmPayment(string region, int confirmationId = 0)
        {
            ViewData["Message"] = "Welcome Guest. Your confirmation id is " + confirmationId;
            ViewData["Region"] = region;
            return View("ConfirmPayment");
        }

        [HttpGet("/catalog/{region}/{section}/confirm-payment")]
        public ViewResult ConfirmPayment(string region, string section, int confirmationId)
        {
            var message = "Welcome " + section + " member. Your confirmation id is " + confirmationId;
            ViewData["Message"] = message;
            ViewData["Region"] = region;

            return View();
        }

        [HttpGet("/catalog/past-purchases/{id}")]
        public ViewResult PastPurchases(string id, int correlationId)
        {
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, id));

            Context.User = new ClaimsPrincipal(identity);
            ViewData["CorrelationId"] = correlationId;
            return View();
        }

        [HttpGet("/categories/{category}")]
        public ViewResult ListCategories(string category, int correlationId)
        {
            ViewData["Category"] = category;
            ViewData["CorrelationId"] = correlationId;

            return View();
        }

        [HttpPost("/categories/update-products")]
        public IActionResult UpdateCategories()
        {
            ProductsService.UpdateProducts();
            return new EmptyResult();
        }
    }
}
