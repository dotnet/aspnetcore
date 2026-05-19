// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Models;

public class RemoteAttributeUser
{
    public int Id { get; set; }

    // Controller in current area.
    [Required(ErrorMessage = "UserId1 is required")]
    [Remote(action: "IsIdAvailable", controller: "RemoteAttribute_Verify")]
    public string UserId1 { get; set; }

    // Controller in root area.
    [Required(ErrorMessage = "UserId2 is required")]
    [Remote(action: "IsIdAvailable", controller: "RemoteAttribute_Verify", areaName: null, HttpMethod = "Post")]
    public string UserId2 { get; set; }

    [Required(ErrorMessage = "UserId3 is required")]
    [Remote(
        action: "IsIdAvailable",
        controller: "RemoteAttribute_Verify",
        areaName: "Area1",
        ErrorMessage = "/Area1/RemoteAttribute_Verify/IsIdAvailable rejects you.")]
    public string UserId3 { get; set; }

    [Required(ErrorMessage = "UserId4 is required")]
    [Remote(
        action: "IsIdAvailable",
        controller: "RemoteAttribute_Verify",
        areaName: "Area2",
        AdditionalFields = "UserId1, UserId2, UserId3")]
    public string UserId4 { get; set; }

    [Required(ErrorMessage = "UserId5 is required")]
    [Remote(routeName: "VerifyRoute", HttpMethod = "Post")]
    public string UserId5 { get; set; }
}
