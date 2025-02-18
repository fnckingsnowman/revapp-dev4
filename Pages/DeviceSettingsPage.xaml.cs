using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RevoluteConfigApp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DeviceSettingsPage : Page
    {
        private BLEFunctionalities _bleFunctionalities;
        public DeviceSettingsPage()
        {
            this.InitializeComponent();
            _bleFunctionalities = new BLEFunctionalities();
            //_bleFunctionalities.Devices.CollectionChanged += Devices_CollectionChanged;
            BLEFunctionalities.DeviceConnected += OnDeviceConnected;
            //BLEFunctionalities.DeviceDisconnected += OnDeviceDisconnected;
            //_bleFunctionalities.StatusUpdated += OnStatusUpdated;
        }

        private void OnDeviceConnected(object sender, string deviceName)
        {
            ConnectedDeviceNameTextBlock.Text = deviceName;
            Debug.WriteLine($"Device connected: {deviceName}");
        }


    }
}
