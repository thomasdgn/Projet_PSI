using System.Windows;

namespace VisualisationWPF
{
    public partial class App : Application
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        public App()
        {
            AllocConsole();
            InitializeComponent();
        }
    }
}
