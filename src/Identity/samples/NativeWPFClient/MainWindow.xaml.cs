using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NativeWPFClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Task _currentAuthorization;

        public MainWindow()
        {
            InitializeComponent();
            // Local client
            DataContext = new NativeWPFClientViewModel
            {
                BaseAddress = "https://localhost:44324/",
                RedirectUri = "urn:ietf:wg:oauth:2.0:oob",
                Tenant = "Identity",
                Policy = "signinsignup",
                ClientId = "06D7C2FB-A66A-41AD-9509-77BDDFAB111B",
                Scopes = "https://localhost/DFC7191F-FF74-42B9-A292-08FEA80F5B20/v2.0/ProtectedApi/read"
            };

            // DataContext = new NativeWPFClientViewModel
            // {
            //     BaseAddress = "https://login.microsoftonline.com/",
            //     RedirectUri = "urn:ietf:wg:oauth:2.0:oob",
            //     Tenant = "jacalvarb2c.onmicrosoft.com",
            //     Policy = "B2C_1_signinsignup",
            //     ClientId = "42291769-0dc8-4497-9cbc-d3879783d6e7",
            //     Scopes = "https://jacalvarb2c.onmicrosoft.com/ProtectedApi/read"
            // };

            ViewModel.Result = "Hit authorize to sign in";
        }

        NativeWPFClientViewModel ViewModel => (NativeWPFClientViewModel)DataContext;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private async void Authorize_Click(object sender, RoutedEventArgs e)
        {
            if (_currentAuthorization == null)
            {
                Authorize.IsEnabled = false;
                await AuthorizeAsync();
            }
        }

        private async Task AuthorizeAsync()
        {
            var authority = $"{ViewModel.BaseAddress}tfp/{ViewModel.Tenant}/{ViewModel.Policy}";
            var client = new PublicClientApplication(ViewModel.ClientId, authority)
            {
                ValidateAuthority = false
            };
            try
            {
                var scope = new string[] { };
                var appScopes = ViewModel.Scopes.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                var currentAuthorization = await client.AcquireTokenAsync(
                    appScopes,
                    user: null,
                    behavior: UIBehavior.ForceLogin,
                    extraQueryParameters: string.Empty,
                    extraScopesToConsent: null,
                    authority: authority);

                ViewModel.Result = currentAuthorization.User?.Name ?? "Authenticated but no name";
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode != "authentication_canceled")
                {
                    // An unexpected error occurred.
                    string message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += "Inner Exception : " + ex.InnerException.Message;
                    }

                    MessageBox.Show(message);
                }
            }
            finally
            {
                _currentAuthorization = null;
                Authorize.IsEnabled = true;
            }
        }
    }

    internal class NativeWPFClientViewModel : INotifyPropertyChanged
    {
        private string _result;

        public string BaseAddress { get; set; }
        public string RedirectUri { get; set; }
        public string Tenant { get; set; }
        public string Policy { get; set; }
        public string ClientId { get; set; }
        public string Scopes { get; set; }


        public string Result
        {
            get => _result;
            set
            {
                _result = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
