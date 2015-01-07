// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace ModelBindingWebSite.Controllers
{
    public class FromAttributesController : Controller
    {
        [Route("/FromAttributes/[action]/{HomeAddress.Street}/{HomeAddress.State}/{HomeAddress.Zip}")]
        public User_FromBody GetUser_FromBody(User_FromBody user)
        {
            return user;
        }

        [Route("/FromAttributes/[action]/{HomeAddress.Street}/{HomeAddress.State}/{HomeAddress.Zip}")]
        public User_FromForm GetUser_FromForm(User_FromForm user)
        {
            return user;
        }

        [Route("/FromAttributes/[action]/{HomeAddress.Street}/{HomeAddress.State}/{HomeAddress.Zip}")]
        public User_FromForm GetUser([FromRoute] Address homeAddress,
                                     [FromForm] Address officeAddress,
                                     [FromQuery] Address shippingAddress)
        {
            return new User_FromForm
            {
                HomeAddress = homeAddress,
                OfficeAddress = officeAddress,
                ShippingAddress = shippingAddress
            };
        }

        public User_FromForm MultipleFromFormParameters([FromForm] Address homeAddress,
                                                        [FromForm] Address officeAddress)
        {
            return new User_FromForm
            {
                HomeAddress = homeAddress,
                OfficeAddress = officeAddress,
            };
        }

        // User_FromForm has a FromForm property.
        public User_FromForm MultipleFromFormParameterAndProperty(User_FromForm user,
                                                                  [FromForm] Address defaultAddress)
        {
            user.HomeAddress = defaultAddress;
            return user;
        }

        public void FromBodyParametersThrows([FromBody] int id, [FromBody] string emp)
        {
        }

        // Customer has a FromBody Property.
        public void FromBodyParameterAndPropertyThrows([FromBody] Person p, Customer customer)
        {
        }

        public void FormAndBody_Throws([FromForm] Person p, Customer customer)
        {
        }

        public void FormAndBody_AsParameters_Throws([FromBody] int id, [FromForm] string emp)
        {
        }
    }
}