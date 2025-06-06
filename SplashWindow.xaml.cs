using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TelegramChatViewer
{
    public partial class SplashWindow : Window
    {
        private DispatcherTimer _progressTimer;
        private double _currentProgress = 0;

        public SplashWindow()
        {
            InitializeComponent();
            LoadVersionInfo();
            StartProgressAnimation();
        }

        private void LoadVersionInfo()
        {
            try
            {
                string versionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
                if (File.Exists(versionPath))
                {
                    string version = File.ReadAllText(versionPath).Trim();
                    VersionText.Text = $"v{version}";
                }
            }
            catch
            {
                VersionText.Text = "v1.0.2";
            }
        }

        private void StartProgressAnimation()
        {
            _progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _progressTimer.Tick += ProgressTimer_Tick;
            _progressTimer.Start();
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            _currentProgress += 2;
            
            if (_currentProgress <= 100)
            {
                ProgressBar.Width = (_currentProgress / 100.0) * 200;
                
                // Update status based on progress
                if (_currentProgress < 20)
                    StatusText.Text = "Initializing services...";
                else if (_currentProgress < 40)
                    StatusText.Text = "Loading performance optimizations...";
                else if (_currentProgress < 60)
                    StatusText.Text = "Setting up user interface...";
                else if (_currentProgress < 80)
                    StatusText.Text = "Preparing media handlers...";
                else if (_currentProgress < 95)
                    StatusText.Text = "Finalizing startup...";
                else
                    StatusText.Text = "Ready!";
            }
            else
            {
                _progressTimer.Stop();
            }
        }

        public void UpdateProgress(double progress, string status = "")
        {
            Dispatcher.Invoke(() =>
            {
                _currentProgress = Math.Max(_currentProgress, progress);
                ProgressBar.Width = (_currentProgress / 100.0) * 200;
                
                if (!string.IsNullOrEmpty(status))
                {
                    StatusText.Text = status;
                }
            });
        }

        public async Task SimulateStartup()
        {
            await Task.Delay(1500); // Minimum splash time for good UX
            
            // Fade out animation
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, e) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }

        public void ShowMainWindow()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }
    }
} 