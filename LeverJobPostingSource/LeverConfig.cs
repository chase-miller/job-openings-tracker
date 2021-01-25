using System.Collections.Generic;

namespace OpeningsTracker.JobPostingSources.Lever
{
    public class LeverConfig
    {
        public IList<string> PostingsUris { get; set; } = new List<string>();
        public IList<string> DepartmentBlacklist { get; set; } = new List<string>();
        public IList<string> TeamBlacklist { get; set; } = new List<string>();
    }
}