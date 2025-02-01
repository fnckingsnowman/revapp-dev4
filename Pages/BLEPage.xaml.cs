using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Advertisement;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RevoluteConfigApp.Pages
{
    public sealed partial class BLEPage : Page
    {
        // Observable collection to bind to ListBox
        public ObservableCollection<DeviceInfo> Devices { get; set; } = new ObservableCollection<DeviceInfo>();

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

                // Check if the device is already paired
                bool isPaired = IsDevicePaired(deviceName);

                // Add the device to the collection with appropriate button text
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (!Devices.Any(d => d.DeviceName == deviceName)) // Avoid duplicates
                    {
                        Devices.Add(new DeviceInfo
                        {
                            DeviceName = deviceName,
                            ButtonText = isPaired ? "Connect" : "Pair"
                        });
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

        // Method to check if the device is paired
        private bool IsDevicePaired(string deviceName)
        {
            var deviceSelector = BluetoothLEDevice.GetDeviceSelectorFromDeviceName(deviceName);
            var devices = DeviceInformation.FindAllAsync(deviceSelector).AsTask().Result;

            if (devices.Count > 0)
            {
                var deviceInfo = devices[0];
                return deviceInfo.Pairing.IsPaired;
            }
            return false;
        }

        // Event handler for the "Pair/Connect" button click
        private async void DeviceButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                string deviceName = button.Tag as string;
                bool isPaired = IsDevicePaired(deviceName);

                if (isPaired)
                {
                    OutputTextBlock.Text = $"Connecting to {deviceName}...";
                    await ConnectToDeviceAsync(deviceName);
                }
                else
                {
                    OutputTextBlock.Text = $"Pairing with {deviceName}...";
                    await PairDeviceAsync(deviceName);
                }
            }
        }

        // Function to pair the device with the computer
        private async Task PairDeviceAsync(string deviceName)
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
                        if (pairingResult.Status == DevicePairingResultStatus.Paired)
                        {
                            OutputTextBlock.Text = $"Successfully paired with {deviceName}.";
                            // Update the button text to 'Connect'
                            UpdateDeviceButton(deviceName, "Connect");
                        }
                        else
                        {
                            OutputTextBlock.Text = $"Failed to pair with {deviceName}. Status: {pairingResult.Status}";
                        }
                    }
                    else
                    {
                        OutputTextBlock.Text = $"Failed to pair with {deviceName}.";
                    }
                }
                else
                {
                    OutputTextBlock.Text = $"Device {deviceName} not found.";
                }
            }
            catch (Exception ex)
            {
                OutputTextBlock.Text = $"Error pairing with {deviceName}: {ex.Message}";
            }
        }

        // Function to connect to a paired BLE device
        private async Task ConnectToDeviceAsync(string deviceName)
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
                        // Perform your BLE connection logic here
                        OutputTextBlock.Text = $"Successfully connected to {deviceName}.";
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

        // Method to update the device button text after pairing
        private void UpdateDeviceButton(string deviceName, string newButtonText)
        {
            var deviceInfo = Devices.FirstOrDefault(d => d.DeviceName == deviceName);
            if (deviceInfo != null)
            {
                deviceInfo.ButtonText = newButtonText;
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

    // Helper class to represent a device with its name and button text
    public class DeviceInfo
    {
        public string DeviceName { get; set; }
        public string ButtonText { get; set; }
    }
}
