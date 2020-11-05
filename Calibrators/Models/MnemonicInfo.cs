using System;

namespace Calibrators.Models
{
    class MnemonicInfo<TType> where TType : struct
    {
        public MnemonicInfo(TType type, string deviceMnemonic, Func<dynamic, bool> isInvalidValue, Func<dynamic, double> decode, Func<double, double> coerce)
        {
            Type = type;
            DeviceMnemonic = deviceMnemonic;
            IsInvalidValue = isInvalidValue;
            Decode = decode;
            Coerce = coerce;
        }

        public TType Type { get; }
        public string DeviceMnemonic { get; }
        public Func<dynamic, bool> IsInvalidValue { get; }
        public Func<dynamic, double> Decode { get; }
        public Func<double, double> Coerce { get; }
        
        public void ThrowIfInvalid(dynamic mnemonicValue)
        {
            if (IsInvalidValue(mnemonicValue))
            {
                throw new Exception($"Мнемоника {DeviceMnemonic} имела недопустимое значение Value:\"{mnemonicValue}\"");
            }
        }

        public double GetValue(dynamic rawValue)
        {
            return Coerce(Decode(rawValue));
        }
    }
}
