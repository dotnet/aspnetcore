// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.JsonPatch.Operations;

namespace Microsoft.AspNetCore.JsonPatch.Adapters
{
    /// <summary>
    /// Defines the operations that can be performed on a JSON patch document.
    /// </summary>
    public interface IObjectAdapter
    {
        /// <summary>
        /// Using the "add" operation a new value is inserted into the root of the target
        /// document, into the target array at the specified valid index, or to a target object at
        /// the specified location.
        ///
        /// When adding to arrays, the specified index MUST NOT be greater than the number of elements in the array.
        /// To append the value to the array, the index of "-" character is used (see [RFC6901]).
        ///
        /// When adding to an object, if an object member does not already exist, a new member is added to the object at the
        /// specified location or if an object member does exist, that member's value is replaced.
        ///
        /// The operation object MUST contain a "value" member whose content
        /// specifies the value to be added.
        ///
        /// For example:
        ///
        /// { "op": "add", "path": "/a/b/c", "value": [ "foo", "bar" ] }
        ///
        /// See RFC 6902 https://tools.ietf.org/html/rfc6902#page-4
        /// </summary>
        /// <param name="operation">The add operation.</param>
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        void Add(Operation operation, object objectToApplyTo);

        /// <summary>
        ///  Using the "copy" operation, a value is copied from a specified location to the
        ///  target location.
        ///
        ///  The operation object MUST contain a "from" member, which references the location in the
        ///  target document to copy the value from.
        ///
        ///  The "from" location MUST exist for the operation to be successful.
        ///
        ///  For example:
        ///
        ///  { "op": "copy", "from": "/a/b/c", "path": "/a/b/e" }
        ///
        /// See RFC 6902 https://tools.ietf.org/html/rfc6902#page-7
        /// </summary>
        /// <param name="operation">The copy operation.</param>
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        void Copy(Operation operation, object objectToApplyTo);

        /// <summary>
        /// Using the "move" operation the value at a specified location is removed and
        /// added to the target location.
        ///
        /// The operation object MUST contain a "from" member, which references the location in the
        /// target document to move the value from.
        ///
        /// The "from" location MUST exist for the operation to be successful.
        ///
        /// For example:
        ///
        /// { "op": "move", "from": "/a/b/c", "path": "/a/b/d" }
        ///
        /// A location cannot be moved into one of its children.
        ///
        /// See RFC 6902 https://tools.ietf.org/html/rfc6902#page-6
        /// </summary>
        /// <param name="operation">The move operation.</param>
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        void Move(Operation operation, object objectToApplyTo);

        /// <summary>
        /// Using the "remove" operation the value at the target location is removed.
        ///
        /// The target location MUST exist for the operation to be successful.
        ///
        /// For example:
        ///
        /// { "op": "remove", "path": "/a/b/c" }
        ///
        /// If removing an element from an array, any elements above the
        /// specified index are shifted one position to the left.
        ///
        /// See RFC 6902 https://tools.ietf.org/html/rfc6902#page-6
        /// </summary>
        /// <param name="operation">The remove operation.</param>
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        void Remove(Operation operation, object objectToApplyTo);

        /// <summary>
        /// Using the "replace" operation the value at the target location is replaced
        /// with a new value.  The operation object MUST contain a "value" member
        /// which specifies the replacement value.
        ///
        /// The target location MUST exist for the operation to be successful.
        ///
        /// For example:
        ///
        /// { "op": "replace", "path": "/a/b/c", "value": 42 }
        ///
        /// See RFC 6902 https://tools.ietf.org/html/rfc6902#page-6
        /// </summary>
        /// <param name="operation">The replace operation.</param>
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        void Replace(Operation operation, object objectToApplyTo);
    }
}