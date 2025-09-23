using System.Collections.Generic;

namespace Litenbib.Models
{
    public class LocalConfig
    {
        public bool ThemeIndex { get; set; }

        public List<string> RecentFiles { get; set; } = [];
    }
}
