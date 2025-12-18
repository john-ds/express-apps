using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls.Primitives;
using Newtonsoft.Json;

namespace ExpressControls
{
    public class LoggingService
    {
        public readonly string SessionID = Guid.NewGuid().ToString();
        public string MainPageID { get; private set; } = "";
        public ExpressApp App { get; set; } = ExpressApp.Express;

        private string Endpoint = "https://api.johnjds.co.uk/api/collect";
        private readonly Timer LogTimer = new(TimeSpan.FromMinutes(10));
        private static readonly HttpClient Client = new();
        private int Retries = 3;

        private readonly Dictionary<string, ClickLogEvent> PendingClickEvents = [];
        private readonly Dictionary<string, Timer> ClickTimers = [];
        private readonly object ClickLock = new();

#if DEBUG
        private readonly bool IsEnabledOnDebug = false;
#endif

        public bool IsEnabled
        {
            get { return LogTimer.Enabled; }
        }

        private string LogPath
        {
            get
            {
                var logFileName = $"{SessionID}.log";
                return Path.Combine(Path.GetTempPath(), logFileName);
            }
        }

        public void EnableLogging()
        {
#if DEBUG
            if (!IsEnabledOnDebug)
                return;
            else
                Endpoint = "http://localhost:3000/api/collect";
#endif
            LogTimer.Start();
        }

        public void DisableLogging()
        {
            LogTimer.Stop();

            lock (ClickLock)
            {
                foreach (var timer in ClickTimers.Values)
                {
                    timer?.Stop();
                    timer?.Dispose();
                }
                ClickTimers.Clear();
                PendingClickEvents.Clear();
            }

            try
            {
                if (File.Exists(LogPath))
                    File.Delete(LogPath);
            }
            catch { }
        }

        public void LogEvent(LogEvent eventInfo)
        {
            if (eventInfo is EntranceLogEvent entranceEvent && string.IsNullOrEmpty(MainPageID))
                MainPageID = entranceEvent.PageID;

            if (!IsEnabled)
                return;

            try
            {
                if (eventInfo is ClickLogEvent clickEvent)
                {
                    HandleClickEvent(clickEvent);
                }
                else
                {
                    File.AppendAllTextAsync(
                        LogPath,
                        JsonConvert.SerializeObject(eventInfo) + Environment.NewLine
                    );
                }
            }
            catch { }
        }

        private void HandleClickEvent(ClickLogEvent clickEvent)
        {
            lock (ClickLock)
            {
                string clickKey =
                    $"{clickEvent.PageID}|{clickEvent.ElementID}|{clickEvent.ElementText}";

                if (PendingClickEvents.TryGetValue(clickKey, out ClickLogEvent? value))
                {
                    value.Count++;

                    if (ClickTimers.TryGetValue(clickKey, out Timer? timer))
                    {
                        timer.Stop();
                        timer.Start();
                    }
                }
                else
                {
                    PendingClickEvents[clickKey] = clickEvent;

                    var timer = new Timer(5000) { AutoReset = false };
                    timer.Elapsed += (sender, e) => FlushClickEvent(clickKey);
                    ClickTimers[clickKey] = timer;
                    timer.Start();
                }
            }
        }

        private void FlushClickEvent(string clickKey)
        {
            lock (ClickLock)
            {
                if (PendingClickEvents.TryGetValue(clickKey, out ClickLogEvent? clickEvent))
                {
                    try
                    {
                        File.AppendAllTextAsync(
                            LogPath,
                            JsonConvert.SerializeObject(clickEvent) + Environment.NewLine
                        );
                    }
                    catch { }

                    PendingClickEvents.Remove(clickKey);
                    if (ClickTimers.TryGetValue(clickKey, out Timer? value))
                    {
                        value.Dispose();
                        ClickTimers.Remove(clickKey);
                    }
                }
            }
        }

        public async Task LogApplicationExit()
        {
            if (!IsEnabled)
                return;

            FlushAllPendingClickEvents();
            await Push(true);
        }

