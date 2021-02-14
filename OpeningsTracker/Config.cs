using System.Collections.Generic;

namespace OpeningsTracker.Runners.BackgroundJob
{
    public class Config
    {
        public IEnumerable<string> ActiveSources { get; set; }
        public IEnumerable<string> ActiveNotifiers { get; set; }
    }
}
