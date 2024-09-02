using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Cronix.Extensions;
using System.Text.RegularExpressions;
using System.Reflection.Emit;

namespace Cronix
{
    /// <summary>
    /// A class that holds information for a cron job.
    /// </summary>
    public class Cron
    {
        /// <summary>
        /// The time the cron job was started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The time the cron job stopped.
        /// </summary>
        public DateTime StopTime { get; set; }

        /// <summary>
        /// The configuration file.
        /// </summary>
	public IConfigurations Configs { get; private set; }

        /// <summary>
        /// The title of the cronix application.
        /// </summary>
        public string Identity { get; set; } = "Cron app";

        /// <summary>
        /// The current process Id.
        /// </summary>
        public int PID { get; protected set; } = -1;

        /// <summary>
        /// Constructs the Cron class.
        /// </summary>
        /// <param name="configs">The coniguration to use.</param>
        public Cron(string identity = "Cron app", IConfigurations configs = null)
        {
			Configs = configs ?? new Configurations();
            Identity = identity;
        }

        /// <summary>
        /// Starts the cron app with the message.
        /// </summary>
        /// <param name="message">The message to use for the start of the log. Default is "Starting App."</param>
        public void Start()
        {
            PID = Environment.ProcessId;

            // Start the clock.
            StartTime = DateTime.Now;
            Log("Starting " + Identity + ".");
            if (Configs.HealthChecksUri != null)
            {
                LogInfo("Sending healthchecks start.");
                HttpClient client = new HttpClient();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("rid", PID.ToString()),
                });
                var result = client.PostAsync(Configs.HealthChecksUri.ToString().TrimEnd('/') + "/start", content).Result;
            }

            // First we check if all the directories are set.

            // Check to see if log path exists. If not, we create it.
            if (!Directory.Exists(Configs.LogPath))
            {
                LogInfo("The log directory does not exist, attempting to create it.");
                try
                {
                    Directory.CreateDirectory(Configs.LogPath);
                    LogInfo("Log directory created successfully.");
                } catch (Exception ex)
                {
                    Debug("Could not create the log path ('" + Configs.LogPath + "') with message: '" + ex.Message + "'");
                    Log("Could not create the log path.", Level.Err);
                    Fail(exitCode: 2);
                }
            }

