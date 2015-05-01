// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ValidationWebSite.Models
{
    public class Person
    {
        public int Id { get; set; }

        // Controller in current area.
        [Remote("IsIdAvailable", "RemoteAttribute_Verify")]
        public string UserId1 { get; set; }

        // Controller in root area.
        [Remote("IsIdAvailable", "RemoteAttribute_Verify", null, HttpMethod = "Post")]
        public string UserId2 { get; set; }

        // Controller in MyArea area.
        [Remote(
            "IsIdAvailable",
            "RemoteAttribute_Verify",
            "Aria",
            ErrorMessage = "/Aria/RemoteAttribute_Verify/IsIdAvailable rejects you.")]
        public string UserId3 { get; set; }

        // Controller in AnotherArea area.
        [Remote(
            "IsIdAvailable",
            "RemoteAttribute_Verify",
            "AnotherAria",
            AdditionalFields = "UserId1, UserId2, UserId3")]
        public string UserId4 { get; set; }
    }
}