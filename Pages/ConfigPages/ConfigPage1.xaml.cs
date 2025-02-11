using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            _bleFunctionalities = new BLEFunctionalities(); // Initialize BLEFunctionalities
            LoadReportsAsync();
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
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RevoluteConfigApp", "configurations.json");

                if (File.Exists(filePath))
                {
                    string jsonText = File.ReadAllText(filePath);
                    var configDataDict = JsonSerializer.Deserialize<Dictionary<string, ConfigData>>(jsonText);

                    if (configDataDict != null && configDataDict.TryGetValue(ConfigId, out var configData))
                    {
                        var leftReport = configData.LeftReport?.Select(b => (byte)b).ToList() ?? new List<byte>();
                        var rightReport = configData.RightReport?.Select(b => (byte)b).ToList() ?? new List<byte>();
                        var leftTransport = configData.LeftTransport;
                        var rightTransport = configData.RightTransport;
                        var clockwiseSensitivity = configData.ClockwiseSensitivity;
                        var anticlockwiseSensitivity = configData.AnticlockwiseSensitivity;
                        var deadzoneSensitivity = configData.DeadzoneSensitivity;

                        var deadzoneHex = Convert.ToByte(deadzoneSensitivity);
                        var anticlockwiseIdentPerRev = Convert.ToByte(anticlockwiseSensitivity);
                        var clockwiseIdentPerRev = Convert.ToByte(clockwiseSensitivity);
                        var leftTransportByte = Convert.ToByte(leftTransport);
                        var rightTransportByte = Convert.ToByte(rightTransport);

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


    }
}