            // Check PID directory.
            if (!Directory.Exists(Path.GetDirectoryName(Configs.PID)))
            {
                LogInfo("The PID directory doesn't exists, attempting to create it.");
                // It doesn't exist, lets make it.
                var dir = Path.GetDirectoryName(Configs.PID);
                if (dir != null)
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                        LogInfo("PID created successfully.");
                    } catch (Exception ex)
                    {
                        Debug("Could not create the pid path ('" + dir + "') with message: '" + ex.Message + "'");
                        Debug("You might need to create pid directory and ensure it has write permissions for user running application.");
                        Log("Could not create pid path.", Level.Err);
                        Fail(exitCode: 3);
                    }
                }
            }

            // Lets see if the process is running.
            if (File.Exists(Configs.PID))
            {
                // The file exists, it may still be running.
                var storedPid = File.ReadAllText(Configs.PID);
                if (ProcessIsRunning(storedPid))
                {
                    Log("The process is currently running. Exiting.", level: Level.Warning);
                    Environment.Exit(5);
                }
                else
                {
                    Log("The PID is assumed to be orphaned or not running.");
                    LogInfo("Attempting to remove pid file.");
                    try
                    {
                        File.Delete(Configs.PID);
                        LogInfo("The PID file was deleted successfully.");
                    }
                    catch (Exception ex)
                    {
                        Debug("Could not delete PID file ('" + Configs.PID + "') with message: '" + ex.Message + "'");
                        Log("Exiting with error. Could not remove PID file.", Level.Err);
                        Fail(exitCode: 4);
                    }
                }
            }

            // Creating the pid file.
            Log("Process id is '" + PID + "'.");
            Log("Creating PID file.");
            try
            {
                File.WriteAllText(Configs.PID, PID.ToString());
                LogInfo("PID file created successfuly.");
            }
            catch (Exception ex)
            {
                Debug("Could not create PID file at ('" + Configs.PID + "') with message: '" + ex.Message + "'");
                Log("Exiting with error. Could not write PID file.", Level.Err);
                Fail(exitCode: 4);
            }

            // Starting the log.
            if (File.Exists(Configs.Log))
            {
                // Log file exists. Checking size.
                var info = new FileInfo(Configs.Log);
                if (info.Length > Configs.LogMaxSize * 1024 * 1024 || info.CreationTime > DateTime.Now.AddDays(Configs.LogMaxAge))
                {
                    // Log file is too large or older than the specified time.
                    Log("Log file set for rotation.");
                    LogInfo("Rotating log file.");
                    var newName = Configs.LogPath + Configs.LogName + ".old." + DateTime.Now.ToString("s");
                    Debug("Attempting to move log file to '" + newName + "'.");
                    try
                    {
                        File.Move(Configs.Log, Configs.LogPath + "/" + Configs.LogName + ".old." + DateTime.Now.ToString("s"));
                        Debug("File moved successfully.");
                    }
                    catch (Exception ex)
                    {
                        Debug("Could not move log file from '" + Configs.Log + "' to '" + newName + "' with message: '" + ex.Message + "'.");
                        Log("Exiting with error. Could not move log file.", Level.Err);
                        Fail(exitCode: 1);
                    }

                    LogInfo("Creating new log file.");
                    CreateLogFile();
                }
            }

            // Getting list of log files
            var oldLogs = new List<string>();
            try
            {
                oldLogs = Directory.GetFiles(Configs.LogPath, Configs.LogName + ".old.*").Select(p => Path.GetFileName(p)).ToList();
                Debug("Obtained log files (" + oldLogs.Count + ") from '" + Configs.LogPath + "'.");
            }
            catch (Exception ex)
            {
                Debug("Could not get file list from the log path ('" + Configs.LogPath + "') with message: '" + ex.Message + "'");
                Log("Could not get directory list of the log path.", Level.Err);
                Environment.Exit(2);
            }

            if (oldLogs.Count > Configs.LogMaxCount)
            {
                LogInfo("The log limit is set at " + Configs.LogMaxCount + " and there are " + oldLogs.Count + ".  Deleting excess.");
                // We got too many logs, we will delete oldest to newest until we have five.
                var orderedList = new SortedList<DateTime, string>();
                foreach (var fileName in oldLogs)
                {
                    var match = long.Parse(Regex.Match(fileName, Configs.LogName + @"\.old\.(.+)").Groups[1].Value);
                    var date = new DateTime(match);
                    orderedList.Add(date, fileName);
                }

                foreach (var entry in orderedList.Take(orderedList.Count - 5))
                {
                    try
                    {
                        Debug("Attempting to delete file " + entry.Value + ".");
                        File.Delete(Configs.LogPath + entry.Value);
                    }
                    catch (Exception ex)
                    {
                        Debug("Could not delete log file at '" + entry.Value + "' with message: '" + ex.Message + "'.");
                        Log("Exiting with error. Could not create log file.", Level.Err);
                        Fail(exitCode: 2);
                    }
                }
            }

        }

        /// <summary>
        /// Fails the cron app and writes the fail message.
        /// </summary>
        /// <param name="message">The message to write to the logs. Default is "Ending cron app in a failed state."</param>
        public void Fail(string message = "Ending cron app in a failed state.", int exitCode = 1)
        {
            Log(message, Level.Err);
			File.WriteAllText(Configs.PID, "failed");
            RunTime();
            if (Configs.HealthChecksUri != null)
            {
                LogInfo("Sending healthchecks fail.");
                HttpClient client = new HttpClient();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("rid", PID.ToString()),
                });
                _ = client.PostAsync(Configs.HealthChecksUri.ToString().TrimEnd('/') + "/fail", content).Result;
            }

            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Marks the job as complete
        /// </summary>
        /// <param name="message"></param>
        public void Complete(string message = "Processs complete.")
        {
            Log(message);
            Debug("Attempting to remove the pid file.");
			try
            {
                File.Delete(Configs.PID);
                LogInfo("Pid file removed.");
            }
            catch (Exception ex)
            {
                Debug("Failed to remove the pid file at '" + Configs.PID + "' with message: '" + ex.Message + "'.");
                Log("Failed to remove the pid file.", Level.Err);
                Fail(exitCode: 2);
            }
            RunTime();
            if (Configs.HealthChecksUri != null)
            {
                LogInfo("Sending healthchecks start.");
                HttpClient client = new HttpClient();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("rid", PID.ToString()),
                });
                _ = client.PostAsync(Configs.HealthChecksUri, content).Result;
            }

        }

        /// <summary>
        /// Logs the runtime for the cron application.
        /// </summary>
        public void RunTime()
        {
            StopTime = DateTime.Now;
            var runTime = StopTime - StartTime;
            LogInfo("Total Run time: " + runTime.ToReadableString() + ".");
        }

        /// <summary>
        /// Logs a message into the cron application.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The priority level of the log.  Default is <seealso cref="Level.Info"/></param>
        /// <param name="identity">The identity of the application logging.  Default is "cron_app".</param>
        /// <param name="system">True if this should be logged to the system journal.  Default is "true".</param>
        public void Log(string message, Level level = Level.Info, bool system = true)
        {
            if (system)
                Syslog.Write(message, level, Identity);
            File.AppendAllText(Configs.Log, DateTime.Now.ToString("s") + "[" + level + "]: " + message + "\n");
        }

        /// <summary>
        /// Logs a message as info just to the cron apps log file and not the system journal.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInfo(string message)
        {
            Log(message, level: Level.Info, system: false);
        }

        /// <summary>
        /// Logs a message as debug.  If debug is set to false (it is by default) it will log nothing.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Debug(string message)
        {
            if (Configs.Debug)
                Log(message, Level.Debug, system: false);
        }

        protected bool ProcessIsRunning(int pid)
        {
            try
            {
                System.Diagnostics.Process.GetProcessById(pid);
                return true;
            } catch(ArgumentException)
            {
                return false;
            }
        }

        protected bool ProcessIsRunning(string pid)
        {
            try
            {
                return ProcessIsRunning(int.Parse(pid));
            } catch (ArgumentNullException)
            {
                Debug("The pid supplied was null.");
            } catch (FormatException)
            {
                Debug("The pid supplied wasn't a number.");
            } catch (OverflowException)
            {
                Debug("The pid provided was to large.");
            }
            return false;
        }

        protected void CreateLogFile()
        {
            try
            {
                File.AppendAllText(Configs.Log, DateTime.Now.ToString("s") + ": Log file created.");
                Debug("Log file created successfuly.");
            }
            catch (Exception ex)
            {
                Debug("Could not create log file at '" + Configs.Log + "' with message: '" + ex.Message + "'.");
                Log("Exiting with error. Could not create log file.", Level.Err);
                Environment.Exit(2);
            }
        }
    }
}


// Exit Codes
// 1: Log directory permission errors.
// 2: Log file permission errors.
// 3: PID directory permission errors.
// 4: PID file permission errors.
// 5: Process already running.
