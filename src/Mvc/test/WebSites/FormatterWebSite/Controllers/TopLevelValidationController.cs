// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FormatterWebSite.Controllers;

public class TopLevelValidationController : Controller
{
    [BindProperty] public int OptionalProp { get; set; }
    [BindProperty, Required] public int RequiredProp { get; set; }
    [BindProperty, BindRequired] public int BindRequiredProp { get; set; }
    [BindProperty, Required, BindRequired] public int RequiredAndBindRequiredProp { get; set; }
    [BindProperty, StringLength(5)] public string OptionalStringLengthProp { get; set; }
    [BindProperty, Range(1, 100), DisplayName("Some Display Name For Prop")] public int OptionalRangeDisplayNameProp { get; set; }

    // Despite the Required/BindRequired attributes, these properties won't be validated
    // because they aren't [BindProperty] properties (hence aren't involved in binding).
    [Required] public int UnboundRequiredProp { get; set; }
    [BindRequired] public int UnboundBindRequiredProp { get; set; }

    // The [BindNever] overrides [BindProperty], meaning [Required] will not apply
    // (nor will any incoming value be used)
    [BindProperty, BindNever, Required] public string BindNeverRequiredProp { get; set; }

    public IActionResult Index(
        int optionalParam,
        [Required] int requiredParam,
        [BindRequired] int bindRequiredParam,
        [Required, BindRequired] int requiredAndBindRequiredParam,
        [StringLength(5)] string optionalStringLengthParam,
        [Range(1, 100), Display(Name = "Some Display Name For Param")] int optionalRangeDisplayNameParam)
    {
        if (ModelState.IsValid)
        {
            return Content($@"
                    [{ nameof(OptionalProp) }:{ OptionalProp }]
                    [{ nameof(RequiredProp) }:{ RequiredProp }]
                    [{ nameof(BindRequiredProp) }:{ BindRequiredProp }]
                    [{ nameof(RequiredAndBindRequiredProp) }:{ RequiredAndBindRequiredProp }]
                    [{ nameof(OptionalStringLengthProp) }:{ OptionalStringLengthProp }]
                    [{ nameof(OptionalRangeDisplayNameProp) }:{ OptionalRangeDisplayNameProp }]
                    [{ nameof(UnboundRequiredProp) }:{ UnboundRequiredProp }]
                    [{ nameof(UnboundBindRequiredProp) }:{ UnboundBindRequiredProp }]
                    [{ nameof(BindNeverRequiredProp) }:{ BindNeverRequiredProp }]
                    [{ nameof(optionalParam) }:{ optionalParam }]
                    [{ nameof(requiredParam) }:{ requiredParam }]
                    [{ nameof(bindRequiredParam) }:{ bindRequiredParam }]
                    [{ nameof(requiredAndBindRequiredParam) }:{ requiredAndBindRequiredParam }]
                    [{ nameof(optionalStringLengthParam) }:{ optionalStringLengthParam }]
                    [{ nameof(optionalRangeDisplayNameParam) }:{ optionalRangeDisplayNameParam }]");
        }
        else
        {
            return BadRequest(ModelState);
        }
    }
}
