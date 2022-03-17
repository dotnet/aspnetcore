// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class GetResponseMetadata_ControllerWithoutConvention : ControllerBase
    {
        public ActionResult<Person> GetPerson(int id) => null;

        public ActionResult<Person> PostPerson(Person person) => null;
    }

    public class GetResponseMetadata_ControllerActionWithAttributes : ControllerBase
    {
        [Produces(typeof(Person))]
        public IActionResult ActionWithProducesAttribute(int id) => null;

        [ProducesResponseType(201)]
        public IActionResult ActionWithProducesResponseType_StatusCodeInConstructor() => null;

        [ProducesResponseType(typeof(Person), 202)]
        public IActionResult ActionWithProducesResponseType_StatusCodeAndTypeInConstructor() => null;

        [ProducesResponseType(200, StatusCode = 203)]
        public IActionResult ActionWithProducesResponseType_StatusCodeInConstructorAndProperty() => null;

        [ProducesResponseType(typeof(object), 200, Type = typeof(Person), StatusCode = 201)]
        public IActionResult ActionWithProducesResponseType_StatusCodeAndTypeInConstructorAndProperty() => null;

        [CustomResponseType(Type = typeof(Person), StatusCode = 204)]
        public IActionResult ActionWithCustomApiResponseMetadataProvider() => null;

        [Produces201ResponseType]
        public IActionResult ActionWithCustomProducesResponseTypeAttributeWithoutArguments() => null;

        [Produces201ResponseType(201)]
        public IActionResult ActionWithCustomProducesResponseTypeAttributeWithArguments() => null;

        [CustomInvalidProducesResponseType(Type = typeof(Person), StatusCode = "204")]
        public IActionResult ActionWithProducesResponseTypeWithIncorrectStatusCodeType() => null;

        [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Find))]
        public IActionResult GetResponseMetadata_ReturnsValuesFromApiConventionMethodAttribute() => null;

        [ProducesResponseType(204)]
        [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Find))]
        public IActionResult GetResponseMetadata_WithProducesResponseTypeAndApiConventionMethod() => null;
    }

    public class Person { }

    public class CustomResponseTypeAttribute : Attribute, IApiResponseMetadataProvider
    {
        public Type Type { get; set; }

        public int StatusCode { get; set; }

        public void SetContentTypes(MediaTypeCollection contentTypes)
        {
        }
    }

    public class Produces201ResponseTypeAttribute : ProducesResponseTypeAttribute
    {
        public Produces201ResponseTypeAttribute() : base(201) { }

        public Produces201ResponseTypeAttribute(int statusCode) : base(statusCode) { }
    }

    public class CustomInvalidProducesResponseTypeAttribute : ProducesResponseTypeAttribute
    {
        private string _statusCode;

        public CustomInvalidProducesResponseTypeAttribute()
            : base(0)
        {
        }

        public new string StatusCode
        {
            get => _statusCode;
            set
            {
                _statusCode = value;
                base.StatusCode = int.Parse(value, CultureInfo.InvariantCulture);
            }
        }
    }

    [ApiConventionType(typeof(DefaultApiConventions))]
    public class GetDeclaredResponseMetadata_ApiConventionTypeAttributeOnType : Controller
    {
        public IActionResult Post(object model) => null;
    }

    [ApiConventionType(typeof(DefaultApiConventions))]
    public class GetDeclaredResponseMetadata_ApiConventionTypeAttributeOnBaseTypeBase
    {
    }

    public class GetDeclaredResponseMetadata_ApiConventionTypeAttributeOnBaseType : GetDeclaredResponseMetadata_ApiConventionTypeAttributeOnBaseTypeBase
    {
        public IActionResult Post(object model) => null;
    }
}
