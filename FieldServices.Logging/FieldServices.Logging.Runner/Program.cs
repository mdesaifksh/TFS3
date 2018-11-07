using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FieldServices.Logging.Runner
{
    /// <summary>
    /// This program fowards logging messages from Hub.dbo.Logging to LogStash - ElasticSearch - Kibana
    /// </summary>
    class Program
    {
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        private static int _timesRun = 0;
        private static ManualResetEvent mre = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            try
            {
                _logger.Info("Running Log Shipper");

                using (var timer = new System.Timers.Timer())
                {
                    Run();

                    timer.Interval = 60000;
                    timer.Elapsed += Timer_Elapsed;
                    timer.Start();

                    _logger.Info("Timer started, waiting for 60 runs.");
                    mre.WaitOne();
                }

                _logger.Info($"Finished Log Shipper Run: Runs:{_timesRun};");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to foward log messages");
            }
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Only Run 60 times, so app restarts every hour or so.
            if (_timesRun >= 60)
            {
                try
                {
                    ((System.Timers.Timer)sender).Stop();
                }
                finally
                {
                    mre.Set();
                }
            }
            else
            {
                Run();
            }
        }

        private static void Run()
        {
            _timesRun += 1;
            _logger.Debug($"Running: Times Run:{_timesRun};");
            try
            {
                //Check last date a message was found and pushed to logstash
                var lastMessageDate = Settings.GetLastDate();
                var logmessages = LoggingDB.GetLogMessagesSinceLastUpdate(lastMessageDate);

                if (logmessages.Count > 0)
                {
                    var max = logmessages.Max(x => x.LogDate);
                    if (max > lastMessageDate)
                    {
                        _logger.Info($"Pushing Messages: Count:{logmessages.Count};");
                        Settings.SaveLastDate(max);
                        UDPSender.Send(logmessages);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to foward log messages. Times Run:{_timesRun};");
            }
        }
    }
}
