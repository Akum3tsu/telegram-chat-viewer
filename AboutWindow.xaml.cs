using System.Reflection;
using System.Windows;

namespace TelegramChatViewer
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadVersionInfo();
        }

        private void LoadVersionInfo()
        {
            // Get version from assembly
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                VersionLabel.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 