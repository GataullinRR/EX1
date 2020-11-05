using Common;
using DeviceBase.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities.Extensions;
using Vectors;

namespace DeviceBase.Models
{
    public class SectionedDataPacketParser : IDataPacketParser
    {
        public class Section
        {
            public byte DeviceId { get; }
            public IDataPacketParser Parser { get; }

            public Section(RUSDeviceId deviceId, IDataPacketParser parser)
            {
                DeviceId = (byte)deviceId;
                Parser = parser ?? throw new ArgumentNullException(nameof(parser));
            }
        }

        readonly int[] _offsets;
        readonly Section[] _sections;
        static readonly byte[] DEVICE_DATA_START_MARKER = new byte[] { 0xAB, 0xCD, 0xEF };
        static readonly int HEADER_LENGTH = DEVICE_DATA_START_MARKER.Length + 1; // + 1 byte Id
        const double DEFAULT_VALUE = double.NaN;

        public ICurveInfo[] Curves { get; set; }
        public IntInterval RowLength { get; }

        public SectionedDataPacketParser(IEnumerable<Section> sections)
        {
            _sections = sections.ToArray();
            RowLength = new IntInterval(_sections.Min(s => s.Parser.RowLength.From) + HEADER_LENGTH, _sections.Sum(s => s.Parser.RowLength.To + HEADER_LENGTH));
            Curves = _sections.Select(s => s.Parser.Curves).Flatten().ToArray();
            _offsets = getOffsets().ToArray();

            IEnumerable<int> getOffsets()
            {
                var prev = 0;
                yield return 0;
                foreach (var section in _sections.SkipFromEnd(1))
                {
                    var value = section.Parser.Curves.Length + prev;
                    prev = value;
                    yield return value;
                }
            }
        }

#warning optimization required
        public IPointsRow ParseRow(IList<byte> data)
        {
            var points = new double[_sections.Sum(s => s.Parser.Curves.Length)];
            points.SetAll(DEFAULT_VALUE);

            var markers = new int[_sections.Length];
            markers.SetAll(-1);
            for (int i = 0; i < data.Count - DEVICE_DATA_START_MARKER.Length; i++)
            {
                var found = true;
                for (int k = 0; k < DEVICE_DATA_START_MARKER.Length; k++)
                {
                    if (data[i + k] != DEVICE_DATA_START_MARKER[k])
                    {
                        found = false;
                    }
                }

                if (found)
                {
                    var deviceId = data[i + DEVICE_DATA_START_MARKER.Length];
                    var deviceIndex = _sections.Find(s => s.DeviceId == deviceId).Index;
                    if (deviceIndex >= 0)
                    {
                        markers[deviceIndex] = i;
                    }
                    i += HEADER_LENGTH - 1;
                }
            }

            for (int i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                if (marker >= 0) // Found
                {
                    var start = marker + HEADER_LENGTH;
                    var end = i == markers.Length - 1
                        ? data.Count
                        : markers.Skip(i).Where(m => m > start).EmptyToNull()?.Min() ?? data.Count; // Take next closest marker as the end of current device's data section or if none exists take an end of array
                    var dataToParse = data.SubArray(start, end - start);
                    var subRow = _sections[i].Parser.ParseRow(dataToParse);
                    for (int k = 0; k < subRow.Points.Length; k++)
                    {
                        points[_offsets[i] + k] = subRow.Points[k];
                    }
                    //Buffer.BlockCopy(subRow.Points, 0, points, _offsets[i], subRow.Points.Length);
                }
            }

            return new PointsRow(points);
        }
    }
}