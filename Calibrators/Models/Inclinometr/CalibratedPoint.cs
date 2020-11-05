using Vectors;

namespace Calibrators.ViewModels.Inclinometr
{
    internal class CalibratedPoint
    {
        public CalibratedPoint(
            V3 angle, 
            V3 anglesBeforeCal, 
            V3 accelerometrBeforeCal,
            V3 magnitometrBeforeCal, 
            V3 anglesAfterCal, 
            V3 accelerometrAfterCal, 
            V3 magnitometrAfterCal, 
            V3 accelerometrTemperatures, 
            V3 magnetometrTemperatures, 
            double avgTemperature)
        {
            Angle = angle;
            AnglesBeforeCal = anglesBeforeCal;
            AccelerometrBeforeCal = accelerometrBeforeCal;
            MagnitometrBeforeCal = magnitometrBeforeCal;
            AnglesAfterCal = anglesAfterCal;
            AccelerometrAfterCal = accelerometrAfterCal;
            MagnitometrAfterCal = magnitometrAfterCal;
            AccelerometrTemperatures = accelerometrTemperatures;
            MagnetometrTemperatures = magnetometrTemperatures;
            AvgTemperature = avgTemperature;
        }

        public V3 Angle { get; }
        public V3 AnglesBeforeCal { get; }
        public V3 AccelerometrBeforeCal { get; }
        public V3 MagnitometrBeforeCal { get; }
        public V3 AnglesAfterCal { get; }
        public V3 AccelerometrAfterCal { get; }
        public V3 MagnitometrAfterCal { get; }
        public V3 AccelerometrTemperatures { get; }
        public V3 MagnetometrTemperatures { get; }
        public double AvgTemperature { get; }
    }
}
