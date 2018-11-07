using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieldServices.Logging.Runner
{
    public class LogMessage
    {
        public static string DateFormat = @"yyyy.MM.dd HH\:mm\:ss\:fff zzz";

        [JsonIgnore]
        public DateTime LogDate { get; set; }

        [JsonProperty(PropertyName = "date")]
        public string FormattedDate { get { return LogDate.ToString(DateFormat); } }

        [JsonProperty(PropertyName = "appName")]
        public string AppName { get; set; }

        [JsonProperty(PropertyName = "logger")]
        public string SprocName { get; set; }

        [JsonProperty(PropertyName = "user")]
        public string User { get; set; }

        [JsonProperty(PropertyName = "level")]
        public string Level { get; set; }

        [JsonProperty(PropertyName = "machinename")]
        public string MachineName { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "exception")]
        public string Exception { get; set; }

        [JsonIgnore]
        public string DetailsJson { get; set; }


        public string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
