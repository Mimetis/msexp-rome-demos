# msexp-rome-demos
Démos pour la session ROME

## Demo 01 Hello world

## Demo 02 LaunchUri with Deezer

In this demo, we show we integrate Project Rome into Deezer UWP.

As we used IOP, we are working with services.

### Remote system

With the Deezer app, we decide to work only with Proximal devices. 
In that way, the filter we add is a DiscoveryType filter set on Proximal.

```
_watcher = RemoteSystem.CreateWatcher(new List<IRemoteSystemFilter>()
{
	new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Proximal)
});
```

Then we register the three RemoteSystem events (Added, Removed, Updated) before starting the watcher.
As we are in a service, I added events to prevent any modification of remote devices and to keep a copy of this remote devices.

### Launch and Handle URI

The launch Uri is working like on the previous Demo. We first register a protocol for our app to enable deeplinks.
Then we pass some parameters like a classical get http call.

The call is catch by the receiver using a regex. 
A bit of logical enable to create some playing options before launching the music.
To catch the call, we override the `OnActivated(IActivatedEventArgs args)` method of the Application class into the App.xaml.cs.
It's in this method that we will try to catch any remote call by calling our service method `_remoteService.TryCatchRemoteAction(followupUrl);`


```
protected override async void OnActivated(IActivatedEventArgs args)
{
	Logger.Current.Trace(LogScope.App, "↑↑ App.OnActivated ↑↑");
	base.OnActivated(args);
	switch (args.Kind)
	{
		case ActivationKind.Protocol:
			await EnsureAppIsInitialized(args);
			ProtocolActivatedEventArgs protocolArgs = args as ProtocolActivatedEventArgs;
			HandleDefaultActivation(protocolArgs?.Uri?.OriginalString, args.PreviousExecutionState == ApplicationExecutionState.Running);
			break;

		default:
			HandleDefaultActivation(null, args.PreviousExecutionState == ApplicationExecutionState.Running);
			break;
	}
}

private async void HandleDefaultActivation(string followupUrl, bool isAlreadyRunning)
{
	if (!isAlreadyRunning)
	{
		await _navigationService.SwitchNavigationStack(PageToken.Bootscreen, followupUrl, false);
		await _remoteService.TryCatchRemoteAction(followupUrl);
	}
	else
	{
		bool remoteActionHandle = await _remoteService.TryCatchRemoteAction(followupUrl);
		if ((!string.IsNullOrEmpty(followupUrl) && !remoteActionHandle))
		{
			Task<bool> doNotAwait = _navigationService.SwitchNavigationStack(PageToken.Bootscreen, followupUrl, false);
		}
	}

	Window.Current.Activate();
}
```

As in our app we need to be logged to play musics, I had an ManualResetEvent to be sure that the playing part is not called since I'm not logged.

## Demo 03 remote commands

Like explain with the Demo one, AppServices are background tasks. To have a communication between foreground and background we should keep a localconnection between both.
I just create an empty background task `SmtcControllerTask : IBackgroundTask` in another project. Added reference to main project and update the manifest to reference the background task

```
<uap:Extension Category="windows.appService" EntryPoint="RemoteControlService.SmtcControllerTask">
	<uap3:AppService Name="com.deezer.smtccontrollertask" SupportsRemoteSystems="true"/>
</uap:Extension>
```

### Local configuration

Then, back to our RemoteService, I create a localconnection that I keep open while running.

```
_localConnection = new AppServiceConnection
{
	AppServiceName = "com.deezer.smtccontrollertask",
	PackageFamilyName = Package.Current.Id.FamilyName
};

Logger.Current.Trace(LogScope.Logging, "Opening connection to remote app service...");
_localConnection.RequestReceived += OnRequestReceived;
AppServiceConnectionStatus status = await _localConnection.OpenAsync();
if (status == AppServiceConnectionStatus.Success)
{
	Logger.Current.Trace(LogScope.Logging, "Successfully connected to remote app service.");
	ValueSet inputs = new ValueSet();
	inputs.Add(AppServiceMessage.KEY_TYPE, AppServiceMessage.VAL_TYPE_INIT);
	await SendMessageAsync(_localConnection, inputs);
}
else
{
	Logger.Current.Trace(LogScope.Logging, "Attempt to open a remote app service connection failed with error - " + status.ToString());
}
```

To know that it's a local initialisation, we set up a key type with "init" as a value. Like this, no command is handle when this message is received.

### BackgroundTask

