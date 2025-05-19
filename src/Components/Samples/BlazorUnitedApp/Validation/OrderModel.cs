using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.Validation;

namespace BlazorUnitedApp.Validation;

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[ValidatableType]
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class OrderModel
{
    [Required(ErrorMessage = "Order Name is required.")]
    [StringLength(100, ErrorMessage = "Order Name cannot be longer than 100 characters.")]
    public string? OrderName { get; set; }

    public CustomerModel CustomerDetails { get; set; } = new CustomerModel();

    public List<OrderItemModel> OrderItems { get; set; } = new List<OrderItemModel>();

    public OrderModel()
    {
        OrderItems.Add(new OrderItemModel());
    }
}
