using System;

namespace Company.WebApplication1.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        
        public string SomeTestProperty => "This is just a test";
    }
}
