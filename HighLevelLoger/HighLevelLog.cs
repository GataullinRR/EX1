using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using MVVMUtilities.Types;
using WPFControls;
using Logging;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows;

namespace Common
{
    /// <summary>
    /// Thread safe
    /// </summary>
    public static class HighLevelLog
    {
        /// <summary>
        /// Will be populated from dispatcher context
        /// </summary>
        public static event EventHandler<ValueEventArgs<LogEntry>> EntryAdded;

        internal static void LogIf(bool condition, LogType type, string message)
        {
            LogIf(condition, type, DateTime.Now, message);
        }
        internal static void LogIf(bool condition, LogType type, DateTime time, string message)
        {
            if (condition)
            {
                Log(type, time, message);
            }
        }
        internal static void Log(LogType type, string message)
        {
            Log(type, DateTime.Now, message);
        }
        internal static void Log(LogType type, DateTime time, string message)
        {
            var entry = new LogEntry(type, time, message);
            Application.Current.Dispatcher?.Invoke(() =>
            {
                EntryAdded?.Invoke(null, entry);
            });
        }
    }
}
