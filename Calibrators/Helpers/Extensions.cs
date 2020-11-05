using Calibrators.Models;
using DeviceBase.Helpers;
using DeviceBase.IOModels;
using DeviceBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Extensions;
using Vectors;

namespace Calibrators
{
    public static class Extensions
    {
        public static IEnumerable<ViewDataEntity> GetViewEntities(this IEnumerable<IDataEntity> entities)
        {
            foreach (var entity in entities)
            {
                var point = (entity as IPointDataEntity)?.Point ?? (double?)null;
                if (point.HasValue)
                {
                    yield return new ViewDataEntity(entity.Descriptor.Name, point.Value, entity.Descriptor.ValueFormat.IsInteger());
                }
                else
                {
                    yield return new ViewDataEntity(entity.Descriptor.Name, entity.Descriptor.SerializeToString(entity.Value));
                }
            }
        }

#warning move to attribute
        internal static string ToAnglesString(this InclinometrTemperatureCalibrator.IncTCalAngle angle)
        {
            return angle.ToString(
                (InclinometrTemperatureCalibrator.IncTCalAngle.INC0_AZI0_GTF0, "INC=0 AZI=0 GTF=0"),
                (InclinometrTemperatureCalibrator.IncTCalAngle.INC45_AZI45_GTF45, "INC=45 AZI=45 GTF=45"),
                (InclinometrTemperatureCalibrator.IncTCalAngle.INC135_AZI315_GTF225, "INC=135 AZI=315 GTF=225"),
                (InclinometrTemperatureCalibrator.IncTCalAngle.INC60_AZI60_GTF60, "INC=60 AZI=60 GTF=60"));
        }

#warning move to attribute
        internal static V3 GetAngles(this InclinometrTemperatureCalibrator.IncTCalAngle angle)
        {
            return angle.Select(
                ((a => a.Equals(InclinometrTemperatureCalibrator.IncTCalAngle.INC0_AZI0_GTF0)), new V3(0, 0, 0)),
                ((a => a.Equals(InclinometrTemperatureCalibrator.IncTCalAngle.INC45_AZI45_GTF45)), new V3(45, 45, 45)),
                ((a => a.Equals(InclinometrTemperatureCalibrator.IncTCalAngle.INC135_AZI315_GTF225)), new V3(135, 315, 225)),
                ((a => a.Equals(InclinometrTemperatureCalibrator.IncTCalAngle.INC60_AZI60_GTF60)), new V3(60, 60, 60))).Single();
        }
    }
}
