using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Cronix
{
    /// <summary>
    /// Holds configuration information for the cron app.  It is loaded from a TOML app.
    /// </summary>
    public interface IConfigurations
    {
        /// <summary>
        /// The path of the log files. Default is "log/".
        /// </summary>
        public string LogPath { get; set; }

        /// <summary>
        /// The log file name to be used.
        /// </summary>
        public string LogName { get; set; }

        /// <summary>
        /// The amount of megabytes in the log file can reach before rotating. Default is 10.
        /// </summary>
        public float LogMaxSize { get; set; }

        /// <summary>
        /// The amount of days the log file can be in age before rotating. Default is 90.
        /// </summary>
        public int LogMaxAge { get; set; }

        /// <summary>
        /// The total amount of log files to keep.
        /// </summary>
        public int LogMaxCount { get; set; }

        /// <summary>
        /// The path of the PSF file. Default is "psf/cron.psf".
        /// </summary>
        public string PID { get; set; }

        /// <summary>
        /// A flag to say the program is running in debug mode.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Gets the file path of the log file. Read only.
        /// </summary>
        public string Log { get; }

        /// <summary>
        /// The string representation of the healthchecks url.
        /// </summary>
        public string HealthChecksUrl { get; set; }

        /// <summary>
        /// The URI to use for healthchecks.
        /// </summary>
        public Uri? HealthChecksUri { get; set; }


        /// <summary>
        /// Loads the configuration of a TOML file and creats the configuration object.
        /// </summary>
        /// <param name="path">The path to the configuration file.</param>
        /// <returns>The created configuration file.</returns>
        /// <exception cref="NotImplementedException">Thrown if not implemented.</exception>
        public static IConfigurations LoadConfig(string path) => throw new NotImplementedException();
    }
}
