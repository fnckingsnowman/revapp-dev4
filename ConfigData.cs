using System.Collections.Generic;
using System.Linq;

public class ConfigData
{
    public string Name { get; set; }
    public string Tag { get; set; }

    // Change byte[] to List<int> for serialization as integers
    public List<int> LeftReport { get; set; }
    public List<int> RightReport { get; set; }
    public string LeftTransport { get; set; }
    public string RightTransport { get; set; }

    // Constructor to initialize List<int> from byte[] if necessary
    public void SetLeftReport(byte[] report)
    {
        LeftReport = report?.Select(b => (int)b).ToList();
    }

    public void SetRightReport(byte[] report)
    {
        RightReport = report?.Select(b => (int)b).ToList();
    }
}
