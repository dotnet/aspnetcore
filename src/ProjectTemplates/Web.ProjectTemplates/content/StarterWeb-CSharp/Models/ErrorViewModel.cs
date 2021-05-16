using System;

namespace Company.WebApplication1.Models
{
    public class ErrorViewModel
    {
#if (!Nullable)
        public string RequestId { get; set; }
#else
        public string? RequestId { get; set; }
#endif

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
