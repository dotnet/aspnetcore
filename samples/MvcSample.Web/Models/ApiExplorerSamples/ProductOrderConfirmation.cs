using System;

namespace MvcSample.Web.ApiExplorerSamples
{
    public class ProductOrderConfirmation
    {
        public Product Product { get; set; }

        public decimal PricePerUnit { get; set; }

        public int Quantity { get; set; }

        public decimal TotalPrice { get; set; }
    }
}