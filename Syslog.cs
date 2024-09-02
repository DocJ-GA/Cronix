using System.Runtime.InteropServices;

namespace Cronix
{
    /// <summary>
    /// A class that exposes Linux journal commands.
    /// </summary>
    public class Syslog
    {
        [DllImport("libc")]
        private static extern void openlog(nint ident, Option option, Facility facility);

        [DllImport("libc")]
        private static extern void syslog(int priority, string message);

        [DllImport("libc")]
        private static extern void closelog();

        /// <summary>
        /// Writes a message to the system journal log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The priority of the level being logged.  Default is <seealso cref="Level.Info"/></param>
        /// <param name="identity">The identity of the app.  Default is "cron_app".</param>
        public static void Write(string message, Level level = Level.Info, string identity = "cron_app")
        {
            //are we on linux?
            if (!OperatingSystem.IsLinux()) return;

            //validate input
            if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(identity))
                return;

            nint ident = Marshal.StringToHGlobalAnsi(identity);
            openlog(ident, Option.Console | Option.Pid | Option.PrintError, Facility.User);

            //split multiline messages, otherwise we end up with "line1 #012 line2 #012 line3" etc
            foreach (var line in message.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                syslog((int)Facility.User | (int)level, line.Trim());
            }

            closelog();
            Marshal.FreeHGlobal(ident);
        }

        [Flags]
        private enum Option
        {
            Pid = 0x01,
            Console = 0x02,
            Delay = 0x04,
            NoDelay = 0x08,
            NoWait = 0x10,
            PrintError = 0x20
        }

        [Flags]
        private enum Facility
        {
            User = 1 << 3, //removed other unused enum values for brevity
        }
    }





    [Flags]
    public enum Level
    {
        Emerg = 0,
        Alert = 1,
        Crit = 2,
        Err = 3,
        Warning = 4,
        Notice = 5,
        Info = 6,
        Debug = 7
    }

}
