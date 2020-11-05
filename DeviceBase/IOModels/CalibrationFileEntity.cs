using DeviceBase.Attributes;
using DeviceBase.Helpers;
using ObjectsComparer;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using Utilities.Extensions;
using Vectors;

namespace DeviceBase.IOModels
{
    public enum ItemTypes : byte
    {
        CONSTANT = 0,
        RECORDING_POINT = 1,
        SPECTRUN = 2,
        STRING_PARAMETER = 3,
        CALIBRATION_TABLE = 4,
        LINEAR_INTERPOLATION_TABLE = 5,
        INCLINOMETR_ACCELEROMETR_CHANNEL_ANGULAR_CALIBRATION_COEFFICIENTS = 6,
        INCLINOMETR_MAGNITOMETR_CHANNEL_ANGULAR_CALIBRATION_COEFFICIENTS = 7,
        ARRAY = 8
    }

    public enum DataTypes : byte
    {
        [Size(4)]
        FLOAT = 13,
        [Size(2)]
        UINT16 = 8,
        [Size(2)]
        INT16 = 7,
        [Size(1)]
        INT8 = 6,
        /// <summary>
        /// No new line and zero symbols allowed 
        /// </summary>
        [Size(1)]
        STRING_CP1251 = 18
    }

    /// <summary>
    /// It is used as <see cref="IDataEntity.Value"/>
    /// </summary>
    public class CalibrationFileEntity
    {
        /// <summary>
        /// Parameter name in CP1251. No new line and zero symbols allowed
        /// </summary>
        public string Name { get; }
        public ItemTypes ItemType { get; }
        public DataTypes DataType { get; }
        /// <summary>
        /// In bytes
        /// </summary>
        public int DataLength { get; }
        /// <summary>
        /// Array of numeric values, or string
        /// </summary>
        public object Data { get; }

#warning add parameters check
        internal CalibrationFileEntity(string name, ItemTypes itemType, DataTypes dataType, object data)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ItemType = itemType;
            DataType = dataType;
            DataLength = calculateDataLength();
            Data = data ?? throw new ArgumentNullException(nameof(data));

            if (Name.Contains(Global.NL) || Name.Contains((char)0))
            {
                throw new ArgumentException();
            }

            int calculateDataLength()
            {
                switch (dataType)
                {
                    case DataTypes.FLOAT:
                    case DataTypes.UINT16:
                    case DataTypes.INT16:
                    case DataTypes.INT8:
                    case DataTypes.STRING_CP1251:
                        return ((dynamic)data).Length * dataType.GetSize();

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public static CalibrationFileEntity CreateConstantsTable<T>
            (string name, DataTypes dataType, IEnumerable<T> coefficients)
        {
            return new CalibrationFileEntity(
                name,
                ItemTypes.CONSTANT,
                dataType,
                coefficients.ToArray());
        }

        public static CalibrationFileEntity CreateLinearInterpolationTable<T>
            (string name, DataTypes dataType, IEnumerable<(T B, T K)> kbCoefficients)
        {
            var kbs = kbCoefficients.Select(kb => new[] { kb.B, kb.K }).Flatten().ToArray();

            return new CalibrationFileEntity(
                name, 
                ItemTypes.LINEAR_INTERPOLATION_TABLE, 
                dataType,  
                kbs);
        }

        public static CalibrationFileEntity CreateInclinometrAccelerometrChannelAngularCalibrationTable<T>
            (string name, DataTypes dataType, IEnumerable<T> coefficients)
        {
            var coeffs = coefficients.ToArray();
            if (coeffs.Length != 11)
            {
                throw new ArgumentException("Необходимо 11 коэффициентов");
            }

            return new CalibrationFileEntity(
                name,
                ItemTypes.INCLINOMETR_ACCELEROMETR_CHANNEL_ANGULAR_CALIBRATION_COEFFICIENTS,
                dataType,
                coeffs);
        }

        public static CalibrationFileEntity CreateInclinometrMagnitometrChannelAngularCalibrationTable<T>
            (string name, DataTypes dataType, IEnumerable<T> coefficients)
        {
            var coeffs = coefficients.ToArray();
            if (coeffs.Length != 4)
            {
                throw new ArgumentException("Необходимо 4 коэффициента");
            }

            return new CalibrationFileEntity(
                name,
                ItemTypes.INCLINOMETR_MAGNITOMETR_CHANNEL_ANGULAR_CALIBRATION_COEFFICIENTS,
                dataType,
                coeffs);
        }

        public static CalibrationFileEntity CreateArray<T>
            (string name, DataTypes dataType, IEnumerable<T> values)
        {
            return new CalibrationFileEntity(
                name,
                ItemTypes.ARRAY,
                dataType,
                values.ToArray());
        }
    }
}
