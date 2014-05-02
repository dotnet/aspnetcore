// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.ComponentModel.DataAnnotations;

namespace MvcSample.Web.Models
{
    public class User
    {
        [Required]
        [MinLength(4)]
        public string Name { get; set; }
        public string Address { get; set; }
        [Range(27, 70)]
        public int Age { get; set; }
        public decimal GPA { get; set; }
        public User Dependent { get; set; }
        public bool Alive { get; set; }
        public string Password { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = true, NullDisplayText = "You can explain about your profession")]
        public string Profession { get; set; }
        public string About { get; set; }
        public string Log { get; set; }
    }
}