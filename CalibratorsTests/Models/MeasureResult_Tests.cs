using NUnit.Framework;
using Calibrators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.IO;
using static Calibrators.Models.InclinometrTemperatureCalibrator;
using static System.Net.Mime.MediaTypeNames;

namespace Calibrators.Models.InclinometrTemperatureCalibratorTests
{
    [TestFixture()]
    public class MeasureResult_Tests
    {
        [Test()]
        public void SerializationConsistency()
        {
            var expected = new MeasureResult(Modes.INC0_AZI0_GTF0, DateTime.Now,
                new string[] { "N", "AZI", "INC" },
                new Enumerable<double[]>()
                {
                    new double[] { 1, 200.3, 100.8 },
                    new double[] { 2, 0, 1e100 },
                    new double[] { 3, 999, -1e100 },
                });

            var serialized = expected.SerializeAsync().GetAwaiter().GetResult();
            var filePath = Path.Combine(Path.GetTempPath(), serialized.FileName);
            using (var file = File.Create(filePath))
            {
                file.Write(serialized.Content);
            }
            var actual = MeasureResult.DeserializeAsync(serialized.FileName, File.ReadAllBytes(filePath)).GetAwaiter().GetResult();

            Assert.AreEqual(expected, actual);
        }
    }
}