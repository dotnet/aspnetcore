// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace FormatterWebSite;

/// <summary>
/// Summary description for DataContractSerializerController
/// </summary>
public class DataContractSerializerController : Controller
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var result = context.Result as ObjectResult;
        if (result != null)
        {
            result.Formatters.Add(new XmlSerializerOutputFormatter());
            result.Formatters.Add(new XmlDataContractSerializerOutputFormatter());
        }

        base.OnActionExecuted(context);
    }

    [HttpPost]
    public Person GetPerson(string name)
    {
        // The XmlSerializer should skip and the
        // DataContractSerializer should pick up this output.
        return new Person(name);
    }

    [HttpPost]
    public Task<Person> GetTaskOfPerson(string name)
    {
        // The XmlSerializer should skip and the
        // DataContractSerializer should pick up this output.
        return Task.FromResult(new Person(name));
    }

    [HttpPost]
    public Task<object> GetTaskOfPersonAsObject(string name)
    {
        // The XmlSerializer should skip and the
        // DataContractSerializer should pick up this output.
        return Task.FromResult<object>(new Person(name));
    }
}
