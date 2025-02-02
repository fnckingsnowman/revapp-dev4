using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Windows;
using Microsoft.UI.Xaml.Input;
using RevoluteConfigApp.Pages;

namespace RevoluteConfigApp
{
    public partial class MainWindow : Window
    {
        private int _configCounter = 1; // Counter to keep track of the number of configurations added

        public MainWindow()
        {
            InitializeComponent();
            nvSample.ItemInvoked += NvSample_ItemInvoked;
            BLEPage.DeviceConnected += OnDeviceConnected;
            BLEPage.DeviceDisconnected += OnDeviceDisconnected;

            // Add event handler for the "Add Configuration" button
            var addConfigItem = nvSample.FooterMenuItems[0] as NavigationViewItem;
            if (addConfigItem != null)
            {
                addConfigItem.Tapped += AddConfigItem_Tapped;
            }
        }

        private void NvSample_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                // Handle settings item invocation if needed
            }
            else
            {
                var selectedItem = args.InvokedItemContainer as NavigationViewItem;

                // Check if the selected item is the "Add Configuration" button
                if (selectedItem != null && selectedItem.Content.ToString() == "Add Configuration")
                {
                    // Do nothing here, as the "Add Configuration" button is handled separately
                    return;
                }

                if (selectedItem != null && selectedItem.Tag != null) // Ensure Tag is not null
                {
                    string pageTag = selectedItem.Tag.ToString();
                    Type pageType = null;

                    switch (pageTag)
                    {
                        case "Discover":
                            pageType = typeof(Pages.DiscoverPage);
                            break;
                        case "BLEPage":
                            pageType = typeof(Pages.BLEPage);
                            break;
                        case "ConfigPage1":
                            pageType = typeof(Pages.ConfigPages.ConfigPage1);
                            break;
                        default:
                            // Handle dynamic configuration pages
                            if (pageTag.StartsWith("ConfigPage"))
                            {
                                pageType = typeof(Pages.ConfigPages.ConfigPage1);
                            }
                            break;
                    }

                    if (pageType != null)
                    {
                        contentFrame.Navigate(pageType);
                    }
                }
            }
        }

        private void OnDeviceConnected(object sender, string deviceName)
        {
            ConnectedDeviceNameTextBlock.Text = deviceName;
        }

        private void OnDeviceDisconnected(object sender, EventArgs e)
        {
            ConnectedDeviceNameTextBlock.Text = "No device connected";
            var blePage = contentFrame.Content as BLEPage;
            if (blePage != null)
            {
                blePage.UpdateOutputText("Device has disconnected.");
            }
        }

        private void AddConfigItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Create a new NavigationViewItem for the new configuration
            var newConfigItem = new NavigationViewItem
            {
                Content = $"Config {_configCounter}",
                Tag = $"ConfigPage{_configCounter}", // Set a unique Tag for the new item
                Icon = new FontIcon { Glyph = "\uE700" } // You can change the icon if needed

            };

            // Add the new item to the NavigationView
            nvSample.MenuItems.Add(newConfigItem);

            // Increment the counter for the next configuration
            _configCounter++;
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            BLEPage.DisconnectDevice();
            ConnectedDeviceNameTextBlock.Text = "No device connected";
        }
    }
}
