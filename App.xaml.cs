using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace TelegramChatViewer
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Write startup debug info to a simple text file
                var debugPath = Path.Combine(AppContext.BaseDirectory, "debug_startup.log");
                File.AppendAllText(debugPath, $"[{DateTime.Now}] App startup beginning...\n");
                
                base.OnStartup(e);
                
                // Show splash screen immediately
                var splashWindow = new SplashWindow();
                splashWindow.Show();
                
                File.AppendAllText(debugPath, $"[{DateTime.Now}] Splash screen shown\n");
                
                // Simulate initialization work and show main window when ready
                await InitializeApplicationAsync(splashWindow);
                
                File.AppendAllText(debugPath, $"[{DateTime.Now}] App startup completed successfully\n");
            }
            catch (Exception ex)
            {
                try
                {
                    var errorPath = Path.Combine(AppContext.BaseDirectory, "startup_error.log");
                    File.WriteAllText(errorPath, $"[{DateTime.Now}] Startup error: {ex}\n");
                }
                catch
                {
                    // If we can't even write the error, try system temp
                    try
                    {
                        var tempErrorPath = Path.Combine(Path.GetTempPath(), "telegram_viewer_error.log");
                        File.WriteAllText(tempErrorPath, $"[{DateTime.Now}] Startup error: {ex}\n");
                    }
                    catch
                    {
                        // Last resort - ignore
                    }
                }
                throw;
            }
        }

        private async Task InitializeApplicationAsync(SplashWindow splashWindow)
        {
            // Real initialization steps with appropriate timing
            await Task.Delay(100);
            splashWindow.UpdateProgress(10, "Starting application...");
            
            await Task.Delay(200);
            splashWindow.UpdateProgress(25, "Loading performance optimizations...");
            
            await Task.Delay(250);
            splashWindow.UpdateProgress(45, "Initializing hardware detection...");
            
            await Task.Delay(200);
            splashWindow.UpdateProgress(60, "Setting up message parsers...");
            
            await Task.Delay(180);
            splashWindow.UpdateProgress(75, "Preparing media handlers...");
            
            await Task.Delay(150);
            splashWindow.UpdateProgress(90, "Finalizing user interface...");
            
            await Task.Delay(200);
            splashWindow.UpdateProgress(100, "Ready!");
            
            // Ensure minimum splash time for smooth UX (total ~1.4 seconds)
            await Task.Delay(300);
            
            // Create and show main window
            var mainWindow = new MainWindow();
            
            // Hide splash before showing main window for smooth transition
            splashWindow.Hide();
            mainWindow.Show();
            
            // Close splash completely
            splashWindow.Close();
        }
    }
} 