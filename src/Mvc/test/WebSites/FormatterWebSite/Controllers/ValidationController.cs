// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FormatterWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite;

public class ValidationController : Controller
{
    [HttpPost]
    public IActionResult Index([FromBody] User user)
    {
        if (!ModelState.IsValid)
        {
            return Content(ModelState["Id"].Errors[0].ErrorMessage + "," +
                ModelState["Name"].Errors[0].ErrorMessage + "," +
                ModelState["Alias"].Errors[0].ErrorMessage + "," +
                ModelState["Designation"].Errors[0].ErrorMessage);
        }

        return Content("User has been registered : " + user.Name);
    }

    [HttpPost]
    public string GetDeveloperName([FromBody] Developer developer)
    {
        // Developer is excluded in startup, hence the value should never be passed.
        if (ModelState.IsValid)
        {
            if (string.IsNullOrEmpty(developer.Name))
            {
                return "No model validation for developer, even though developer.Name is empty.";
            }

            return developer.Name;
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    [HttpPost]
    public string GetDeveloperAlias(Developer developer)
    {
        if (ModelState.IsValid)
        {
            return developer.Alias;
        }
        else
        {
            return ModelState["Name"].Errors[0].ErrorMessage;
        }
    }

    // 'Developer' type is excluded but the shallow validation on the
    // property Developers should happen
    [ModelStateValidationFilter]
    public Project CreateProject([FromBody] Project project)
    {
        return project;
    }

    [ModelStateValidationFilter]
    public SimpleTypePropertiesModel CreateSimpleTypePropertiesModel([FromBody] SimpleTypePropertiesModel simpleTypePropertiesModel)
    {
        return simpleTypePropertiesModel;
    }

    [HttpPost]
    public IActionResult ValidationProviderAttribute([FromBody] ValidationProviderAttributeModel validationProviderAttributeModel)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult ValidationThrowsError_WhenValidationExceedsMaxValidationDepth([FromBody] InfinitelyRecursiveModel model)
    {
        return Ok();
    }

    [HttpPost]
    [ModelStateValidationFilter]
    public IActionResult CreateInvalidModel([FromBody] InvalidModel model)
    {
        return Ok(model);
    }
}
