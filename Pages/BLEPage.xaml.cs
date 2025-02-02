using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Advertisement;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace RevoluteConfigApp.Pages
{
    public sealed partial class BLEPage : Page
    {
        public ObservableCollection<string> Devices { get; set; } = new ObservableCollection<string>();
        private BluetoothLEAdvertisementWatcher _watcher;

        public BLEPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private void StartBLEScan_Click(object sender, RoutedEventArgs e)
        {
            Devices.Clear();
            OutputTextBlock.Text = "Scanning for BLE devices...";
            StartBLEScan();
        }

        private void StartBLEScan()
        {
            _watcher = new BluetoothLEAdvertisementWatcher();
            _watcher.Received += Watcher_Received;
            _watcher.Start();
        }

        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                string deviceName = args.Advertisement.LocalName;
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (!Devices.Contains(deviceName))
                    {
                        Devices.Add(deviceName);
                    }
                });

                foreach (var uuid in args.Advertisement.ServiceUuids)
                {
                    Debug.WriteLine($"Device: {deviceName}, UUID: {uuid}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing advertisement: {ex.Message}");
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                string deviceName = button.Tag as string;
                OutputTextBlock.Text = $"Connecting to {deviceName}...";
                await PairAndConnectToDeviceAsync(deviceName);
            }
        }

        private async Task PairAndConnectToDeviceAsync(string deviceName)
        {
            try
            {
                var deviceSelector = BluetoothLEDevice.GetDeviceSelectorFromDeviceName(deviceName);
                var devices = await DeviceInformation.FindAllAsync(deviceSelector);

                if (devices.Count > 0)
                {
                    var deviceInfo = devices[0];
                    var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);

                    if (bluetoothLeDevice != null)
                    {
                        var pairingResult = await bluetoothLeDevice.DeviceInformation.Pairing.PairAsync();
                        if (pairingResult.Status == DevicePairingResultStatus.Paired || pairingResult.Status == DevicePairingResultStatus.AlreadyPaired)
                        {
                            OutputTextBlock.Text = $"Successfully paired or already paired with {deviceName}.";

                            if (bluetoothLeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                            {
                                OutputTextBlock.Text = $"{deviceName} is connected to the app.";
                            }
                            else
                            {
                                OutputTextBlock.Text = $"{deviceName} is paired but not yet connected. Attempting connection...";
                            }

                            await DiscoverServicesAsync(bluetoothLeDevice);
                        }
                        else
                        {
                            OutputTextBlock.Text = $"Failed to pair with {deviceName}. Status: {pairingResult.Status}";
                        }
                    }
                    else
                    {
                        OutputTextBlock.Text = $"Failed to connect to {deviceName}.";
                    }
                }
                else
                {
                    OutputTextBlock.Text = $"Device {deviceName} not found.";
                }
            }
            catch (Exception ex)
            {
                OutputTextBlock.Text = $"Error connecting to {deviceName}: {ex.Message}";
            }
        }

        private async Task DiscoverServicesAsync(BluetoothLEDevice bluetoothLeDevice)
        {
            try
            {
                var gattServicesResult = await bluetoothLeDevice.GetGattServicesAsync();

                if (gattServicesResult.Status == GattCommunicationStatus.Success)
                {
                    foreach (var service in gattServicesResult.Services)
                    {
                        Debug.WriteLine($"Service UUID: {service.Uuid}");
                        var gattCharacteristicsResult = await service.GetCharacteristicsAsync();
                        if (gattCharacteristicsResult.Status == GattCommunicationStatus.Success)
                        {
                            foreach (var characteristic in gattCharacteristicsResult.Characteristics)
                            {
                                Debug.WriteLine($"Characteristic UUID: {characteristic.Uuid}");
                            }
                        }
                    }
                    OutputTextBlock.Text = $"Discovered services and characteristics for {bluetoothLeDevice.Name}.";
                }
                else
                {
                    OutputTextBlock.Text = $"Failed to discover services for {bluetoothLeDevice.Name}.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error discovering services: {ex.Message}");
            }
        }

        private void StopBLEScan()
        {
            if (_watcher != null && _watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
            {
                _watcher.Stop();
            }
        }
    }
}
