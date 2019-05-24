// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Forms
{
    internal class FieldState
    {
        private readonly FieldIdentifier _fieldIdentifier;

        // We track which ValidationMessageStore instances have a nonempty set of messages for this field so that
        // we can quickly evaluate the list of messages for the field without having to query all stores. This is
        // relevant because each validation component may define its own message store, so there might be as many
        // stores are there are fields or UI elements.
        private HashSet<ValidationMessageStore> _validationMessageStores;

        public FieldState(FieldIdentifier fieldIdentifier)
        {
            _fieldIdentifier = fieldIdentifier;
        }

        public bool IsModified { get; set; }

        public IEnumerable<string> GetValidationMessages()
        {
            if (_validationMessageStores != null)
            {
                foreach (var store in _validationMessageStores)
                {
                    foreach (var message in store[_fieldIdentifier])
                    {
                        yield return message;
                    }
                }
            }
        }

        public void AssociateWithValidationMessageStore(ValidationMessageStore validationMessageStore)
        {
            if (_validationMessageStores == null)
            {
                _validationMessageStores = new HashSet<ValidationMessageStore>();
            }

            _validationMessageStores.Add(validationMessageStore);
        }

        public void DissociateFromValidationMessageStore(ValidationMessageStore validationMessageStore)
            => _validationMessageStores?.Remove(validationMessageStore);
    }
}
