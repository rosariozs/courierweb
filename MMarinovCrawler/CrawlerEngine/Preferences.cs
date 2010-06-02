using System;

namespace MMarinov.WebCrawler
{
    /// <summary>
    /// Retrieve data from web.config (or app.config)
    /// </summary>
    public static class Preferences
    {
        #region Private Fields

        private static int _RecursionLimit;
        private static string _UserAgent;
        private static string _RobotUserAgent;
        private static int _RequestTimeout;
        private static int _SummaryCharacters;
        private static string _workingPath;

        #endregion

        /// <summary>
        /// Load preferences from *.config (web.config for ASPX, app.config for the console program)
        /// </summary>
        static Preferences()
        {
            _RecursionLimit = IfNullDefault("RecursionLimit", 200);
            _UserAgent = IfNullDefault("UserAgent", "Mozilla/6.0 (MSIE 6.0; Windows NT 5.1; MMarinov.NET; robot)");
            _RobotUserAgent = IfNullDefault("RobotUserAgent", "MMarinov");
            _RequestTimeout = IfNullDefault("RequestTimeout", 5);
            _SummaryCharacters = IfNullDefault("SummaryChars", 350);
        }

        /// <summary>
        /// Seconds to wait for a page to respond, before giving up. 
        /// Default: 5 seconds
        /// </summary>
        public static int RequestTimeout
        {
            get
            {
                return _RequestTimeout; // IfNullDefault("RequestTimeout", 5);
            }
        }

        public static bool IndexOnlyHTMLDocuments
        {
            get
            {
                return IfNullDefault("IndexOnlyHTMLDocuments", true);
            }
        }

        public static int ThreadsCount
        {
            get
            {
                return IfNullDefault("ThreadsCount", 4);
            }
        }

        /// <summary>
        /// Limit to the number of 'levels' of links to follow
        /// Default: 200 
        /// </summary>
        public static int RecursionLimit
        {
            get
            {
                return _RecursionLimit; // IfNullDefault("RecursionLimit", 200);
            }
        }

        /// <summary>
        /// Zip file that contains XLS with 1000000 most famous sites
        /// </summary>
        public static string SeedURLsSource
        {
            get
            {
                return IfNullDefault("SeedURLsSource", "");
            }
        }

        /// <summary>
        /// Whether to use stemming (English only)
        /// Default: Off
        /// </summary>
        public static bool StemmingModeEnabled
        {
            get
            {
                return IfNullDefault("StemmingModeEnabled", false);
            }
        }

        /// <summary>
        /// Whether to use stemming (English only), and if so, what mode [ Off | Short | List ]
        /// Default: Short
        /// </summary>
        public static MMarinov.WebCrawler.Stopper.StoppingModes StoppingMode
        {
            get
            {
                switch (IfNullDefault("StoppingType", "Short"))
                {
                    case "Off":
                        return MMarinov.WebCrawler.Stopper.StoppingModes.Off;
                    case "Short":
                        return MMarinov.WebCrawler.Stopper.StoppingModes.Short;
                    case "List":
                        return MMarinov.WebCrawler.Stopper.StoppingModes.List;
                }

                return MMarinov.WebCrawler.Stopper.StoppingModes.Short;
            }
        }

        /// <summary>
        /// Number of characters to include in 'file summary'
        /// Default: 350
        /// </summary>
        public static int SummaryCharacters
        {
            get
            {
                return _SummaryCharacters; // IfNullDefault("SummaryChars", 350);
            }
        }

        /// <summary>
        /// User Agent sent with page requests, in case you wish to change it
        /// Default: Mozilla/6.0 (MSIE 6.0; Windows NT 5.1; MMarinov.NET; robot)
        /// </summary>
        public static string UserAgent
        {
            get
            {
                return _UserAgent; // IfNullDefault("UserAgent", "Mozilla/6.0 (MSIE 6.0; Windows NT 5.1; MMarinov.NET; robot)");
            }
        }

        /// <summary>
        /// User Agent detected by robots
        /// Default: MMarinov
        /// </summary>
        public static string RobotUserAgent
        {
            get
            {
                return _RobotUserAgent; // IfNullDefault("RobotUserAgent", "MMarinov");
            }
        }

        public static string TempPath
        {
            get
            {
                if (!System.IO.Directory.Exists(WorkingPath + "\\Temp"))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(WorkingPath + "\\Temp");
                    }
                    catch (Exception e)
                    {
                        MMarinov.WebCrawler.Report.Logger.ErrorLog(new Exception("Directory " + WorkingPath + "\\Temp" + " is invalid. WorkingPath is set to default directory.", e));
                        //_workingPath = System.Web.HttpContext.Current.Server.MapPath("~/");
                        _workingPath = System.Reflection.Assembly.GetEntryAssembly().Location.Substring(0, System.Reflection.Assembly.GetEntryAssembly().Location.LastIndexOf('\\') - 1);
                    }
                }

                return WorkingPath + "\\Temp";
            }
        }

        public static string WorkingPath
        {
            get
            {
                _workingPath = IfNullDefault("WorkingPath", "");
                if (_workingPath == "")
                {
                    _workingPath = System.Reflection.Assembly.GetEntryAssembly().Location.Substring(0, System.Reflection.Assembly.GetEntryAssembly().Location.LastIndexOf('\\') - 1);
                    //_workingPath = System.Web.HttpContext.Current.Server.MapPath("~/");
                }
                else
                {
                    if (!System.IO.Directory.Exists(_workingPath))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(_workingPath);
                        }
                        catch (Exception e)
                        {
                            MMarinov.WebCrawler.Report.Logger.ErrorLog(new Exception("Directory " + _workingPath + " is invalid. WorkingPath is set to default directory.", e));
                            _workingPath = System.Reflection.Assembly.GetEntryAssembly().Location.Substring(0, System.Reflection.Assembly.GetEntryAssembly().Location.LastIndexOf('\\') - 1);
                            //_workingPath = System.Web.HttpContext.Current.Server.MapPath("~/");
                        }
                    }
                }

                return _workingPath;
            }
        }

        /// <summary>
        /// Language to use when none is supplied (or supplied language is not available)
        /// Default: en-US
        /// </summary>
        public static string DefaultLanguage
        {
            get
            {
                return IfNullDefault("DefaultLanguage", "en");
            }
        }

        #region Private Methods: IfNullDefault
        private static int IfNullDefault(string appSetting, int defaultValue)
        {
            return System.Configuration.ConfigurationManager.AppSettings[appSetting] == null ? defaultValue : Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings[appSetting]);
        }
        private static string IfNullDefault(string appSetting, string defaultValue)
        {
            return System.Configuration.ConfigurationManager.AppSettings[appSetting] == null ? defaultValue : System.Configuration.ConfigurationManager.AppSettings[appSetting];
        }
        private static bool IfNullDefault(string appSetting, bool defaultValue)
        {
            return System.Configuration.ConfigurationManager.AppSettings[appSetting] == null ? defaultValue : Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings[appSetting]);
        }
        #endregion

    }  // Preferences class
}
