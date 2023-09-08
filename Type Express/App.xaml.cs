using ExpressControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Type_Express.Properties;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static System.Reflection.FieldInfo? _menuDropAlignmentField;

        void App_Startup(object sender, StartupEventArgs e)
        {
            // File args
            Settings.Default.Files.Clear();
            Settings.Default.Save();

            foreach (var item in e.Args)
                if (File.Exists(item))
                    Settings.Default.Files.Add(item);

            Settings.Default.Save();

            // Interface theme
            Funcs.AppThemeTimer.Tick += Funcs.AppThemeTimer_Tick;
            Funcs.AutoDarkModeOn = Settings.Default.AutoDarkOn;
            Funcs.AutoDarkModeOff = Settings.Default.AutoDarkOff;
            Funcs.SetAppTheme((ThemeOptions)Settings.Default.InterfaceTheme);

            // Menu alignment
            _menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            EnsureStandardPopupAlignment();
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;

            Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        private async void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            await Funcs.SendErrorReport(Funcs.GenerateErrorReport(e.Exception));
            Funcs.ShowMessageRes("CriticalErrorDescStr", "CriticalErrorStr", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static void SystemParameters_StaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            EnsureStandardPopupAlignment();
        }

        private static void EnsureStandardPopupAlignment()
        {
            if (SystemParameters.MenuDropAlignment && _menuDropAlignmentField != null)
                _menuDropAlignmentField.SetValue(null, false);
        }
    }
}
