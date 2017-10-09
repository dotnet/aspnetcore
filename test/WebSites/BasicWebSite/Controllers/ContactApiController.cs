// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite
{
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
    }
}