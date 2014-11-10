using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FormatterWebSite
{
    public class Project
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [MinLength(2)]
        [MaxLength(5)]
        public Supplier[] Suppliers { get; set; }
    }

    public class Supplier
    {
        public int Id { get; set; }

        [MaxLength(5)]
        public string Name { get; set; }
    }
}