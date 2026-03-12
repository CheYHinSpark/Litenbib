using Avalonia;
using Avalonia.Controls;
using System.Collections.Generic;

namespace Litenbib.Models
{
    public class LocalConfig
    {
        public bool ThemeIndex { get; set; }

        public double? WindowWidth { get; set; }

        public double? WindowHeight { get; set; }

        public double? WindowPositionX { get; set; }

        public double? WindowPositionY { get; set; }

        public WindowState? WindowState { get; set; }

        public int SelectedTabIndex { get; set; } = -1;

        public List<RecentFileState> RecentFiles { get; set; } = [];
    }

    public class RecentFileState
    {
        public string FilePath { get; set; } = string.Empty;

        public int FilterMode { get; set; }

        public string FilterField { get; set; } = "Whole";

        public string FilterText { get; set; } = string.Empty;
    }
}
