using System.Collections.Generic;

namespace OpeningsTracker.JobPostingSources.Lever
{
    public class LeverConfig
    {
        public string PostingsUri { get; set; }
        public IList<string> DepartmentBlacklist { get; set; } = new List<string>();
        public IList<string> TeamBlacklist { get; set; } = new List<string>();
    }
}