Side of the background task, we check is the connection is set localy or not with ths `IsRemoteSystemConnection` property. In that case, we saved the connection into a private static variable as only one local connection is possible.
After that we send a message to the foreground to be sure the connection is open.

All message received by the background task are forwarded to the foreground task by using this static localconnection.

```
async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
{
	var messageDeferral = args.GetDeferral();

	try
	{
		var inputs = args.Request.Message;
		string type = (string)inputs[AppServiceMessage.KEY_TYPE];
		if (type == AppServiceMessage.VAL_TYPE_DATA)
		{
			AppServiceResponse response = await _localConnection.SendMessageAsync(inputs);
		}
		else
		{
			//Background task launched
		}

		var result = new ValueSet();
		result.Add(AppServiceMessage.KEY_TYPE, AppServiceMessage.VAL_TYPE_RESULT);
		result.Add(AppServiceMessage.KEY_RESULT, AppServiceMessage.VAL_RESULT_OK);
		await args.Request.SendResponseAsync(result);
	}
	finally
	{
		messageDeferral.Complete();
	}
}
```

In case of the connection is a remote one, we just register to the RequestReceived event to forward the request to the localconnection.

### remote call

The remote part is acting like the local one. The difference is that we dispose the connection after sending the message.

```
using (AppServiceConnection connection = new AppServiceConnection
		{
			AppServiceName = "com.deezer.smtccontrollertask",
			PackageFamilyName = Package.Current.Id.FamilyName
		})
{
	connection.RequestReceived += OnRequestReceived;
	Logger.Current.Trace(LogScope.Logging, "Opening connection to remote app service...");
	AppServiceConnectionStatus status = await connection.OpenRemoteAsync(connectionRequest);
	if (status == AppServiceConnectionStatus.Success)
	{
		Logger.Current.Trace(LogScope.Logging, "Successfully connected to remote app service.");
		ValueSet inputs = new ValueSet();
		inputs.Add(AppServiceMessage.KEY_TYPE, AppServiceMessage.VAL_TYPE_DATA);
		inputs.Add(AppServiceMessage.KEY_COMMAND, remoteCommand.ToString());
		await SendMessageAsync(connection, inputs);
	}
	else
	{
		Logger.Current.Trace(LogScope.Logging, "Attempt to open a remote app service connection failed with error - " + status.ToString());
	}
}
```

This time as key type we send "data" to be sure the catch will consider this is a command. The command that is given into the key Command as a string. 
When received by the RemoteService, we execute the good command by checking the command value.

## Demo 04 Android SDK

As we decide to only enable to go from Mobile to Desktop app, on the Android device we just implement the "send" part.

The main difference between Windows and other plateform apps, is that we are not connected with an MSA on the device.
So, how to access Remote Devices ? 
We should as the user to login with an MSA. Microsoft SDK enable us to call `Platform.initialize()` who will check if we have an MSA token and directly connect us with it or otherwise ask to fetch the an authcode :

```
Platform.initialize(getApplicationContext(), new IAuthCodeProvider() {
	@Override
	/**
	 * ConnectedDevices Platform needs the app to fetch a MSA auth_code using the given oauthUrl.
	 * When app is fetched the auth_code, it needs to invoke the authCodeHandler onAuthCodeFetched method.
	 */
	public void fetchAuthCodeAsync(String oauthUrl, Platform.IAuthCodeHandler handler) {
		[...]
	}

	@Override
	public String getClientId() {
		return API_ID;
	}
},  
new IPlatformInitializationHandler() {
	@Override
	/**
	 * When Auth is successfully done 
	 */
	public void onDone() {
		[...]
	}

	@Override
	/**
	 * When Auth is generating an error
	 */
	public void onError(PlatformInitializationStatus status) {
		[...]
	}
});
```

In case of we didn't have an MSA register in the app, we have an authUrl to navigate that enable to get the MSA token.


The RemoteDevice API is working closely like on Windows.
We have a RemoteSystemDiscovery builder that catch events added, removed, updated for devices. We have RemoteSystemDiscoveryTypeFilter and RemoteSystemKindFilter.
The `RemoteLauncher.LaunchUriAsync` is waiting for completed event.
And, of course, we also have a communication with the AppService using a ServiceDispatcher acting like on Windows:
 - Open a connection `new AppServiceConnection("com.deezer.smtccontrollertask", PACKAGE_FAMILY_NAME, new RemoteSystemConnectionRequest(device.getSystem()), serviceConnectionListener, appServiceRequestListener)`
 - Send message by settings KeyValuePair parameters.