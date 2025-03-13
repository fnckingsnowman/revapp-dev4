using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

namespace RevoluteConfigApp.Pages.ConfigPages
{
    public class ReportModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("report")]
        public List<byte> Report { get; set; }

        [JsonPropertyName("transport")]
        public string Transport { get; set; }
    }

    public sealed partial class ConfigPage1 : Page
    {
        public event Action<string, string, List<byte>, string> ReportSelected; // Updated event

        //private BLEFunctionalities _bleFunctionalities;
        private BLEFunctionalities _bleFunctionalities = BLEFunctionalities.Instance;

        public string ConfigId { get; private set; }
        public ObservableCollection<ReportModel> Reports { get; private set; } = new();

        public ConfigPage1()
        {
            this.InitializeComponent();
            this.DataContext = this;
            _bleFunctionalities = BLEFunctionalities.Instance; // Initialize BLEFunctionalities
            LoadReportsAsync();

            this.PointerPressed += MainWindow_PointerPressed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is ConfigPageParameters parameters)
            {
                ConfigId = parameters.ConfigId;
                ConfigTitle.Text = parameters.ConfigName;
                System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Navigated to {ConfigId} - {parameters.ConfigName}");

                // Load saved descriptions
                LoadSavedDescriptions(ConfigId);
            }
        }

        private void LoadSavedDescriptions(string configId)
        {
            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RevoluteConfigApp", "configurations.json");

                if (File.Exists(filePath))
                {
                    string jsonText = File.ReadAllText(filePath);
                    var configDataDict = JsonSerializer.Deserialize<Dictionary<string, ConfigData>>(jsonText);

                    if (configDataDict != null && configDataDict.TryGetValue(configId, out var configData))
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Loaded descriptions and sensitivity values for {configId}");

                        // Update UI with saved descriptions
                        AnticlockwiseActDisplay.Content = new TextBlock { Text = configData.LeftDescription ?? "---", FontSize = 16 };
                        ClockwiseActDisplay.Content = new TextBlock { Text = configData.RightDescription ?? "---", FontSize = 16 };

                        // Load sensitivity values
                        ClockwiseSens.Value = configData.ClockwiseSensitivity;
                        AnticlockwiseSens.Value = configData.AnticlockwiseSensitivity;
                        DeadzoneSens.Value = configData.DeadzoneSensitivity;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Config {configId} not found in configurations.json.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ConfigPage1] configurations.json not found.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Error loading descriptions: {ex.Message}");
            }
        }


        public void UpdateConfigName(string newName)
        {
            if (ConfigTitle != null)
            {
                ConfigTitle.Text = newName;
                System.Diagnostics.Debug.WriteLine($"Updated ConfigName to: {newName}");
            }
        }

        private async Task LoadReportsAsync()
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "Assets", "report.json");

                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"report.json not found at {filePath}");
                    return;
                }

                string jsonText = await File.ReadAllTextAsync(filePath);
                System.Diagnostics.Debug.WriteLine($"report.json contents: {jsonText}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var reports = JsonSerializer.Deserialize<List<ReportModel>>(jsonText, options);

                Reports.Clear();
                foreach (var report in reports)
                {
                    Reports.Add(report);
                    System.Diagnostics.Debug.WriteLine($"Loaded Report: {report.Name}, Description: {report.Description}, Transport: {report.Transport}, Report: {report.Report}");
                }

                // Initialize the FilteredReports with all reports initially
                FilteredReports.Clear();
                foreach (var report in Reports)
                {
                    FilteredReports.Add(report);
                }

                System.Diagnostics.Debug.WriteLine($"Total reports loaded: {Reports.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load reports: {ex}");
            }
        }

        private void OnLeftButtonClicked(object sender, object e)
        {
            if (sender is Button button && button.DataContext is ReportModel report)
            {
                AnticlockwiseActDisplay.Content = new TextBlock { Text = report.Description, FontSize = 16 };

                string reportDataString = string.Join(", ", report.Report);
                System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Report '{report.Name}' with data [{reportDataString}] was assigned to Left.");

                // Pass the description separately
                ReportSelected?.Invoke("Left", report.Transport, report.Report, report.Description);
            }
        }
        private void OnRightButtonClicked(object sender, object e)
        {
            if (sender is Button button && button.DataContext is ReportModel report)
            {
                ClockwiseActDisplay.Content = new TextBlock { Text = report.Description, FontSize = 16 };

                string reportDataString = string.Join(", ", report.Report);
                System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Report '{report.Name}' with data [{reportDataString}] was assigned to Right.");

                // Pass the description separately
                ReportSelected?.Invoke("Right", report.Transport, report.Report, report.Description);
            }
        }

        private void OnSensitivityChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RevoluteConfigApp", "configurations.json");

                if (File.Exists(filePath))
                {
                    string jsonText = File.ReadAllText(filePath);
                    var configDataDict = JsonSerializer.Deserialize<Dictionary<string, ConfigData>>(jsonText);

                    if (configDataDict != null && configDataDict.TryGetValue(ConfigId, out var configData))
                    {
                        // Update sensitivity values based on which NumberBox changed
                        if (sender == ClockwiseSens)
                        {
                            configData.ClockwiseSensitivity = ClockwiseSens.Value;
                        }
                        else if (sender == AnticlockwiseSens)
                        {
                            configData.AnticlockwiseSensitivity = AnticlockwiseSens.Value;
                        }
                        else if (sender == DeadzoneSens)
                        {
                            configData.DeadzoneSensitivity = DeadzoneSens.Value;
                        }

                        // Save updated configurations
                        string updatedJson = JsonSerializer.Serialize(configDataDict, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(filePath, updatedJson);

                        System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Updated sensitivity values for {ConfigId}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Error saving sensitivity values: {ex.Message}");
            }
        }

        private async void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // First, we need to make sure we're working with the most up-to-date values
                var deadzoneSensitivity = DeadzoneSens.Value;
                var anticlockwiseSensitivity = AnticlockwiseSens.Value;
                var clockwiseSensitivity = ClockwiseSens.Value;

                // Now, let's make sure the BLE device is connected and the target characteristic is valid
                if (_bleFunctionalities.IsDeviceConnected() && _bleFunctionalities.IsTargetCharacteristicNull())
                {
                    await _bleFunctionalities.ReassignTargetCharacteristicAsync();
                }

                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RevoluteConfigApp", "configurations.json");

                if (File.Exists(filePath))
                {
                    string jsonText = File.ReadAllText(filePath);
                    var configDataDict = JsonSerializer.Deserialize<Dictionary<string, ConfigData>>(jsonText);

                    if (configDataDict != null && configDataDict.TryGetValue(ConfigId, out var configData))
                    {
                        // Now make sure the correct report data is being used
                        var leftReport = configData.LeftReport?.Select(b => (byte)b).ToList() ?? new List<byte>();
                        var rightReport = configData.RightReport?.Select(b => (byte)b).ToList() ?? new List<byte>();
                        var leftTransport = configData.LeftTransport;
                        var rightTransport = configData.RightTransport;

                        // Convert sensitivity values to bytes
                        var deadzoneHex = Convert.ToByte(deadzoneSensitivity);
                        var anticlockwiseIdentPerRev = Convert.ToByte(anticlockwiseSensitivity);
                        var clockwiseIdentPerRev = Convert.ToByte(clockwiseSensitivity);

                        var leftTransportByte = Convert.ToByte(leftTransport);
                        var rightTransportByte = Convert.ToByte(rightTransport);

                        // Create the organized byte array with sensitivity values
                        var organizedByteArray = new List<byte>
                {
                    deadzoneHex // 0x01 - Deadzone
                };

                        organizedByteArray.AddRange(leftReport); // Anticlockwise/left_report
                        organizedByteArray.Add(anticlockwiseIdentPerRev); // Anticlockwise/left_identPerRev
                        organizedByteArray.Add(leftTransportByte); // Anticlockwise/left_transport

                        organizedByteArray.AddRange(rightReport); // Clockwise/right_report
                        organizedByteArray.Add(clockwiseIdentPerRev); // Clockwise/right_identPerRev
                        organizedByteArray.Add(rightTransportByte); // Clockwise/right_transport

                        string byteArrayString = string.Join(", ", organizedByteArray.Select(b => $"0x{b:X2}"));
                        System.Diagnostics.Debug.WriteLine($"Organized Byte Array: {byteArrayString}");

                        // Ensure BLEFunctionalities instance is valid before writing
                        if (_bleFunctionalities == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ConfigPage1] BLEFunctionalities instance is null. Unable to send data.");
                            return;
                        }

                        // Write the organized byte array to the BLE device
                        await _bleFunctionalities.WriteDataAsync(organizedByteArray.ToArray());
                        System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Sent byte array to BLE device.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Config {ConfigId} not found in configurations.json.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ConfigPage1] configurations.json not found.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigPage1] Error during configuration: {ex.Message}");
            }
        }
        private ObservableCollection<ReportModel> FilteredReports { get; set; } = new ObservableCollection<ReportModel>();

        // Event handler for when the search text changes
        private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            string searchText = sender.Text.ToLower();
            FilterReports(searchText);
        }

        // Method to filter reports based on the search query
        private void FilterReports(string query)
        {
            FilteredReports.Clear();

            // Filter reports based on the Name or Description
            foreach (var report in Reports)
            {
                if (report.Name.ToLower().Contains(query) || report.Description.ToLower().Contains(query))
                {
                    FilteredReports.Add(report);
                }
            }

            // Update the ItemsSource of the ListView to show filtered reports
            ReportsListView.ItemsSource = FilteredReports;
        }


        private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string searchText = args.QueryText.ToLower();
            FilterReports(searchText);
        }

        // Event handler for Anticlockwise button click
        private void AnticlockwiseButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract the text from the TextBlock inside the button
            var buttonText = (AnticlockwiseActDisplay.Content as TextBlock)?.Text;

            if (buttonText != null)
            {
                // Set the TeachingTip content
                TeachingTipAnticlockwise.Content = buttonText;

                // Set the target of the TeachingTip to the clicked button
                TeachingTipAnticlockwise.Target = AnticlockwiseActDisplay;

                // Open the TeachingTip near the button
                TeachingTipAnticlockwise.IsOpen = true;
            }
            else
            {
                // Handle the case where the button text is not found
                Debug.WriteLine("Error: Anticlockwise button text is null or not set.");
            }
        }

        // Event handler for Clockwise button click
        private void ClockwiseButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract the text from the TextBlock inside the button
            var buttonText = (ClockwiseActDisplay.Content as TextBlock)?.Text;

            if (buttonText != null)
            {
                // Set the TeachingTip content
                TeachingTipClockwise.Content = buttonText;

                // Set the target of the TeachingTip to the clicked button
                TeachingTipClockwise.Target = ClockwiseActDisplay;

                // Open the TeachingTip near the button
                TeachingTipClockwise.IsOpen = true;
            }
            else
            {
                // Handle the case where the button text is not found
                Debug.WriteLine("Error: Clockwise button text is null or not set.");
            }
        }

        // Event handler for clicks anywhere on the window
        private void MainWindow_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Close the TeachingTip if it's open and the click is not on the TeachingTip or its target
            if (TeachingTipAnticlockwise.IsOpen && !IsPointerOverTeachingTipOrTarget(e))
            {
                TeachingTipAnticlockwise.IsOpen = false;
            }

            if (TeachingTipClockwise.IsOpen && !IsPointerOverTeachingTipOrTarget(e))
            {
                TeachingTipClockwise.IsOpen = false;
            }
        }

        // Helper method to check if the pointer is over the TeachingTip or its target
        private bool IsPointerOverTeachingTipOrTarget(PointerRoutedEventArgs e)
        {
            var pointerPosition = e.GetCurrentPoint(this).Position;

            // Check if the pointer is over the TeachingTip
            if (TeachingTipAnticlockwise.IsOpen && IsPointerOverElement(TeachingTipAnticlockwise, pointerPosition))
            {
                return true;
            }

            if (TeachingTipClockwise.IsOpen && IsPointerOverElement(TeachingTipClockwise, pointerPosition))
            {
                return true;
            }

            // Check if the pointer is over the target (button)
            if (IsPointerOverElement(AnticlockwiseActDisplay, pointerPosition) || IsPointerOverElement(ClockwiseActDisplay, pointerPosition))
            {
                return true;
            }

            return false;
        }

        // Helper method to check if the pointer is over a given UI element
        private bool IsPointerOverElement(UIElement element, Windows.Foundation.Point pointerPosition)
        {
            var bounds = element.TransformToVisual(null).TransformBounds(new Windows.Foundation.Rect(0, 0, element.RenderSize.Width, element.RenderSize.Height));
            return bounds.Contains(pointerPosition);
        }

        private async void CustomizeActionButton_Click(object sender, RoutedEventArgs e)
        {
            // Show the ContentDialog
            var result = await CustomizeActionDialog.ShowAsync();

            // Handle the result if needed
            if (result == ContentDialogResult.Primary)
            {
                // User clicked "Save"
                Debug.WriteLine("Save clicked");
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // User clicked "Cancel"
                Debug.WriteLine("Cancel clicked");
            }
            else
            {
                // User clicked "Close" or pressed the escape key
                Debug.WriteLine("Dialog closed");
            }
        }

        private Expander _currentlyExpandedExpander;

        private void OnExpanderExpanding(object sender, ExpanderExpandingEventArgs e)
        {
            if (_currentlyExpandedExpander != null && _currentlyExpandedExpander != sender)
            {
                // Collapse the previously expanded Expander
                _currentlyExpandedExpander.IsExpanded = false;
            }

            // Update the currently expanded Expander
            _currentlyExpandedExpander = sender as Expander;
        }

        private void OnExpanderCollapsed(object sender, ExpanderCollapsedEventArgs e)
        {
            if (_currentlyExpandedExpander == sender)
            {
                // Clear the currently expanded Expander
                _currentlyExpandedExpander = null;
            }
        }
    }
}
