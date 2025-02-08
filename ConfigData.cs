using System.Collections.Generic;
using System.Linq;

public class ConfigData
{
    public string Name { get; set; }
    public string Tag { get; set; }

    // Store reports as List<int> for correct JSON format
    public List<int> LeftReport { get; set; }
    public List<int> RightReport { get; set; }

    // Store the transport type
    public string LeftTransport { get; set; }
    public string RightTransport { get; set; }

    // Store the description separately
    public string LeftDescription { get; set; }
    public string RightDescription { get; set; }

    public double ClockwiseSensitivity { get; set; } = 30;  // Default value
    public double AnticlockwiseSensitivity { get; set; } = 30;  // Default value
    public double DeadzoneSensitivity { get; set; } = 1;  // Default value


    // Helper methods to convert byte[] to List<int> and save separately
    public void SetLeftReport(byte[] report)
    {
        LeftReport = report?.Select(b => (int)b).ToList();
    }

    public void SetRightReport(byte[] report)
    {
        RightReport = report?.Select(b => (int)b).ToList();
    }
}
