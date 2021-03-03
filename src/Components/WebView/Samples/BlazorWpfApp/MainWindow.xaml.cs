using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();
            Resources.Add("services", serviceCollection.BuildServiceProvider());

            InitializeComponent();
        }
    }
}

// TODO: Replace with .razor component
namespace BlazorWpfApp
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Rendering;

    internal class DemoComponent : ComponentBase
    {
        int count;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "h1");
            builder.AddContent(1, "Hello, world!");
            builder.CloseElement();

            builder.OpenElement(2, "p");
            builder.AddContent(3, $"Current count: {count}");
            builder.CloseElement();

            builder.OpenElement(4, "button");
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () =>
            {
                count++;
            }));
            builder.AddContent(6, "Click me");
            builder.CloseElement();
        }
    }
}
