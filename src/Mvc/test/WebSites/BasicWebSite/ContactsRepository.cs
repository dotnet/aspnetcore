// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using BasicWebSite.Models;

namespace BasicWebSite
{
    public class ContactsRepository
    {
        private readonly List<Contact> _contacts = new List<Contact>();

        public Contact GetContact(int id)
        {
            return _contacts.FirstOrDefault(f => f.ContactId == id);
        }

        public void Add(Contact contact)
        {
            contact.ContactId = _contacts.Count + 1;
            _contacts.Add(contact);
        }
    }
}