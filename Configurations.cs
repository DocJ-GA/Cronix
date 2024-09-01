using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;

namespace Cronix
{
    /// <summary>
    /// Holds configuration information for a cron job.
    /// </summary>
    public class Configurations : IConfigurations
    {
        protected string _healthChecksUrl;
        protected string _logPath;

        /// <summary>
        /// The path of the log files. Default is "log/cron".
        /// </summary>
        public string LogPath
        {
            get => _logPath;
            set
            {
                _logPath = value.Trim();
                if (!_logPath.EndsWith('/'))
                    _logPath += "/";
            }
        }

        /// <summary>
        /// The log file name to be used.
        /// </summary>
        public string LogName { get; set; } = "current.log";

        /// <summary>
        /// The amount of megabytes in the log file can reach before rotating. Default is 10.
        /// </summary>
        public float LogMaxSize { get; set; } = 10;

        /// <summary>
        /// The amount of days the log file can be in age before rotating. Default is 90.
        /// </summary>
        public int LogMaxAge { get; set; } = 90;

        /// <summary>
        /// The total amount of log files to keep.
        /// </summary>
        public int LogMaxCount { get; set; } = 10;

        /// <summary>
        /// The path of the PSF file. Default is "psf/cron.psf".
        /// </summary>
        public string PID { get; set; }

        /// <summary>
        /// A flag to say the program is running in debug mode.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// The string representation of the healthchecks url.
        /// </summary>
        public string HealthChecksUrl
        {
            get => _healthChecksUrl;
            set
            {
                HealthChecksUri = new Uri(value);
                if (string.IsNullOrWhiteSpace(value))
                    HealthChecksUri = null;
                _healthChecksUrl = value;
            }
        }

        /// <summary>
        /// The URI to use for healthchecks.
        /// </summary>
        [IgnoreDataMember]
        public Uri? HealthChecksUri { get; set; }

        /// <summary>
        /// Gets the file path of the log file. Read only.
        /// </summary>
        [IgnoreDataMember]
        public string Log
        {
            get
            {
                if (!LogPath.EndsWith("/"))
                    LogPath += "/";
                return LogPath + "/" + LogName;
            }
        }

        /// <summary>
        /// The directory of the PSF file.
        /// 
        /// </summary>
        [IgnoreDataMember]
        public string PIDDirectory
        {
            get => Path.GetDirectoryName(PID) ?? string.Empty;
        }

        /// <summary>
        /// Initializes the configuration.
        /// </summary>
        public Configurations()
        {
            _logPath = "log/";
            PID = "pid/cron.pid";
            Debug = false;
            _healthChecksUrl = "";
        }

        /// <summary>
        /// Loads the configuration of a TOML file and creats the configuration object.
        /// </summary>
        /// <param name="path">The path to the configuration file.</param>
        /// <returns>The created configuration file.</returns>

        public static Configurations LoadConfig(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("The configuration file '" + path + "' was not found.");
            var rawConfig = File.ReadAllText(path);
            var config = Toml.ToModel<Configurations>(rawConfig);
            return config;
        }

    }
}
