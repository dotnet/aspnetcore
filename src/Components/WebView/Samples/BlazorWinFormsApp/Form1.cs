using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;

namespace BlazorWinFormsApp
{
    public partial class Form1 : Form
    {
        //private readonly AppState _appState;

        public Form1()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();
            InitializeComponent();

            blazorWebView1.HostPage = @"wwwroot\index.html";
            blazorWebView1.Services = serviceCollection.BuildServiceProvider();
            blazorWebView1.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = typeof(Main) });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"Current counter value is <unknown>", "Counter Value");
        }
    }
}
