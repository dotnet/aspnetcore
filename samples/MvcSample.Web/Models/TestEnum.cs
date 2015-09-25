using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MvcSample.Web.Models
{
    public enum TestEnum
    {
        Zero = 0,
        [Display(GroupName = "Primes")]
        One = 1,
        [Display(GroupName = "Evens", Name = "Dos")]
        Two = 2,
        [Display(GroupName = "Primes")]
        Three = 3,
        [Display(Name = "4th")]
        Four = 4
    }
}
