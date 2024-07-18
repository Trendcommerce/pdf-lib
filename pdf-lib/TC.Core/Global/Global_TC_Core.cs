using System.Drawing;
using TC.Classes;
using TC.Data;
using TC.Interfaces;

namespace TC.Global
{
    // Global Objects (11.11.2022, SME)
    public static class Global_TC_Core
    {
        // Global Debug-Info-Handler (11.11.2022, SME)
        // DONT initialize straight away, otherwise error will be thrown as soon as code access this class, as well in design-mode, like for GlobalUserInfo in UserStatusItem (03.02.2024, SME)
        private static Debug.DebugInfoHandler _GlobalDebugInfoHandler;
        public static Debug.DebugInfoHandler GlobalDebugInfoHandler
        {
            get
            {
                if (_GlobalDebugInfoHandler == null)
                {
                    _GlobalDebugInfoHandler = new Debug.DebugInfoHandler();
                }
                return _GlobalDebugInfoHandler;
            }
        }

        // Global User-Settings (03.07.2023, SME)
        // DONT initialize straight away, otherwise error will be thrown as soon as code access this class, as well in design-mode, like for GlobalUserInfo in UserStatusItem (03.02.2024, SME)
        private static XmlSettingFile _GlobalUserSettings;
        public static XmlSettingFile GlobalUserSettings
        {
            get
            {
                if (_GlobalUserSettings == null)
                {
                    _GlobalUserSettings = new XmlSettingFile();
                }
                return _GlobalUserSettings;
            }
        }

        // Global App-Settings (05.07.2023, SME)
        // DONT initialize straight away, otherwise error will be thrown as soon as code access this class, as well in design-mode, like for GlobalUserInfo in UserStatusItem (03.02.2024, SME)
        private static XmlAppSettingFile _GlobalAppSettings;
        public static XmlAppSettingFile GlobalAppSettings
        {
            get
            {
                if (_GlobalAppSettings == null)
                {
                    _GlobalAppSettings = new XmlAppSettingFile();
                }
                return _GlobalAppSettings;
            }
        }

        // CustomerId (02.01.2024, SME)
        public static string CustomerId { get; set; }

        // Customer-Database-Prefix (22.03.2024, SME)
        public static string CustomerDatabasePrefix { get; set; }

        // Global User-Info (03.02.2024, SME)
        private static IUserInfo _GlobalUserInfo;
        public static IUserInfo GlobalUserInfo
        {
            get
            {
                if (_GlobalUserInfo == null)
                {
                    _GlobalUserInfo = new WindowsUser();
                }
                return _GlobalUserInfo;
            }
            set
            {
                _GlobalUserInfo = value;
            }
        }

    }
}
