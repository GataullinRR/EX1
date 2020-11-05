using System.Threading.Tasks;
using System.IO;
using Utilities.Extensions;
using Utilities.Types;
using System.Runtime.InteropServices;
using System.Linq;

namespace RUSTelemetryStreamSenderWidget
{
    class RegDataReader : Disposable
    {
        const int DATA_OFFSET = 512;
        static readonly int ENTRY_LENGTH = typeof(RegData).StructLayoutAttribute.Size;

        readonly Stream _serialized;

        public Task<RegData> this[int index]
        {
            get
            {
                return Task.Run(() =>
                {
                    _serialized.Position = index * ENTRY_LENGTH + DATA_OFFSET;
                    var data = _serialized.Read(ENTRY_LENGTH);
                   
                    return RegData.FromBytes(data, 0);
                });
            }
        }

        public int Count { get; }

        public RegDataReader(Stream serializedData)
        {
            Count = (int)((serializedData.Length - DATA_OFFSET) / ENTRY_LENGTH);
            _serialized = serializedData;
        }

        protected override void disposeManagedState()
        {
            _serialized.Dispose();
        }
    }
}
