using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

#if NETFX_CORE
using System.Reflection;
#endif

namespace PlayFab.Internal
{
    public static class PlayFabUtil
    {
        static PlayFabUtil() { }

        private static string _localSettingsFileName = "playfab.local.settings.json";
        public static readonly string[] _defaultDateTimeFormats = new string[]{ 

            "yyyy-MM-ddTHH:mm:ss.FFFFFFZ",
            "yyyy-MM-ddTHH:mm:ss.FFFFZ",
            "yyyy-MM-ddTHH:mm:ss.FFFZ", 
            "yyyy-MM-ddTHH:mm:ss.FFZ",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-dd HH:mm:ssZ", 

            "yyyy-MM-dd HH:mm:ss.FFFFFF",
            "yyyy-MM-dd HH:mm:ss.FFFF",
            "yyyy-MM-dd HH:mm:ss.FFF",
            "yyyy-MM-dd HH:mm:ss.FF", 
            "yyyy-MM-dd HH:mm:ss",

            "yyyy-MM-dd HH:mm.ss.FFFF",
            "yyyy-MM-dd HH:mm.ss.FFF",
            "yyyy-MM-dd HH:mm.ss.FF",
            "yyyy-MM-dd HH:mm.ss",
        };
        public const int DEFAULT_UTC_OUTPUT_INDEX = 2; 
        public const int DEFAULT_LOCAL_OUTPUT_INDEX = 9; 
        public static DateTimeStyles DateTimeStyles = DateTimeStyles.RoundtripKind;

        public static string timeStamp
        {
            get { return DateTime.Now.ToString(_defaultDateTimeFormats[DEFAULT_LOCAL_OUTPUT_INDEX]); }
        }

        public static string utcTimeStamp
        {
            get { return DateTime.UtcNow.ToString(_defaultDateTimeFormats[DEFAULT_UTC_OUTPUT_INDEX]); }
        }

        public static string Format(string text, params object[] args)
        {
            return args.Length > 0 ? string.Format(text, args) : text;
        }

        [ThreadStatic]
        private static StringBuilder _sb;

        public static string ReadAllFileText(string filename)
        {
            if (!File.Exists(filename))
            {
                return string.Empty;
            }

            if (_sb == null)
            {
                _sb = new StringBuilder();
            }
            _sb.Length = 0;

            using (var fs = new FileStream(filename, FileMode.Open))
            {
                using (var br = new BinaryReader(fs))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        _sb.Append(br.ReadChar());
                    }
                }
            }

            return _sb.ToString();
        }

        public static T TryEnumParse<T>(string value, T defaultValue)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), value);
            }
            catch (InvalidCastException)
            {
                return defaultValue;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Enum cast failed with unknown error: " + e.Message);
                return defaultValue;
            }
        }

#if UNITY_2017_1_OR_NEWER
        internal static string GetLocalSettingsFileProperty(string propertyKey)
        {
            string envFileContent = null;

            string currDir = Directory.GetCurrentDirectory();
            string currDirEnvFile = Path.Combine(currDir, _localSettingsFileName);

            if (File.Exists(currDirEnvFile))
            {
                envFileContent = ReadAllFileText(currDirEnvFile);
            }
            else
            {
                string tempDir = Path.GetTempPath();
                string tempDirEnvFile = Path.Combine(tempDir, _localSettingsFileName);

                if (File.Exists(tempDirEnvFile))
                {
                    envFileContent = ReadAllFileText(tempDirEnvFile);
                }
            }

            if (!string.IsNullOrEmpty(envFileContent))
            {
                var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
                var envJson = serializer.DeserializeObject<Dictionary<string, object>>(envFileContent);
                try
                {
                    object result;
                    if (envJson.TryGetValue(propertyKey, out result))
                    {
                        return result == null ? null : result.ToString();
                    }

                    return null;
                }
                catch (KeyNotFoundException)
                {
                    return string.Empty;
                }
            }
            return string.Empty;
        }
#endif
    }
}
