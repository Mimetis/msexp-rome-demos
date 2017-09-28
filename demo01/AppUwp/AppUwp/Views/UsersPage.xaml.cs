using AppUwp.Common;
using AppUwp.ViewModels;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace AppUwp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UsersPage : Page
    {
        MsalAuthenticationProvider authenticationProvider;
        GraphServiceClient graphService;

        public UsersPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.authenticationProvider = new MsalAuthenticationProvider();
            this.graphService = new GraphServiceClient(authenticationProvider);
        }

        public ObservableCollection<UserViewModel> Users = new ObservableCollection<UserViewModel>();

        public bool IsLoading { get; private set; }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.QueryText))
                return;


            try
            {
                this.IsLoading = true;
                Users.Clear();

                var userRequest = graphService.Users.Request();
                var usersRequestFilter = userRequest.Filter($"startswith(displayName,'{args.QueryText}')");
                var userRequestFilterSelect = usersRequestFilter.Select("id,displayName,mail,givenName,userPrincipalName,givenName");
                var allusers = await userRequestFilterSelect.GetAsync();
                
                foreach (var user in allusers)
                    Users.Add(new UserViewModel(user, graphService));

                this.IsLoading = false;
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine("Task canceled " + ex.Message);
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("Operation canceled " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception " + ex.Message);
            }
            finally
            {
                this.IsLoading = false;
            }

        }

        private void UserViewModel_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}
