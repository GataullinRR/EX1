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
using DeviceBase.Models;

namespace DeviceBase.Tests
{
    [TestFixture()]
    public class FileStringSerializer_Tests
    {
        [Test()]
        public void SeamlessSerialization_IncTCalFile_Test()
        {
            var serializer = new FileStringSerializer();
            var deserialized = serializer.Deserialize(
                DeviceBaseTests.Properties.Resources.InclinometrTCal_V7_0_0, 
                FileType.TEMPERATURE_CALIBRATION, 
                0b00000011);
            var serialized = serializer.Serialize(deserialized);

            Assert.AreEqual(DeviceBaseTests.Properties.Resources.InclinometrTCal_V7_0_0, serialized);
        }

        [Test()]
        public void SeamlessSerialization_EmptyIncTCalFile_Test()
        {
            var serializer = new FileStringSerializer();
            var deserialized = serializer.Deserialize(
                DeviceBaseTests.Properties.Resources.EmptyInclinometrTCal_V7_0_0,
                FileType.TEMPERATURE_CALIBRATION,
                0b00000011);
            var serialized = serializer.Serialize(deserialized);

            Assert.AreEqual(DeviceBaseTests.Properties.Resources.EmptyInclinometrTCal_V7_0_0, serialized);
        }
    }
}