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
        // Observable collection to bind to ListBox
        public ObservableCollection<string> Devices { get; set; } = new ObservableCollection<string>();

        private BluetoothLEAdvertisementWatcher _watcher;

        public BLEPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        // Event handler for the "Scan for BLE Devices" button click
        private void StartBLEScan_Click(object sender, RoutedEventArgs e)
        {
            // Clear any existing devices
            Devices.Clear();
            OutputTextBlock.Text = "Scanning for BLE devices...";

            // Initialize and start BLE scanning
            StartBLEScan();
        }

        // Function to start BLE scan using BluetoothLEAdvertisementWatcher
        private void StartBLEScan()
        {
            // Initialize the watcher
            _watcher = new BluetoothLEAdvertisementWatcher();

            // Set up event handler when an advertisement is found
            _watcher.Received += Watcher_Received;

            // Start the scan
            _watcher.Start();
        }

        // Event handler for receiving BLE advertisements
        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                // Extract the device's name (from advertisement data)
                string deviceName = args.Advertisement.LocalName;

                // Add the device name to the devices collection (thread-safe)
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (!Devices.Contains(deviceName))  // Avoid adding duplicates
                    {
                        Devices.Add(deviceName);
                    }
                });

                // Extract and log the service UUIDs from the advertisement data
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

        // Event handler for the "Connect" button click
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                string deviceName = button.Tag as string;
                OutputTextBlock.Text = $"Connecting to {deviceName}...";

                // Find the device by name and attempt to pair/connect
                await PairAndConnectToDeviceAsync(deviceName);
            }
        }

        // Function to pair and connect to a BLE device
        private async Task PairAndConnectToDeviceAsync(string deviceName)
        {
            try
            {
                // Find the device by name
                var deviceSelector = BluetoothLEDevice.GetDeviceSelectorFromDeviceName(deviceName);
                var devices = await DeviceInformation.FindAllAsync(deviceSelector);

                if (devices.Count > 0)
                {
                    var deviceInfo = devices[0];
                    var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);

                    if (bluetoothLeDevice != null)
                    {
                        // Attempt to pair with the device
                        var pairingResult = await bluetoothLeDevice.DeviceInformation.Pairing.PairAsync();
                        if (pairingResult.Status == DevicePairingResultStatus.Paired)
                        {
                            OutputTextBlock.Text = $"Successfully paired with {deviceName}.";

                            // Establish a GATT connection to the device
                            if (bluetoothLeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                            {
                                OutputTextBlock.Text = $"{deviceName} is connected to the app.";
                            }
                            else
                            {
                                OutputTextBlock.Text = $"{deviceName} is paired but not connected to the app.";
                            }

                            // Optionally, discover services and characteristics
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

        // Method to discover services and characteristics
        private async Task DiscoverServicesAsync(BluetoothLEDevice bluetoothLeDevice)
        {
            try
            {
                // Get the GATT services
                var gattServicesResult = await bluetoothLeDevice.GetGattServicesAsync();

                if (gattServicesResult.Status == GattCommunicationStatus.Success)
                {
                    foreach (var service in gattServicesResult.Services)
                    {
                        Debug.WriteLine($"Service UUID: {service.Uuid}");

                        // Get the characteristics for each service
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

        // Stop the BLE scan (optional)
        private void StopBLEScan()
        {
            if (_watcher != null && _watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
            {
                _watcher.Stop();
            }
        }
    }
}