        private void FlushAllPendingClickEvents()
        {
            lock (ClickLock)
            {
                foreach (var kvp in PendingClickEvents.ToList())
                {
                    try
                    {
                        File.AppendAllTextAsync(
                            LogPath,
                            JsonConvert.SerializeObject(kvp.Value) + Environment.NewLine
                        );
                    }
                    catch { }
                }

                foreach (var timer in ClickTimers.Values)
                {
                    timer?.Stop();
                    timer?.Dispose();
                }
                ClickTimers.Clear();
                PendingClickEvents.Clear();
            }
        }

        private async Task Push(bool applicationExit = false)
        {
            try
            {
                if (applicationExit)
                {
                    LogTimer.Stop();
                    FlushAllPendingClickEvents();
                }

                if (File.Exists(LogPath))
                {
                    IEnumerable<string> logs = (await File.ReadAllLinesAsync(LogPath)).Where(x =>
                        !string.IsNullOrWhiteSpace(x)
                    );

                    if (!applicationExit)
                    {
                        var pendingLogs = new List<string>();
                        lock (ClickLock)
                        {
                            foreach (var clickEvent in PendingClickEvents.Values)
                            {
                                try
                                {
                                    pendingLogs.Add(JsonConvert.SerializeObject(clickEvent));
                                }
                                catch { }
                            }
                        }
                        logs = logs.Concat(pendingLogs);
                    }

                    if (!logs.Any())
                    {
                        File.Delete(LogPath);
                        return;
                    }

                    string logContent = $"[{string.Join(",", logs)}]";
                    HttpResponseMessage response = await Client.PostAsync(
                        Endpoint,
                        new StringContent(logContent)
                    );

                    if (applicationExit || response.IsSuccessStatusCode || Retries <= 0)
                    {
                        File.Delete(LogPath);
                        Retries = 3;
                    }
                    else
                    {
                        Retries--;
                    }
                }
            }
            catch { }
        }

