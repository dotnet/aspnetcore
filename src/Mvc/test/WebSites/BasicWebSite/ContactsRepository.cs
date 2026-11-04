// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;

namespace BasicWebSite;

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
