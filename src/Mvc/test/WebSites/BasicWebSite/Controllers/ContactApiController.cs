// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BasicWebSite;

[ApiController]
[Route("/contact")]
public class ContactApiController : Controller
{
    private readonly ContactsRepository _repository;

    public ContactApiController(ContactsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("{id}")]
    public ActionResult<Contact> Get(int id)
    {
        var contact = _repository.GetContact(id);
        if (contact == null)
        {
            return NotFound();
        }

        return contact;
    }

    [HttpPost]
    public ActionResult<Contact> Post([FromBody] Contact contact)
    {
        _repository.Add(contact);
        return CreatedAtAction(nameof(Get), new { id = contact.ContactId }, contact);
    }

    [VndError]
    [HttpPost("PostWithVnd")]
    public ActionResult<Contact> PostWithVnd([FromBody] Contact contact)
    {
        _repository.Add(contact);
        return CreatedAtAction(nameof(Get), new { id = contact.ContactId }, contact);
    }

    [HttpPost("ActionWithInferredFromBodyParameter")]
    public ActionResult<Contact> ActionWithInferredFromBodyParameter(Contact contact) => contact;

    [HttpPost(nameof(ActionWithInferredFromBodyParameterAndCancellationToken))]
    public ActionResult<Contact> ActionWithInferredFromBodyParameterAndCancellationToken(Contact contact, CancellationToken cts)
        => contact;

    [HttpPost("ActionWithInferredRouteAndQueryParameters/{name}/{id}")]
    public ActionResult<Contact> ActionWithInferredRouteAndQueryParameter(int id, string name, string email)
    {
        return new Contact
        {
            ContactId = id,
            Name = name,
            Email = email,
        };
    }

    [HttpGet("[action]")]
    public ActionResult<Contact> ActionWithInferredEmptyPrefix([FromQuery] Contact contact)
    {
        return contact;
    }

    [HttpGet("[action]")]
    public ActionResult<string> ActionWithInferredModelBinderType(
        [ModelBinder(typeof(TestModelBinder))] string foo)
    {
        return foo;
    }

    [HttpGet("[action]")]
    public ActionResult<string> ActionWithInferredModelBinderTypeWithExplicitModelName(
        [ModelBinder(typeof(TestModelBinder), Name = "bar")] string foo)
    {
        return foo;
    }

    [HttpGet("[action]/{id}")]
    public ActionResult<Contact> ActionWithInferredFromServicesParameter(int id, ContactsRepository repository)
        => repository.GetContact(id) ?? new Contact() { ContactId = id };

    [HttpPost("[action]/{id}")]
    public ActionResult<ContactRequest> ActionWithCompositeComplexTypeParameter(ContactRequest request, ContactsRepository repository)
        => Ok(request);

    [HttpGet("[action]")]
    public ActionResult<int> ActionReturningStatusCodeResult()
    {
        return NotFound();
    }

    [HttpGet("[action]")]
    public ActionResult<int> ActionReturningProblemDetails()
    {
        return NotFound(new ProblemDetails
        {
            Title = "Not Found",
            Type = "Type",
            Detail = "Detail",
            Status = 404,
            Instance = "Instance",
            Extensions =
                {
                    ["tracking-id"] = 27,
                },
        });
    }

    [HttpGet("[action]")]
    public ActionResult<int> ActionReturningValidationProblemDetails()
    {
        return BadRequest(new ValidationProblemDetails
        {
            Title = "Error",
            Status = 400,
            Extensions =
                {
                    ["tracking-id"] = "27",
                },
            Errors =
                {
                    { "Error1", new[] { "Error Message" } },
                },
        });
    }

    [HttpGet("[action]/{id}")]
    public IResult ActionReturningObjectIResult(int id)
        => Results.Ok(new Contact() { ContactId = id });

    [HttpGet("[action]")]
    public IResult ActionReturningStatusCodeIResult()
        => Results.NoContent();

    private class TestModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var val = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            bindingContext.Result = ModelBindingResult.Success("From TestModelBinder: " + val);
            return Task.CompletedTask;
        }
    }
}
