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
using Windows.Storage.Streams;
using System.Collections.Generic;

namespace RevoluteConfigApp.Pages
{
    public sealed partial class BLEPage : Page
    {
        public ObservableCollection<string> Devices { get; set; } = new ObservableCollection<string>();
        private BluetoothLEAdvertisementWatcher _watcher;
        private GattCharacteristic _targetCharacteristic;
        private Button _lastClickedButton;
        private Dictionary<ulong, DeviceInfo> _deviceInfoMap = new Dictionary<ulong, DeviceInfo>();

        public BLEPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private void StartBLEScan_Click(object sender, RoutedEventArgs e)
        {
            Devices.Clear();
            _deviceInfoMap.Clear();
            OutputTextBlock.Text = "Scanning for BLE devices...";
            StartBLEScan();
        }

        private void StartBLEScan()
        {
            _watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active // Requests scan responses
            };
            _watcher.Received += Watcher_Received;
            _watcher.Start();
        }

        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                string deviceName = args.Advertisement.LocalName;
                ulong bluetoothAddress = args.BluetoothAddress;

                Debug.WriteLine($"Discovered Device: {deviceName}");
                Debug.WriteLine("Service UUIDs:");
                foreach (var uuid in args.Advertisement.ServiceUuids)
                {
                    Debug.WriteLine(uuid.ToString());
                }

                // Check if the device includes the target UUID
                bool hasTargetUuid = args.Advertisement.ServiceUuids.Contains(Guid.Parse("00001523-1212-efde-1523-785feabcd133"));

                if (!_deviceInfoMap.ContainsKey(bluetoothAddress))
                {
                    _deviceInfoMap[bluetoothAddress] = new DeviceInfo
                    {
                        Name = deviceName,
                        ServiceUuids = new HashSet<Guid>(args.Advertisement.ServiceUuids),
                        HasTargetUuid = hasTargetUuid // Track whether the device has the target UUID
                    };
                }
                else
                {
                    var deviceInfo = _deviceInfoMap[bluetoothAddress];
                    if (string.IsNullOrEmpty(deviceInfo.Name) && !string.IsNullOrEmpty(deviceName))
                    {
                        deviceInfo.Name = deviceName;
                    }
                    foreach (var uuid in args.Advertisement.ServiceUuids)
                    {
                        deviceInfo.ServiceUuids.Add(uuid);
                    }
                    deviceInfo.HasTargetUuid = deviceInfo.HasTargetUuid || hasTargetUuid; // Update the flag if the target UUID is found
                }

                DispatcherQueue.TryEnqueue(() =>
                {
                    Devices.Clear();
                    foreach (var deviceInfo in _deviceInfoMap.Values)
                    {
                        // Only add devices that have the target UUID
                        if (deviceInfo.HasTargetUuid && !string.IsNullOrEmpty(deviceInfo.Name))
                        {
                            Devices.Add($"{deviceInfo.Name} - {string.Join(", ", deviceInfo.ServiceUuids)}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing advertisement: {ex.Message}");
            }
        }

        private class DeviceInfo
        {
            public string Name { get; set; }
            public HashSet<Guid> ServiceUuids { get; set; }
            public bool HasTargetUuid { get; set; } // Flag to track if the device has the target UUID
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                string deviceName = button.Tag as string;
                _lastClickedButton = button;
                OutputTextBlock.Text = $"Connecting to {deviceName}...";
                await PairAndConnectToDeviceAsync(deviceName);
            }
        }

        private async Task PairAndConnectToDeviceAsync(string deviceName)
        {
            try
            {
                var devices = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());

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
                    Debug.WriteLine($"Discovered {gattServicesResult.Services.Count} services:");
                    foreach (var service in gattServicesResult.Services)
                    {
                        Debug.WriteLine($"Service UUID: {service.Uuid}");
                    }
                    OutputTextBlock.Text = $"Discovered {gattServicesResult.Services.Count} services for {bluetoothLeDevice.Name}.";
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

        private async void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_targetCharacteristic != null)
            {
                byte[] data = new byte[] {
                    0x01, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
                    0x02, 0x03, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
                    0x04, 0x05
                };
                var writer = new DataWriter();
                writer.WriteBytes(data);
                await _targetCharacteristic.WriteValueAsync(writer.DetachBuffer());
                OutputTextBlock.Text = "Data written successfully.";
            }
        }

        private async void ScanForSpecificUUID_Click(object sender, RoutedEventArgs e)
        {
            string targetUUID = "00001523-1212-efde-1523-785feabcd133";
            OutputTextBlock.Text = $"Scanning for devices advertising {targetUUID}...";

            var selector = GattDeviceService.GetDeviceSelectorFromUuid(Guid.Parse(targetUUID));
            var devices = await DeviceInformation.FindAllAsync(selector);

            Devices.Clear();
            if (devices.Count > 0)
            {
                foreach (var device in devices)
                {
                    Devices.Add(device.Name);
                    Debug.WriteLine($"Found device advertising {targetUUID}: {device.Name}");
                }
                OutputTextBlock.Text = $"Found {devices.Count} devices advertising {targetUUID}.";
            }
            else
            {
                OutputTextBlock.Text = $"No devices found advertising {targetUUID}.";
            }
        }
    }
}