using Windows.Devices.Bluetooth.Advertisement;

namespace RevoluteConfigApp.Pages
{
    public class DeviceInfo
    {
        public string Name { get; set; }
        public bool IsPaired { get; set; }
        public BluetoothLEAdvertisementReceivedEventArgs AdvertisementArgs { get; set; }
    }
}