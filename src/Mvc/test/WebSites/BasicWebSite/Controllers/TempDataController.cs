// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BasicWebSite.Filters;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class TempDataController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult DisplayTempData(string value)
    {
        TempData["key"] = value;
        return View();
    }

    public IActionResult SetTempData(string value)
    {
        TempData["key"] = value;
        return Content(value);
    }

    public IActionResult GetTempDataAndRedirect()
    {
        var value = TempData["key"];
        return RedirectToAction("GetTempData");
    }

    public string GetTempData()
    {
        var value = TempData["key"];
        return value?.ToString();
    }

    public IActionResult PeekTempData()
    {
        var peekValue = TempData.Peek("key");
        return Content(peekValue.ToString());
    }

    public IActionResult SetTempDataMultiple(
        string value,
        int intValue,
        IList<string> listValues,
        DateTime datetimeValue,
        Guid guidValue)
    {
        TempData["key1"] = value;
        TempData["key2"] = intValue;
        TempData["key3"] = listValues;
        TempData["key4"] = datetimeValue;
        TempData["key5"] = guidValue;
        return RedirectToAction("GetTempDataMultiple");
    }

    public async Task SetTempDataResponseWrite()
    {
        TempData["value1"] = "steve";

        await Response.WriteAsync("Steve!");
    }

    public string GetTempDataMultiple()
    {
        var value1 = TempData["key1"].ToString();
        var value2 = Convert.ToInt32(TempData["key2"], CultureInfo.InvariantCulture);
        var value3 = (IList<string>)TempData["key3"];
        var value4 = (DateTime)TempData["key4"];
        var value5 = (Guid)TempData["key5"];
        return $"{value1} {value2} {value3.Count} {value4} {value5}";
    }

    [HttpGet]
    public IActionResult SetTempDataInActionResult()
    {
        return new StoreIntoTempDataActionResult();
    }

    [HttpGet]
    public string GetTempDataSetInActionResult()
    {
        return TempData["Name"]?.ToString();
    }

    [HttpGet]
    public IActionResult SetLargeValueInTempData(int size, char character)
    {
        TempData["LargeValue"] = new string(character, size);
        return Ok();
    }

    [HttpGet]
    public string GetLargeValueFromTempData()
    {
        return TempData["LargeValue"]?.ToString();
    }

    [HttpGet]
    [TestExceptionFilter]
    public IActionResult UnhandledExceptionAndSettingTempData()
    {
        TempData[nameof(UnhandledExceptionAndSettingTempData)] = "James";
        throw new InvalidOperationException($"Exception from action {nameof(UnhandledExceptionAndSettingTempData)}");
    }

    [HttpGet]
    public string UnhandledExceptionAndGetTempData()
    {
        return TempData[nameof(UnhandledExceptionAndSettingTempData)]?.ToString();
    }

    [HttpGet]
    public void GrantConsent()
    {
        HttpContext.Features.Get<ITrackingConsentFeature>().GrantConsent();
    }
}
