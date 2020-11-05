using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vectors;

namespace DeviceBase.Models
{
    /// <summary>
    /// Must be thread safe
    /// </summary>
    public interface IDataPacketParser
    {
        ICurveInfo[] Curves { get; }
        IPointsRow ParseRow(IList<byte> data);
        /// <summary>
        /// [Minimal; Maximal]
        /// </summary>
        IntInterval RowLength { get; }
    }
}
