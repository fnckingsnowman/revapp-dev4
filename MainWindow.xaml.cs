using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml.Input;
using RevoluteConfigApp.Pages;

namespace RevoluteConfigApp
{
    public partial class MainWindow : Window
    {
        private const string ConfigFilePath = "configurations.json"; // JSON storage file
        private List<ConfigData> _configPages = new();

        public MainWindow()
        {
            InitializeComponent();
            nvSample.ItemInvoked += NvSample_ItemInvoked;
            BLEPage.DeviceConnected += OnDeviceConnected;
            BLEPage.DeviceDisconnected += OnDeviceDisconnected;

            LoadConfigurations();

            // Attach event to "Add Configuration" button
            var addConfigItem = nvSample.FooterMenuItems[0] as NavigationViewItem;
            if (addConfigItem != null)
            {
                addConfigItem.Tapped += AddConfigItem_Tapped;
            }
        }

        private void LoadConfigurations()
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                _configPages = JsonSerializer.Deserialize<List<ConfigData>>(json) ?? new List<ConfigData>();

                foreach (var config in _configPages)
                {
                    AddConfigTab(config, false); // Load saved tabs without modifying JSON
                }
            }
        }

        private void SaveConfigurations()
        {
            string json = JsonSerializer.Serialize(_configPages, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }

        private void NvSample_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked) return;

            var selectedItem = args.InvokedItemContainer as NavigationViewItem;
            if (selectedItem != null && selectedItem.Tag != null)
            {
                string pageTag = selectedItem.Tag.ToString();

                Type pageType = null;
                object pageParameter = null; // Store unique config info

                switch (pageTag)
                {
                    case "Discover":
                        pageType = typeof(Pages.DiscoverPage);
                        break;
                    case "BLEPage":
                        pageType = typeof(Pages.BLEPage);
                        break;
                    default:
                        if (pageTag.StartsWith("ConfigPage"))
                        {
                            pageType = typeof(Pages.ConfigPages.ConfigPage1); // Use a single template page
                            pageParameter = pageTag; // Pass the unique identifier to the page
                        }
                        break;
                }

                if (pageType != null)
                {
                    contentFrame.Navigate(pageType, pageParameter);
                }
            }
        }


        private void AddConfigItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            string configName = $"Config {_configPages.Count + 1}";
            string pageTag = $"ConfigPage{_configPages.Count + 1}";

            var configData = new ConfigData { Name = configName, Tag = pageTag };
            _configPages.Add(configData);
            SaveConfigurations();

            AddConfigTab(configData);
        }

        private void AddConfigTab(ConfigData config, bool save = true)
        {
            var newConfigItem = new NavigationViewItem
            {
                Content = config.Name,
                Tag = config.Tag,
                Icon = new FontIcon { Glyph = "\uE700" }
            };

            newConfigItem.RightTapped += ConfigItem_RightTapped;
            nvSample.MenuItems.Add(newConfigItem);

            if (save)
            {
                SaveConfigurations();
            }
        }

        private void ConfigItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var configItem = sender as NavigationViewItem;
            if (configItem != null)
            {
                var flyout = new MenuFlyout();

                var renameItem = new MenuFlyoutItem { Text = "Rename" };
                renameItem.Click += (s, args) => RenameConfigItem(configItem);

                var deleteItem = new MenuFlyoutItem { Text = "Delete" };
                deleteItem.Click += (s, args) => DeleteConfigItem(configItem);

                flyout.Items.Add(renameItem);
                flyout.Items.Add(deleteItem);

                flyout.ShowAt(configItem, e.GetPosition(configItem));
            }
        }

        private void RenameConfigItem(NavigationViewItem configItem)
        {
            var textBox = new TextBox
            {
                Text = configItem.Content.ToString(),
                Width = 200
            };

            textBox.LostFocus += (s, e) => ConfirmRename(configItem, textBox);
            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    ConfirmRename(configItem, textBox);
                }
            };

            configItem.Content = textBox;
            textBox.Focus(FocusState.Programmatic);
        }

        private void ConfirmRename(NavigationViewItem configItem, TextBox textBox)
        {
            string newName = textBox.Text;
            configItem.Content = newName;

            var config = _configPages.Find(c => c.Tag == configItem.Tag.ToString());
            if (config != null)
            {
                config.Name = newName;
                SaveConfigurations();
            }
        }

        private void DeleteConfigItem(NavigationViewItem configItem)
        {
            _configPages.RemoveAll(c => c.Tag == configItem.Tag.ToString());
            SaveConfigurations();

            if (contentFrame.Content is Page currentPage && currentPage.Tag?.ToString() == configItem.Tag?.ToString())
            {
                contentFrame.Content = null;
            }

            nvSample.MenuItems.Remove(configItem);
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

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            BLEPage.DisconnectDevice();
            ConnectedDeviceNameTextBlock.Text = "No device connected";
        }
    }

    public class ConfigData
    {
        public string Name { get; set; }
        public string Tag { get; set; }
    }
}
