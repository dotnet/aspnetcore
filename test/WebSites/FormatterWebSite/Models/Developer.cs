// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace FormatterWebSite
{
    public class Developer
    {
        private string _name;

        [Required]
        public string NameThatThrowsOnGet
        {
            get
            {
                if (_name == "RandomString")
                {
                    throw new InvalidOperationException();
                }

                return _name;
            }
            set
            {
                _name = value;
            }
        }
    }
}