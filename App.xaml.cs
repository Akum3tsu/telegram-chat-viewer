using System;
using System.IO;
using System.Windows;

namespace TelegramChatViewer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Write startup debug info to a simple text file
                var debugPath = Path.Combine(AppContext.BaseDirectory, "debug_startup.log");
                File.AppendAllText(debugPath, $"[{DateTime.Now}] App startup beginning...\n");
                
                base.OnStartup(e);
                
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
    }
} 