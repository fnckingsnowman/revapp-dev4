using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml.Input;
using RevoluteConfigApp.Pages;
using RevoluteConfigApp.Pages.ConfigPages;

namespace RevoluteConfigApp
{
    public partial class MainWindow : Window
    {
        private int _configCounter = 1; // Counter for configurations
        private Dictionary<string, ConfigData> configNames = new(); // Stores config names and reports
        private static readonly string ConfigFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RevoluteConfigApp", "configurations.json");
        private List<ConfigData> _configPages = new();

        public MainWindow()
        {
            InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null);
            nvSample.ItemInvoked += NvSample_ItemInvoked;
            BLEPage.DeviceConnected += OnDeviceConnected;
            BLEPage.DeviceDisconnected += OnDeviceDisconnected;

            LoadConfigurations();

            // Attach event to "Add Configuration" button
            if (nvSample.FooterMenuItems.Count > 0 && nvSample.FooterMenuItems[0] is NavigationViewItem addConfigItem)
            {
                addConfigItem.Tapped += AddConfigItem_Tapped;
            }
        }

        private void LoadConfigurations()
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                configNames = JsonSerializer.Deserialize<Dictionary<string, ConfigData>>(json) ?? new();

                //  Remove only the config-related items, keeping Discover, BLE, Search, etc.
                var itemsToRemove = new List<NavigationViewItem>();

                foreach (var item in nvSample.MenuItems)
                {
                    if (item is NavigationViewItem navItem && navItem.Tag != null && navItem.Tag.ToString().StartsWith("ConfigPage"))
                    {
                        itemsToRemove.Add(navItem);
                    }
                }

                foreach (var item in itemsToRemove)
                {
                    nvSample.MenuItems.Remove(item);
                }

                _configPages.Clear(); //  Reset list to match saved data

                foreach (var entry in configNames)
                {
                    var configData = entry.Value;
                    _configPages.Add(configData);
                    AddConfigTab(configData); //  Re-add items properly with updated names
                }

                _configCounter = _configPages.Count + 1;
            }
        }

        private void SaveConfigurations()
        {
            string configDirectory = Path.GetDirectoryName(ConfigFilePath);

            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory); // Ensure directory exists
            }

            var configDataDict = new Dictionary<string, ConfigData>();
            foreach (var config in _configPages)
            {
                configDataDict[config.Tag] = config;
            }

            string jsonString = JsonSerializer.Serialize(configDataDict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, jsonString);
        }

        private void NvSample_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked) return;

            if (args.InvokedItemContainer is NavigationViewItem selectedItem && selectedItem.Tag != null)
            {
                string pageTag = selectedItem.Tag.ToString();

                Type pageType = pageTag switch
                {
                    "Discover" => typeof(Pages.DiscoverPage),
                    "BLEPage" => typeof(Pages.BLEPage),
                    _ => pageTag.StartsWith("ConfigPage") ? typeof(ConfigPage1) : null
                };

                if (pageType != null)
                {
                    var page = Activator.CreateInstance(pageType) as ConfigPage1;
                    contentFrame.Navigate(pageType, new ConfigPageParameters(pageTag, selectedItem.Content.ToString()));

                    // Subscribe to the ReportSelected event
                    if (page != null)
                    {
                        page.ReportSelected += (string side, List<byte> report) =>
                        {
                            if (side == "Left")
                                configNames[pageTag].LeftReport = report.ToArray();
                            else if (side == "Right")
                                configNames[pageTag].RightReport = report.ToArray();

                            SaveConfigurations();
                        };
                    }
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

        private void AddConfigTab(ConfigData config)
        {
            var newConfigItem = new NavigationViewItem
            {
                Content = config.Name,
                Tag = config.Tag,
                Icon = new FontIcon { Glyph = "\uE700" }
            };

            newConfigItem.RightTapped += ConfigItem_RightTapped;
            nvSample.MenuItems.Add(newConfigItem);
        }

        private void ConfigItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is NavigationViewItem configItem)
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
            string newName = textBox.Text.Trim();
            configItem.Content = newName;
            string configId = configItem.Tag.ToString();

            if (configNames.ContainsKey(configId))
            {
                configNames[configId].Name = newName; // Update the dictionary
            }

            // Update the corresponding ConfigData object in _configPages
            var configData = _configPages.Find(c => c.Tag == configId);
            if (configData != null)
            {
                configData.Name = newName;
            }

            SaveConfigurations(); // Ensure the new name persists after restart

            // Notify ConfigPage1 to update title dynamically
            if (contentFrame.Content is ConfigPage1 currentPage && currentPage.ConfigId == configId)
            {
                currentPage.UpdateConfigName(newName);
            }
        }

        private void DeleteConfigItem(NavigationViewItem configItem)
        {
            string configId = configItem.Tag.ToString();
            _configPages.RemoveAll(c => c.Tag == configId);
            configNames.Remove(configId);
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
            if (contentFrame.Content is BLEPage blePage)
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

    public class ConfigPageParameters
    {
        public string ConfigId { get; }
        public string ConfigName { get; }

        public ConfigPageParameters(string id, string name)
        {
            ConfigId = id;
            ConfigName = name;
        }
    }
}
