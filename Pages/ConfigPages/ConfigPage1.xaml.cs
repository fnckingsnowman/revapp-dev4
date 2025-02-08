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
        public event Action<string, List<byte>> ReportSelected; // Event for report selection

        public string ConfigId { get; private set; }
        public ObservableCollection<ReportModel> Reports { get; private set; } = new();

        public ConfigPage1()
        {
            this.InitializeComponent();
            this.DataContext = this;
            LoadReportsAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is ConfigPageParameters parameters)
            {
                ConfigId = parameters.ConfigId;
                ConfigTitle.Text = parameters.ConfigName;
            }
        }

        public void UpdateConfigName(string newName)
        {
            if (ConfigTitle != null)
            {
                ConfigTitle.Text = newName;
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

                // Log assignment to output console
                string reportDataString = string.Join(", ", report.Report);
                System.Diagnostics.Debug.WriteLine($"Report '{report.Name}' with data [{reportDataString}] was assigned to Left.");

                // Notify MainWindow of the selected report data
                ReportSelected?.Invoke("Left", report.Report);
            }
        }

        private void OnRightButtonClicked(object sender, object e)
        {
            if (sender is Button button && button.DataContext is ReportModel report)
            {
                ClockwiseActDisplay.Content = new TextBlock { Text = report.Description, FontSize = 16 };

                // Log assignment to output console
                string reportDataString = string.Join(", ", report.Report);
                System.Diagnostics.Debug.WriteLine($"Report '{report.Name}' with data [{reportDataString}] was assigned to Right.");

                // Notify MainWindow of the selected report data
                ReportSelected?.Invoke("Right", report.Report);
            }
        }
    }
}
