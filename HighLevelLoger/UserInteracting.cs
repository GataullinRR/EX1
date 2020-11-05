using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Utilities;

namespace Common
{
    public static class UserInteracting
    {
        public static bool RequestAcknowledgement(string operationName, string scaryMessage)
        {
            scaryMessage = processMessage(scaryMessage);
            var result = MessageBox.Show(
                scaryMessage, operationName, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2, default);

            return result == DialogResult.Yes;
        }

        public static async Task<bool> RequestAcknowledgementAsync(string operationName, string scaryMessage)
        {
            scaryMessage = processMessage(scaryMessage);
            DialogResult result = default;
            await Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => result = MessageBox.Show(
                scaryMessage, operationName, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2, default)));

            return result == DialogResult.Yes;
        }

        public static void Warn(string operationName, string scaryMessage)
        {
            scaryMessage = processMessage(scaryMessage);
            var result = MessageBox.Show(
                scaryMessage, operationName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, default);
        }

        public static void ReportSuccess(string operationName, string message)
        {
            message = processMessage(message);
            MessageBox.Show(
                message, operationName, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, default);
        }

        public static void ReportError(string operationName, string scaryMessage)
        {
            scaryMessage = processMessage(scaryMessage);
            MessageBox.Show(
                scaryMessage, operationName, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, default);
        }

        static string processMessage(string message)
        {
            return message.Replace("-NL", Global.NL);
        }
    }
}
