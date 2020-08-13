using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace BlazingPizza.Server
{
    public class OrderState
    {
        private readonly ILogger<OrderState> logger;

        public OrderState(ILogger<OrderState> logger)
        {
            this.logger = logger;
        }

        public bool ShowingConfigureDialog { get; private set; }

        public Pizza ConfiguringPizza { get; private set; }

        public Order Order { get; private set; } = new Order();

        public void ShowConfigurePizzaDialog(PizzaSpecial special)
        {
            logger.LogInformation($"Configuring pizza {special.Id}");

            ConfiguringPizza = new Pizza()
            {
                Special = special,
                SpecialId = special.Id,
                Size = Pizza.DefaultSize,
                Toppings = new List<PizzaTopping>(),
            };

            ShowingConfigureDialog = true;
        }

        public void CancelConfigurePizzaDialog()
        {
            ConfiguringPizza = null;

            ShowingConfigureDialog = false;
        }

        public void ConfirmConfigurePizzaDialog()
        {
            logger.LogInformation($"Adding pizza {ConfiguringPizza.SpecialId}");

            Order.Pizzas.Add(ConfiguringPizza);
            ConfiguringPizza = null;

            ShowingConfigureDialog = false;
        }

        public void RemoveConfiguredPizza(Pizza pizza)
        {
            Order.Pizzas.Remove(pizza);
        }

        public void ResetOrder()
        {
            Order = new Order();
        }

        public void ReplaceOrder(Order order)
        {
            Order = order;
        }
    }
}