        private async void OnLogTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!IsEnabled)
            {
                LogTimer.Stop();
                return;
            }
            await Push();
        }

        public LoggingService()
        {
            LogTimer.Elapsed += OnLogTimerElapsed;
        }
    }

    public static class LoggingProperties
    {
        public class Conversion
        {
            public const string WebsiteVisit = "website_visit";
            public const string HelpGuideVisit = "help_guide_visit";
            public const string UpdatePageVisit = "update_page_visit";
            public const string ImportSettings = "import_settings";
            public const string ExportSettings = "export_settings";
            public const string CreateChart = "create_chart";
            public const string CreateDrawing = "create_drawing";
            public const string CreateShape = "create_shape";
            public const string CreateText = "create_text";
            public const string CreateScreenshot = "create_screenshot";
            public const string CreateFontCategory = "create_font_category";
            public const string AccountConnected = "account_connected";
            public const string FileSaved = "file_saved";
            public const string FileExported = "file_exported";
            public const string PhotoEdited = "photo_edited";
            public const string SlideshowStarted = "slideshow_started";
            public const string APIRequest = "api_request";
            public const string CreateSoundtrack = "create_soundtrack";
        }

        public static readonly DependencyProperty DisableLoggingProperty =
            DependencyProperty.RegisterAttached(
                "DisableLogging",
                typeof(bool),
                typeof(LoggingProperties)
            );

        public static void SetDisableLogging(ButtonBase element, bool value) =>
            element.SetValue(DisableLoggingProperty, value);

        public static bool GetDisableLogging(ButtonBase element) =>
            (bool)element.GetValue(DisableLoggingProperty);
    }

    public abstract class LogEvent(ExpressApp app, string sessionID, string pageID)
    {
        [JsonProperty("app")]
        public string App { get; } =
            app switch
            {
                ExpressApp.Type => "type",
                ExpressApp.Present => "present",
                ExpressApp.Font => "font",
                ExpressApp.Quota => "quota",
                _ => "express",
            };

        [JsonProperty("session")]
        public string SessionID { get; } = sessionID;

        [JsonProperty("page")]
        public string PageID { get; } = pageID;

        [JsonProperty("type")]
        public abstract string Type { get; }

        [JsonProperty("datetime")]
        public string Timestamp { get; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
    }

    public class EntranceLogEvent(
        ExpressApp app,
        string sessionID,
        string pageID,
        string windowName,
        string windowTitle,
        int windowWidth,
        int windowHeight,
        int loadTime
    ) : LogEvent(app, sessionID, pageID)
    {
        public override string Type { get; } = "entrance";

        [JsonProperty("country")]
        public string Country { get; } = RegionInfo.CurrentRegion.TwoLetterISORegionName;

        [JsonProperty("timezone")]
        public string Timezone { get; } = TimeZoneInfo.Local.Id;

        [JsonProperty("hostname")]
        public string Hostname { get; } = Funcs.GetCurrentAppName();

        [JsonProperty("browser")]
        public string Browser { get; } = ".NET";

        [JsonProperty("browser_version")]
        public string BrowserVersion { get; } = Environment.Version.ToString();

        [JsonProperty("device_category")]
        public string DeviceCategory { get; } = "desktop";

        [JsonProperty("architecture")]
        public string Architecture { get; } = "amd64";

        [JsonProperty("language")]
        public string Language { get; } = Funcs.GetCurrentLang().ToLower();

        [JsonProperty("os")]
        public string OS { get; } = "Windows";

        [JsonProperty("os_version")]
        public string OSVersion { get; } = Environment.OSVersion.Version.ToString();

        [JsonProperty("platform")]
        public string Platform { get; } = "windows";

        [JsonProperty("path")]
        public string Path { get; set; } = windowName;

        [JsonProperty("title")]
        public string Title { get; set; } = windowTitle;

        [JsonProperty("resolution")]
        public string Resolution { get; set; } = $"{windowWidth}x{windowHeight}";

        [JsonProperty("logged_in")]
        public bool LoggedIn { get; } = false;

        [JsonProperty("version")]
        public string Version { get; } = Funcs.GetCurrentAppVersion();

        [JsonProperty("load_time")]
        public int LoadTime { get; set; } = loadTime;
    }

    public class ExitLogEvent(ExpressApp app, string sessionID, string pageID)
        : LogEvent(app, sessionID, pageID)
    {
        public override string Type { get; } = "exit";
    }

    public class ClickLogEvent(
        ExpressApp app,
        string sessionID,
        string pageID,
        string elementID,
        string elementText,
        int xPosition,
        int yPosition
    ) : LogEvent(app, sessionID, pageID)
    {
        public override string Type { get; } = "click";

        [JsonProperty("link_url")]
        public string ElementID { get; set; } = elementID;

        [JsonProperty("link_text")]
        public string ElementText { get; set; } = elementText;

        [JsonProperty("count")]
        public int Count { get; set; } = 1;

        [JsonProperty("position")]
        public string Position { get; set; } = $"{xPosition},{yPosition}";
    }

    public class DownloadLogEvent(
        ExpressApp app,
        string sessionID,
        string pageID,
        string url,
        string description = ""
    ) : LogEvent(app, sessionID, pageID)
    {
        public override string Type { get; } = "download";

        [JsonProperty("link_url")]
        public string LinkUrl { get; set; } = url;

        [JsonProperty("link_data")]
        public string LinkDescription { get; set; } = description;
    }

    public class ConversionLogEvent(
        ExpressApp app,
        string sessionID,
        string pageID,
        string conversionType,
        string conversionData = ""
    ) : LogEvent(app, sessionID, pageID)
    {
        public override string Type { get; } = "conversion";

        [JsonProperty("conversion_type")]
        public string ConversionType { get; set; } = conversionType;

        [JsonProperty("conversion_data")]
        public string ConversionData { get; set; } = conversionData;
    }

    public class ErrorLogEvent(
        ExpressApp app,
        string sessionID,
        string pageID,
        string message,
        string source
    ) : LogEvent(app, sessionID, pageID)
    {
        public override string Type { get; } = "error";

        [JsonProperty("error_message")]
        public string ErrorMessage { get; set; } = message;

        [JsonProperty("error_source")]
        public string ErrorSource { get; set; } = source;
    }
}
