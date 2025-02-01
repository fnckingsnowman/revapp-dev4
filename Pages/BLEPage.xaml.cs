using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace RevoluteConfigApp.Pages
{
    public sealed partial class BLEPage : Page
    {
        // Observable collection to bind to ListBox
        public ObservableCollection<DeviceInfo> Devices { get; set; } = new ObservableCollection<DeviceInfo>();

        private BluetoothLEAdvertisementWatcher _watcher;
        private Dictionary<string, BluetoothLEAdvertisementReceivedEventArgs> _discoveredDevices = new Dictionary<string, BluetoothLEAdvertisementReceivedEventArgs>();

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
            _discoveredDevices.Clear();
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
        private async void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                string deviceName = args.Advertisement.LocalName;

                // Check if the device is already paired
                var deviceSelector = BluetoothLEDevice.GetDeviceSelectorFromDeviceName(deviceName);
                var devices = await DeviceInformation.FindAllAsync(deviceSelector);
                bool isPaired = devices.Count > 0 && devices[0].Pairing.IsPaired;

                DispatcherQueue.TryEnqueue(() =>
                {
                    var existingDevice = Devices.FirstOrDefault(d => d.Name == deviceName);
                    if (existingDevice == null)
                    {
                        Devices.Add(new DeviceInfo
                        {
                            Name = deviceName,
                            IsPaired = isPaired,
                            AdvertisementArgs = args
                        });
                    }
                    else
                    {
                        existingDevice.IsPaired = isPaired; // Update pairing status
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing advertisement: {ex.Message}");
            }
        }

        // Event handler for the "Pair" button click
        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var deviceInfo = button.Tag as DeviceInfo;
                if (deviceInfo != null)
                {
                    OutputTextBlock.Text = $"Pairing with {deviceInfo.Name}...";
                    await PairDeviceAsync(deviceInfo);
                }
            }
        }

        // Event handler for the "Connect" button click
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var deviceInfo = button.Tag as DeviceInfo;
                if (deviceInfo != null)
                {
                    OutputTextBlock.Text = $"Connecting to {deviceInfo.Name}...";
                    await ConnectToDeviceAsync(deviceInfo);
                }
            }
        }

        // Function to pair a device
        private async Task PairDeviceAsync(DeviceInfo deviceInfo)
        {
            try
            {
                Debug.WriteLine($"Attempting to pair with {deviceInfo.Name}...");

                // Find the device by name
                var deviceSelector = BluetoothLEDevice.GetDeviceSelectorFromDeviceName(deviceInfo.Name);
                var devices = await DeviceInformation.FindAllAsync(deviceSelector);

                Debug.WriteLine($"Found {devices.Count} devices matching the name {deviceInfo.Name}.");

                if (devices.Count > 0)
                {
                    var deviceInfoObj = devices[0];
                    Debug.WriteLine($"Device ID: {deviceInfoObj.Id}, IsPaired: {deviceInfoObj.Pairing.IsPaired}");

                    // Check if the device is already paired
                    if (deviceInfoObj.Pairing.IsPaired)
                    {
                        Debug.WriteLine($"{deviceInfo.Name} is already paired.");
                        OutputTextBlock.Text = $"{deviceInfo.Name} is already paired.";
                        deviceInfo.IsPaired = true;
                        return;
                    }

                    // Use the Bluetooth address to connect to the device
                    using (var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInfoObj.Id))
                    {
                        if (bluetoothLeDevice != null)
                        {
                            Debug.WriteLine($"Connected to {deviceInfo.Name}.");

                            // Attempt to pair with the device
                            var pairingResult = await deviceInfoObj.Pairing.PairAsync();
                            Debug.WriteLine($"Pairing result: {pairingResult.Status}");

                            if (pairingResult.Status == DevicePairingResultStatus.Paired)
                            {
                                OutputTextBlock.Text = $"Successfully paired with {deviceInfo.Name}.";
                                deviceInfo.IsPaired = true;

                                // Force UI refresh
                                Devices.Remove(deviceInfo);
                                Devices.Add(deviceInfo);
                            }
                            else
                            {
                                OutputTextBlock.Text = $"Failed to pair with {deviceInfo.Name}. Status: {pairingResult.Status}";
                            }
                        }
                        else
                        {
                            OutputTextBlock.Text = $"Failed to connect to {deviceInfo.Name}.";
                        }
                    }
                }
                else
                {
                    OutputTextBlock.Text = $"Device {deviceInfo.Name} not found.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error pairing with {deviceInfo.Name}: {ex.Message}");
                OutputTextBlock.Text = $"Error pairing with {deviceInfo.Name}: {ex.Message}";
            }
        }
        // Function to connect to a device
        private async Task ConnectToDeviceAsync(DeviceInfo deviceInfo)
        {
            try
            {
                var bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceInfo.AdvertisementArgs.BluetoothAddress);
                if (bluetoothLeDevice != null)
                {
                    if (bluetoothLeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                    {
                        OutputTextBlock.Text = $"{deviceInfo.Name} is already connected to the app.";
                    }
                    else
                    {
                        OutputTextBlock.Text = $"{deviceInfo.Name} is connected to the app.";
                        await DiscoverServicesAsync(bluetoothLeDevice);
                    }
                }
            }
            catch (Exception ex)
            {
                OutputTextBlock.Text = $"Error connecting to {deviceInfo.Name}: {ex.Message}";
            }
        }

        // Function to discover services and characteristics
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
                    }
                    OutputTextBlock.Text = $"Discovered services for {bluetoothLeDevice.Name}.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error discovering services: {ex.Message}");
            }
        }

        private async Task RefreshDeviceListAsync()
        {
            foreach (var deviceInfo in Devices)
            {
                var deviceSelector = BluetoothLEDevice.GetDeviceSelectorFromDeviceName(deviceInfo.Name);
                var devices = await DeviceInformation.FindAllAsync(deviceSelector);
                deviceInfo.IsPaired = devices.Count > 0 && devices[0].Pairing.IsPaired;
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