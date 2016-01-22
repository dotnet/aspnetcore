// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiltersWebSite
{
    [AllowAnonymous]
    [HandleInvalidOperationExceptionFilter]
    public class RandomNumberController : Controller
    {
        [ServiceFilter(typeof(RandomNumberFilter))]
        public int GetRandomNumber()
        {
            return 2;
        }

        [ServiceFilter(typeof(AuthorizeUserAttribute))]
        public int GetAuthorizedRandomNumber()
        {
            return 2;
        }

        [TypeFilter(typeof(RandomNumberProvider))]
        public int GetModifiedRandomNumber(int randomNumber)
        {
            return randomNumber / 2;
        }

        [TypeFilter(typeof(ModifiedRandomNumberProvider))]
        public int GetHalfOfModifiedRandomNumber(int randomNumber)
        {
            return randomNumber / 2;
        }

        [TypeFilter(typeof(RandomNumberModifier), Order = 2)]
        [TypeFilter(typeof(RandomNumberProvider), Order = 1)]
        public int GetOrderedRandomNumber(int randomNumber)
        {
            return randomNumber;
        }

        public string ThrowException()
        {
            throw new InvalidOperationException();
        }
    }
}