using Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;

namespace Common
{
    public static class Logger
    {
        static readonly LockableObject<StreamWriter> _ioLogStream;
        static readonly Logging.Logger _rootLogger;

        static Logger()
        {
            var dirPath = Path.Combine(Environment.CurrentDirectory, "Logs");
            var date = DateTime.Now;
            var path = Path.Combine(dirPath, $"Log for {date.ToString("HH.mm.ss K dd.MM.yyyy").Replace(":", ".")}.txt");
            var storage = new StreamWriter(IOUtils.CreateFile(path, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8);
            _rootLogger = new Logging.Logger(storage);
            var ioLogPath = Path.Combine(dirPath, $"IO Log for {date.ToString("HH.mm.ss K dd.MM.yyyy").Replace(":", ".")}.txt");
            _ioLogStream = new StreamWriter(IOUtils.CreateFile(ioLogPath, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8);
        }

        public static IDisposable Indent => _rootLogger.IndentMode;

        public static void LogOKEverywhere(
            string inAppAndVerboseMessage, [CallerMemberName]string caller = "", [CallerFilePath]string callerFilePath = "")
            => LogOK(inAppAndVerboseMessage, inAppAndVerboseMessage, caller, callerFilePath);
        public static void LogInfoEverywhere(
            string inAppAndVerboseMessage, [CallerMemberName]string caller = "", [CallerFilePath]string callerFilePath = "")
            => LogInfo(inAppAndVerboseMessage, inAppAndVerboseMessage, caller, callerFilePath);
        public static void LogWarningEverywhere(
            string inAppAndVerboseMessage, [CallerMemberName]string caller = "", [CallerFilePath]string callerFilePath = "")
            => LogWarning(inAppAndVerboseMessage, inAppAndVerboseMessage, caller, callerFilePath);
        public static void LogErrorEverywhere(
            string inAppAndVerboseMessage, Exception exception, [CallerMemberName]string caller = "", [CallerFilePath]string callerFilePath = "")
            => LogError(inAppAndVerboseMessage, inAppAndVerboseMessage, exception, caller, callerFilePath);
        public static void LogErrorEverywhere(
            string inAppAndVerboseMessage, [CallerMemberName]string caller = "", [CallerFilePath]string callerFilePath = "")
            => LogError(inAppAndVerboseMessage, inAppAndVerboseMessage, caller, callerFilePath);

        public static void LogOK(
            string inAppMessage, string verboseMessage, [CallerMemberName]string caller = "", [CallerFilePath]string callerFilePath = "")
        {
            _rootLogger.Log(getVerboseMessage(inAppMessage, verboseMessage), LogType.OK, caller, callerFilePath);
            HighLevelLog.LogIf(inAppMessage != null, LogType.OK, getInAppMessage(inAppMessage));
        }
        public static void LogInfo(
            string inAppMessage, string verboseMessage, [CallerMemberName]string caller = "", [CallerFilePath]string callerFilePath = "")
        {
            _rootLogger.Log(getVerboseMessage(inAppMessage, verboseMessage), caller, callerFilePath);
            HighLevelLog.LogIf(inAppMessage != null, LogType.INFO, getInAppMessage(inAppMessage));
        }
        public static void LogWarning(
            string inAppMessage, string verboseMessage, [CallerMemberName]string caller = "", [CallerFilePath]string callerFilePath = "")
        {
            _rootLogger.LogWarning(getVerboseMessage(inAppMessage, verboseMessage), caller, callerFilePath);
            HighLevelLog.LogIf(inAppMessage != null, LogType.WARNING, getInAppMessage(inAppMessage));
        }
        public static void LogError(
            string inAppMessage, string verboseMessage, Exception exception, [CallerMemberName]string caller = "", [CallerFilePath]string callerFilePath = "")
        {
            _rootLogger.LogError(getVerboseMessage(inAppMessage, verboseMessage), exception, caller, callerFilePath);
            HighLevelLog.LogIf(inAppMessage != null, LogType.ERROR, getInAppMessage(inAppMessage));
        }
        public static void LogError(
            string inAppMessage, string verboseMessage, [CallerMemberName]string caller = "", [CallerFilePath]string callerFilePath = "")
        {
            _rootLogger.LogError(getVerboseMessage(inAppMessage, verboseMessage), caller, callerFilePath);
            HighLevelLog.LogIf(inAppMessage != null, LogType.ERROR, getInAppMessage(inAppMessage));
        }

        static string getInAppMessage(string inAppMessage)
        {
            return inAppMessage
                ?.Replace("-NL", Global.NL);
        }
        static string getVerboseMessage(string inAppMessage, string verboseMessage)
        {
            return verboseMessage
                .Replace("-MSG", inAppMessage ?? "")
                .Replace("-NL", Global.NL);
        }

        public static async Task LogRequestAsync(IList<byte> requestBytes)
        {
            var time = DateTime.Now;
            await ThreadingUtils.ContinueAtThreadPull();
            using (await _ioLogStream.Locker.AcquireAsync())
            {
                var data = $"{time.ToString(Logging.Logger.TIME_FORMAT)} Request[{requestBytes.Count}] >> ".PadRight(40, ' ') + requestBytes.Select(b => b.ToString("X2")).Aggregate(" ");
                _ioLogStream.Object.WriteLine(data);
                _ioLogStream.Object.Flush();
            }
        }
        public static async Task LogResponseAsync(IList<byte> responseBytes, int expectedLength)
        {
            var time = DateTime.Now;
            await ThreadingUtils.ContinueAtThreadPull();
            using (await _ioLogStream.Locker.AcquireAsync())
            {
                var data = $"{time.ToString(Logging.Logger.TIME_FORMAT)} Response[{responseBytes.Count} from {expectedLength}] << ".PadRight(40, ' ') + responseBytes.Select(b => b.ToString("X2")).Aggregate(" ");
                _ioLogStream.Object.WriteLine(data);
                _ioLogStream.Object.Flush();
            }
        }
    }
}
