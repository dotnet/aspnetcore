
using System;

namespace TagHelperSample.Web.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Blurb { get; set; }

        public DateTimeOffset DateOfBirth { get; set; }

        public int YearsEmployeed { get; set; }
    }
}