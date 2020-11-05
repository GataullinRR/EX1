using System;

namespace Calibrators.Models
{
    public enum TestType
    {
        AXIS,
        AXIS_SHOCK
    }

    public enum AxisTestMode
    {
        XZ,
        Y
    }

    public enum AxisShockTestMode
    {
        X,
        Y,
        Z
    }

    enum ShockSensorMnemonicType
    {
        ADXU,
        ADYU,
        ADZU,
    }

    public class ShockTestMode
    {
        readonly object _mode;
        readonly int _numOfPoints;
        readonly double _pulseDuration;

        public TestType TestType { get; }
        public AxisTestMode AxisTestMode => TestType == TestType.AXIS
            ? (AxisTestMode)_mode
            : throw new InvalidOperationException();
        public AxisShockTestMode AxisShockTestMode => TestType == TestType.AXIS_SHOCK
            ? (AxisShockTestMode)_mode
            : throw new InvalidOperationException();
        public int NumOfPoints => TestType == TestType.AXIS
            ? _numOfPoints
            : throw new InvalidOperationException();
        public double PulseDuration => TestType == TestType.AXIS_SHOCK
            ? _pulseDuration
            : throw new InvalidOperationException();

        public ShockTestMode(AxisTestMode mode, int numOfPoints)
        {
            _mode = mode;
            _numOfPoints = numOfPoints;
            TestType = TestType.AXIS;
        }
        public ShockTestMode(AxisShockTestMode mode, double pulseDuration)
        {
            _mode = mode;
            _pulseDuration = pulseDuration;
            TestType = TestType.AXIS_SHOCK;
        }
    }
}
