using Common;
using Ninject;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Utilities.Types;

namespace RUSManagingTool
{
    public partial class App : Application
    {
        static Mutex _mutex = null;

        public App()
        {
            const string appId = "96f478cb-c39d-4d1f-a630-a8ae4f9fe760";
            _mutex = new Mutex(true, appId, out bool createdNew);
            if (!createdNew)
            {
                UserInteracting.ReportError("Запуск программы", "Приложение уже запущено");
                Current.Shutdown();
            }

            var version = Assembly.GetEntryAssembly().GetName().Version;
            var versionString = $"{version.Major}.{version.Minor}.{version.Build}:{version.Revision}";
            Logger.LogInfo(null, $"Программа запущена. V{versionString}");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            Logger.LogError(null, "Необработаное исключение", ex);
            Reporter.ReportErrorAndExit(ex);
        }
    }
}
