using NUnit.Framework;
using DeviceBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using DeviceBase.IOModels;

namespace DeviceBaseTests
{
    [TestFixture()]
    public class EntityDescriptor_Tests
    {
        [Test()]
        public void SeamlessSerializationForDPEntityDescriptor()
        {
            var descriptor = new EntityDescriptor("1", 0, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.DATA_PACKET_ENTITIES_ARRAY);
            var data = new DataPacketEntityDescriptor[]
                {
                    new DataPacketEntityDescriptor("ENT1", 4, 0, 1, true),
                    new DataPacketEntityDescriptor("ENT2", 4, 4, 2, false),
                    new DataPacketEntityDescriptor("ENT3", 2, 8, 3, false),
                };

            var serialized = descriptor.Serialize(data);
            var deserialized = descriptor.Deserialize(serialized);

            var actual = (DataPacketEntityDescriptor[])deserialized;
            Assert.AreEqual(data, actual);
        }

        [Test()]
        public void SeamlessSerializationForASCIIString()
        {
            var descriptor = new EntityDescriptor("1", 0, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.ASCII_STRING);
            var rawData = new[] { 0, 0xFF, 'd', 'g' };
            var data = rawData.Select(v => (char)v).Aggregate();

            var serialized = descriptor.Serialize(data);
            var deserialized = descriptor.Deserialize(serialized);
            serialized = descriptor.Serialize(deserialized);

            var actual = (string)deserialized;
            Assert.AreEqual(rawData, serialized);
        }

        //[Test()]
        //public void SeamlessSerializationForCalibrationFileEntityDescriptor()
        //{
        //    var descriptor = new EntityDescriptor("Test", 0, EntityLength.TILL_THE_END_OF_A_PACKET, DataEntityFormat.CALIBRATION_PACKET_ENTITIES_ARRAY);
        //    var data = new[]
        //    {
        //        CalibrationFileEntityDescriptor.CreateLinearInterpolationTable("Hello1", DataTypes.FLOAT, new[] { (1F, 5.5F), (-9.9F, 0F) }),
        //        CalibrationFileEntityDescriptor.CreateLinearInterpolationTable("Hello2", DataTypes.FLOAT, new[] { (0.1F, -1e10F), (0F, 0F) }),
        //    };

        //    var serialized = descriptor.Serialize(data);
        //    var deserialized = descriptor.Deserialize(serialized);

        //    var actual = (CalibrationFileEntityDescriptor[])deserialized;
        //    Assert.AreEqual(data, actual);
        //}
    }
}