﻿using Microsoft.UI.Xaml;
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

namespace RevoluteConfigApp.Pages
{
    public class BLEFunctionalities
    {
        public static event EventHandler<string> DeviceConnected;
        public static event EventHandler DeviceDisconnected;

        private static BluetoothLEDevice _connectedDevice;
        private BluetoothLEAdvertisementWatcher _watcher;
        private GattCharacteristic _targetCharacteristic;
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

        public event EventHandler<string> StatusUpdated;

        public ObservableCollection<string> Devices { get; set; } = new ObservableCollection<string>();

        public BLEFunctionalities()
        {
            // Initialize any necessary components here
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        }

        public void StartBLEScan()
        {
            Devices.Clear();
            _watcher = new BluetoothLEAdvertisementWatcher();
            UpdateStatus("Scanning for BLE devices...");
            _watcher.Received += Watcher_Received;
            _watcher.Start();
        }

        private void UpdateStatus(string message)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                StatusUpdated?.Invoke(this, message);
            });
        }

        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                if (args.Advertisement == null)
                {
                    Debug.WriteLine("Received advertisement with null Advertisement data.");
                    return;
                }

                string deviceName = args.Advertisement.LocalName ?? "Unknown Device";
                Debug.WriteLine($"Device Address: {args.BluetoothAddress:X12}");
                Debug.WriteLine($"Device Name: {deviceName}");

                if (args.Advertisement.ServiceUuids != null)
                {
                    foreach (var serviceUuid in args.Advertisement.ServiceUuids)
                    {
                        Debug.WriteLine($"Advertising Service UUID: {serviceUuid}");
                    }
                }

                if (args.Advertisement.ManufacturerData != null)
                {
                    foreach (var manufacturerData in args.Advertisement.ManufacturerData)
                    {
                        Debug.WriteLine($"Manufacturer ID: {manufacturerData.CompanyId}");
                    }
                }

                // Use the _dispatcherQueue field initialized in the constructor
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    if (!string.IsNullOrEmpty(deviceName) && !Devices.Contains(deviceName))
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

        public async Task PairAndConnectToDeviceAsync(string deviceName)
        {
            try
            {
                var deviceSelector = BluetoothLEDevice.GetDeviceSelectorFromDeviceName(deviceName);
                var devices = await DeviceInformation.FindAllAsync(deviceSelector);
                UpdateStatus($"Connecting to {deviceName}...");

                if (devices.Count > 0)
                {
                    var deviceInfo = devices[0];
                    var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);

                    if (bluetoothLeDevice != null)
                    {
                        var pairingResult = await bluetoothLeDevice.DeviceInformation.Pairing.PairAsync();
                        if (pairingResult.Status == DevicePairingResultStatus.Paired || pairingResult.Status == DevicePairingResultStatus.AlreadyPaired)
                        {
                            UpdateStatus($"Successfully paired or already paired with {deviceName}.");
                            _connectedDevice = bluetoothLeDevice;
                            DeviceConnected?.Invoke(this, deviceName);
                            await DiscoverServicesAsync(bluetoothLeDevice);
                        }
                        else
                        {
                            Debug.WriteLine($"Failed to pair with {deviceName}. Status: {pairingResult.Status}");
                            UpdateStatus($"Failed to pair with {deviceName}. Status: {pairingResult.Status}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to connect to {deviceName}.");
                        UpdateStatus($"Failed to connect to {deviceName}.");
                    }
                }
                else
                {
                    Debug.WriteLine($"Device {deviceName} not found.");
                    UpdateStatus($"Device {deviceName} not found.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error connecting to {deviceName}: {ex.Message}");
                UpdateStatus($"Error connecting to {deviceName}: {ex.Message}");
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
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"Failed to discover services for {bluetoothLeDevice.Name}.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error discovering services: {ex.Message}");
            }
        }

        public async Task WriteDataAsync(byte[] data)
        {
            if (_targetCharacteristic != null)
            {
                var writer = new DataWriter();
                writer.WriteBytes(data);
                await _targetCharacteristic.WriteValueAsync(writer.DetachBuffer());
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
    }
}