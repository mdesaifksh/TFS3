using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Topshelf;

namespace FieldServices.Logging.Runner
{
    /// <summary>
    /// This program fowards logging messages from Hub.dbo.Logging to LogStash - ElasticSearch - Kibana
    /// </summary>
    class Program
    {
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            try
            {
                
                _logger.Info("Running Log Shipper");

                var intervalstr = System.Configuration.ConfigurationManager.AppSettings["Timer.Interval"];
                int interval;
                if (string.IsNullOrWhiteSpace(intervalstr) || !int.TryParse(intervalstr, out interval))
                {
                    throw new ArgumentException($"Timer.Interval app setting not set: Value:{intervalstr};");
                }

                _logger.Info($"Timer Interval: {interval:#,0}");

                var rc = HostFactory.Run(x =>                                   
                {
                    x.Service<LogShipperTimer>(s =>                                   
                    {
                        s.ConstructUsing(name => new LogShipperTimer(interval));                
                        s.WhenStarted(tc => tc.Start());                         
                        s.WhenStopped(tc => tc.Stop());
                        s.WhenShutdown(tc => tc.Stop());
                    });
                    x.RunAsPrompt();                             
                    x.SetDescription("This process ships logs from the DB to Logstash->ElasticSearch->Kibana");
                    x.SetDisplayName("FKH FieldServices.Logging.Runner");
                    x.SetServiceName("FKH FieldServices.Logging.Runner");

                    x.EnableShutdown();
                    x.UseNLog();
                });

                var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
                Environment.ExitCode = exitCode;

                _logger.Info($"Finished Log Shipper");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to foward log messages.");
            }
        }
    }

    public class LogShipperTimer
    {
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        private static int _timesRun = 0;
        readonly System.Timers.Timer _timer;

        public LogShipperTimer(int interval)
        {
            _timer = new System.Timers.Timer(interval) { AutoReset = true };
            _timer.Elapsed += Timer_Elapsed;
        }
        public void Start() { _timer.Start(); }
        public void Stop() { _timer.Stop(); }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Run();
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
                        _logger.Debug($"Pushing Messages Completed: Count:{logmessages.Count};");
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
