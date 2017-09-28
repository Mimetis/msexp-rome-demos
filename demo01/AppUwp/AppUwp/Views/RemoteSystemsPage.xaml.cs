using AppUwp.Common;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.RemoteSystems;
using Windows.UI.Core;
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
    public sealed partial class RemoteSystemsPage : Page
    {
        public RemoteSystemsPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        public ObservableCollection<RemoteSystem> RemoteSystems { get; set; }= new ObservableCollection<RemoteSystem>();


        RemoteSystemWatcher remoteSystemWatcher = null;
        private async void btnGetDevices_Click(object sender, RoutedEventArgs e)
        {
            RemoteSystems.Clear();
            // Verify access for Remote Systems. 
            // Note: RequestAccessAsync needs to called from the UI thread.
            RemoteSystemAccessStatus accessStatus = await RemoteSystem.RequestAccessAsync();

            if (accessStatus != RemoteSystemAccessStatus.Allowed)
                return;

            if (remoteSystemWatcher != null)
            {
                remoteSystemWatcher.Stop();
                remoteSystemWatcher = null;
            }

            // Build a watcher to continuously monitor for all remote systems.
            remoteSystemWatcher = RemoteSystem.CreateWatcher();

            remoteSystemWatcher.RemoteSystemAdded += M_remoteSystemWatcher_RemoteSystemAdded;
            remoteSystemWatcher.RemoteSystemRemoved += M_remoteSystemWatcher_RemoteSystemRemoved;
            remoteSystemWatcher.RemoteSystemUpdated += M_remoteSystemWatcher_RemoteSystemUpdated;

            // Start the watcher.
            remoteSystemWatcher.Start();

        }

        private async void M_remoteSystemWatcher_RemoteSystemUpdated(RemoteSystemWatcher sender, RemoteSystemUpdatedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RemoteSystems.Remove(RemoteSystems.First(rs => rs.Id == args.RemoteSystem.Id));
                RemoteSystems.Add(args.RemoteSystem);

            });
        }

        private async void M_remoteSystemWatcher_RemoteSystemRemoved(RemoteSystemWatcher sender, RemoteSystemRemovedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RemoteSystems.Remove(RemoteSystems.First(rs => rs.Id == args.RemoteSystemId)));
        }


        private async void M_remoteSystemWatcher_RemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RemoteSystems.Add(args.RemoteSystem);
            });
        }

      
    }
}
