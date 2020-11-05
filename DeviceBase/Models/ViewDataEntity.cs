using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Extensions;

namespace DeviceBase.Models
{
    public class ViewDataEntity
    {
        public string CurveName { get; }
        public string StringValue { get; }
        public double? PointValue { get; }
        public bool IsInteger { get; }

        public ViewDataEntity(string curve, double pointValue, bool isInteger)
        {
            CurveName = curve;
            PointValue = pointValue;
            IsInteger = isInteger;
        }
        public ViewDataEntity(string curve, string stringValue)
        {
            CurveName = curve;
            StringValue = stringValue ?? throw new ArgumentNullException(nameof(stringValue));
        }

        public string GetAsString(int maxNumOfDigits)
        {
            return StringValue ?? 
                   (IsInteger 
                       ? PointValue.Value.ToStringInvariant("F0") 
                       : PointValue.Value.ToStringInvariant("F" + maxNumOfDigits));
        }

        public bool IsSameAs(ViewDataEntity entity)
        {
            //return CurveName == entity.CurveName
            //    && PointValue.HasValue == entity.PointValue.HasValue
            //    && IsInteger == entity.IsInteger;
            return CurveName == entity.CurveName;
        }
    }
}
