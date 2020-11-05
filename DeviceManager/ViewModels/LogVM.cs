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
using Common;
using WPFUtilities.Types;

namespace RUSManagingTool.ViewModels
{
    public class LogVM
    {
        public DisplaceCollection<LogEntry> Entries { get; } = new DisplaceCollection<LogEntry>(100);

        public ActionCommand Clear { get; }

        public LogVM()
        {
            HighLevelLog.EntryAdded += HighLevelLog_EntryAdded;
            Clear = new ActionCommand((Action)Entries.Clear);

            void HighLevelLog_EntryAdded(object sender, ValueEventArgs<LogEntry> e)
            {
                Entries.Add(e);
            }
        }
    }
}
