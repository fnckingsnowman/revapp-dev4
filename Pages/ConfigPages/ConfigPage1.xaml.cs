using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace RevoluteConfigApp.Pages.ConfigPages
{
    public class ReportModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public byte[] Report { get; set; }
    }

    public sealed partial class ConfigPage1 : Page
    {
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
                // Get the absolute path to report.json
                string filePath = Path.Combine(AppContext.BaseDirectory, "report.json");

                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"report.json not found at {filePath}");
                    return;
                }

                // Read the JSON file
                string jsonText = await File.ReadAllTextAsync(filePath);
                System.Diagnostics.Debug.WriteLine($"report.json contents: {jsonText}");

                // Deserialize JSON into the reports list
                var reports = JsonSerializer.Deserialize<List<ReportModel>>(jsonText);

                // Update the UI
                Reports.Clear();
                foreach (var report in reports)
                {
                    Reports.Add(report);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {Reports.Count} reports.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load reports: {ex}");
            }
        }



    }
}
