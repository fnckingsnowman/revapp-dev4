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
using Microsoft.UI.Xaml.Navigation;

namespace RevoluteConfigApp.Pages
{
    public sealed partial class BLEPage : Page
    {
        public static event EventHandler<string> DeviceConnected;
        public static event EventHandler DeviceDisconnected;
        private static BluetoothLEDevice _connectedDevice;

        public ObservableCollection<string> Devices { get; set; } = new ObservableCollection<string>();
        private BluetoothLEAdvertisementWatcher _watcher;
        private GattCharacteristic _targetCharacteristic;
        private Button _lastClickedButton;

        public BLEPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
            BLEDataService.Instance.DataReadyToWrite += OnDataReadyToWrite;
        }

        private async void OnDataReadyToWrite(object sender, byte[] data)
        {
            if (_targetCharacteristic != null)
            {
                try
                {
                    var writer = new DataWriter();
                    writer.WriteBytes(data);
                    await _targetCharacteristic.WriteValueAsync(writer.DetachBuffer());
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        OutputTextBlock.Text = "Configuration data written successfully.";
                    });
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        OutputTextBlock.Text = $"Error writing data: {ex.Message}";
                    });
                }
            }
            else
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    OutputTextBlock.Text = "No target characteristic found. Ensure the device is connected.";
                });
            }
        }

        // Unsubscribe from the event when the page is unloaded
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            BLEDataService.Instance.DataReadyToWrite -= OnDataReadyToWrite;
            base.OnNavigatedFrom(e);
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
                Debug.WriteLine($"Device Address: {args.BluetoothAddress:X12}");
                Debug.WriteLine($"Device Name: {deviceName}");

                foreach (var serviceUuid in args.Advertisement.ServiceUuids)
                {
                    Debug.WriteLine($"Advertising Service UUID: {serviceUuid}");
                }

                foreach (var manufacturerData in args.Advertisement.ManufacturerData)
                {
                    Debug.WriteLine($"Manufacturer ID: {manufacturerData.CompanyId}");
                }

                DispatcherQueue.TryEnqueue(() =>
                {
                    if (!Devices.Contains(deviceName))
                    {
                        Devices.Add(deviceName);
                    }
                });
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
                _lastClickedButton = button;
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
                            _connectedDevice = bluetoothLeDevice;
                            DeviceConnected?.Invoke(this, deviceName);
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
                    Debug.WriteLine($"Discovered services for {bluetoothLeDevice.Name}:");
                    foreach (var service in gattServicesResult.Services)
                    {
                        Debug.WriteLine($"Service UUID: {service.Uuid}");
                        var gattCharacteristicsResult = await service.GetCharacteristicsAsync();
                        if (gattCharacteristicsResult.Status == GattCommunicationStatus.Success)
                        {
                            Debug.WriteLine($"Characteristics for service {service.Uuid}:");
                            foreach (var characteristic in gattCharacteristicsResult.Characteristics)
                            {
                                Debug.WriteLine($"Characteristic UUID: {characteristic.Uuid}");
                                if (characteristic.Uuid == Guid.Parse("00000000000000000000003323de1226"))
                                {
                                    _targetCharacteristic = characteristic;
                                    DispatcherQueue.TryEnqueue(() =>
                                    {
                                        if (_lastClickedButton != null)
                                        {
                                            _lastClickedButton.Content = "Write";
                                            _lastClickedButton.Click -= ConnectButton_Click;
                                            _lastClickedButton.Click += WriteButton_Click;
                                        }
                                    });
                                }
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

        private async void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_targetCharacteristic != null)
            {
                byte[] data = new byte[] {
                    0x01, // deadzone
                    0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, // up_report
                    0x02, // up_identPerRev
                    0x03, // up_transport
                    0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, // dn_report
                    0x04, // dn_identPerRev
                    0x05  // dn_transport
                };
                var writer = new DataWriter();
                writer.WriteBytes(data);
                await _targetCharacteristic.WriteValueAsync(writer.DetachBuffer());
                OutputTextBlock.Text = "Data written successfully.";
            }
        }

        public static void DisconnectDevice()
        {
            if (_connectedDevice != null)
            {
                _connectedDevice.Dispose();
                _connectedDevice = null;
                DeviceDisconnected?.Invoke(null, EventArgs.Empty);
            }
        }

        public void UpdateOutputText(string message)
        {
            OutputTextBlock.Text = message;
        }
    }
    public class BLEDataService
    {
        private static BLEDataService _instance;
        public static BLEDataService Instance => _instance ??= new BLEDataService();

        public event EventHandler<byte[]> DataReadyToWrite;

        private BLEDataService() { }

        public void NotifyDataReady(byte[] data)
        {
            DataReadyToWrite?.Invoke(this, data);
        }
    }
}
