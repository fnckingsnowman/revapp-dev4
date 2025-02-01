using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Advertisement;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System;
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
        private async void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                // Extract the device's name (from advertisement data)
                string deviceName = args.Advertisement.LocalName;

                // Check if the device is already paired
                bool isPaired = await IsDevicePairedAsync(args.BluetoothAddress);

                // Add the device to the devices collection (thread-safe)
                DispatcherQueue.TryEnqueue(() =>
                {
                    var deviceInfo = new DeviceInfo
                    {
                        Name = deviceName,
                        BluetoothAddress = args.BluetoothAddress,
                        IsPaired = isPaired
                    };

                    if (!Devices.Contains(deviceInfo))  // Avoid adding duplicates
                    {
                        Devices.Add(deviceInfo);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing advertisement: {ex.Message}");
            }
        }

        // Check if a device is already paired
        private async Task<bool> IsDevicePairedAsync(ulong bluetoothAddress)
        {
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            if (device != null)
            {
                return device.DeviceInformation.Pairing.IsPaired;
            }
            return false;
        }

        // Event handler for the "Connect" button click
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var deviceInfo = button.Tag as DeviceInfo;
                OutputTextBlock.Text = $"Connecting to {deviceInfo.Name}...";

                // Connect to the device
                await ConnectToDeviceAsync(deviceInfo);
            }
        }

        // Event handler for the "Pair" button click
        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var deviceInfo = button.Tag as DeviceInfo;
                OutputTextBlock.Text = $"Pairing with {deviceInfo.Name}...";

                // Pair the device
                await PairDeviceAsync(deviceInfo);
            }
        }

        // Function to connect to a paired device
        private async Task ConnectToDeviceAsync(DeviceInfo deviceInfo)
        {
            try
            {
                var bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceInfo.BluetoothAddress);

                if (bluetoothLeDevice != null && bluetoothLeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    OutputTextBlock.Text = $"{deviceInfo.Name} is connected to the app.";
                }
                else
                {
                    OutputTextBlock.Text = $"{deviceInfo.Name} is paired but not connected to the app.";
                }
            }
            catch (Exception ex)
            {
                OutputTextBlock.Text = $"Error connecting to {deviceInfo.Name}: {ex.Message}";
            }
        }

        // Function to pair an unpaired device
        private async Task PairDeviceAsync(DeviceInfo deviceInfo)
        {
            try
            {
                var bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceInfo.BluetoothAddress);

                if (bluetoothLeDevice != null)
                {
                    var pairingResult = await bluetoothLeDevice.DeviceInformation.Pairing.PairAsync();

                    if (pairingResult.Status == DevicePairingResultStatus.Paired)
                    {
                        OutputTextBlock.Text = $"Successfully paired with {deviceInfo.Name}.";
                        deviceInfo.IsPaired = true; // Update the device's pairing status
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
            catch (Exception ex)
            {
                OutputTextBlock.Text = $"Error pairing with {deviceInfo.Name}: {ex.Message}";
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

    // Class to represent a BLE device
    public class DeviceInfo
    {
        public string Name { get; set; }
        public ulong BluetoothAddress { get; set; }
        public bool IsPaired { get; set; }
    }
}