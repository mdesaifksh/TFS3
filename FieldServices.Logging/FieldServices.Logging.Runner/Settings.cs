using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieldServices.Logging.Runner
{
    public static class Settings
    {
        public static DateTime GetLastDate()
        {
            var filepath = System.Configuration.ConfigurationManager.AppSettings["Settings.File"];
            if (!File.Exists(filepath))
            {
                throw new Exception($"Settings file not found: FilePath:{filepath};");
            }
                        
            var text = File.ReadAllText(filepath);

            if (!DateTime.TryParseExact(text, LogMessage.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
            {
                throw new Exception($"Failed to parse last date: Data:{text};");
            }

            return dateValue;
        }

        public static void SaveLastDate(DateTime lastDate)
        {
            var filepath = System.Configuration.ConfigurationManager.AppSettings["Settings.File"];
            if (!File.Exists(filepath))
            {
                throw new Exception($"Settings file not found: FilePath:{filepath};");
            }

            File.WriteAllText(filepath, lastDate.ToString(LogMessage.DateFormat));            
        }
    }
}
