using DeviceBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataViewWidget
{
    class InitializeCommand : ChartCommandBase
    {
        public InitializeCommand(IList<ICurveInfo> curveInfos, IDecimatedRowsReader rows) : base(ChartCommand.INITIALIZE)
        {
            CurveInfos = curveInfos;
            Rows = rows;
        }

        public IList<ICurveInfo> CurveInfos { get; }
        public IDecimatedRowsReader Rows { get; }

        public override string ToString()
        {
            return base.ToString() + $" (Кривых: {CurveInfos.Count}, строк: {Rows.Source.RowsCount})";
        }
    }
}
