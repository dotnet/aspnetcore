// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using BasicWebSite.Formatters;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;

namespace BasicWebSite.Controllers.ContentNegotiation;

public class NormalController : Controller
{
    private static readonly JsonSerializerSettings _indentedSettings;
    private readonly NewtonsoftJsonOutputFormatter _indentingFormatter;

    static NormalController()
    {
        _indentedSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();
        _indentedSettings.Formatting = Formatting.Indented;
    }

    public NormalController(ArrayPool<char> charPool)
    {
        _indentingFormatter = new NewtonsoftJsonOutputFormatter(_indentedSettings, charPool, new MvcOptions(), new MvcNewtonsoftJsonOptions());
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var result = context.Result as ObjectResult;
        if (result != null)
        {
            result.Formatters.Add(new PlainTextFormatter());
            result.Formatters.Add(new CustomFormatter("application/custom"));
            result.Formatters.Add(_indentingFormatter);
        }

        base.OnActionExecuted(context);
    }

    public string ReturnClassName()
    {
        return "NormalController";
    }

    public User ReturnUser()
    {
        return CreateUser();
    }

    [Produces("application/NoFormatter")]
    public User ReturnUser_NoMatchingFormatter()
    {
        return CreateUser();
    }

    [Produces("application/custom", "application/json", "text/json")]
    public User MultipleAllowedContentTypes()
    {
        return CreateUser();
    }

    [Produces("application/custom")]
    public string WriteUserUsingCustomFormat()
    {
        return "Written using custom format.";
    }

    [NonAction]
    public User CreateUser()
    {
        User user = new User()
        {
            Name = "My name",
            Address = "My address",
        };

        return user;
    }
}
