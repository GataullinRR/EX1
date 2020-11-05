using DeviceBase.Devices;
using DeviceBase.IOModels;
using DeviceBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calibrators
{
    public delegate void DataRowAquiredDelegate(IEnumerable<ViewDataEntity> data);

    /// <summary>
    /// Allows calibrators to show recalculated points from DATA packet
    /// </summary>
    public interface IDataProvider
    {
        event DataRowAquiredDelegate DataRowAquired;
    }
}
