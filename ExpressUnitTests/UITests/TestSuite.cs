using System.Diagnostics;
using System.Windows.Automation;

namespace ExpressTests.UITests
{
    public abstract class TestSuite : IDisposable
    {
        public abstract string AppName { get; }

        public readonly Process AppProcess;
        public readonly AutomationElement MainWindow;

        private Process StartApplication()
        {
            string appPath =
                @$"..\..\..\..\{AppName}\bin\Debug\net{Environment.Version.Major}.0-windows\{AppName}.exe";

            if (!File.Exists(appPath))
                throw new FileNotFoundException($"WPF application not found at: {appPath}");

            return Process.Start(
                    new ProcessStartInfo() { FileName = appPath, UseShellExecute = true }
                ) ?? throw new NullReferenceException("Process could not be started");
        }

        private AutomationElement GetMainWindow()
        {
            var timeout = DateTime.Now.AddSeconds(60);

            while (DateTime.Now < timeout)
            {
                var windows = AutomationElement.RootElement.FindAll(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.ProcessIdProperty, AppProcess.Id)
                );

                foreach (AutomationElement window in windows)
                {
                    string windowTitle = window.Current.Name;

                    if (windowTitle == "Please wait while the application opens")
                        continue;

                    if (!string.IsNullOrEmpty(windowTitle))
                        return window;
                }

                Thread.Sleep(500);
            }

            throw new TimeoutException("Main window did not appear within the timeout period");
        }

        void IDisposable.Dispose()
        {
            try
            {
                if (AppProcess != null && !AppProcess.HasExited)
                    AppProcess.Kill();
            }
            catch { }
            finally
            {
                AppProcess?.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        public TestSuite()
        {
            AppProcess = StartApplication();
            MainWindow = GetMainWindow();
        }
    }
}
