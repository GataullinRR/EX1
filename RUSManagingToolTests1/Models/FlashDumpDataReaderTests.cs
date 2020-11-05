using DeviceBase.Models;
using NUnit.Framework;
using RUSManagingTool.Models;
using RUSManagingTool.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.Threading;

namespace RUSManagingTool.Models.Tests
{
    [TestFixture()]
    public class FlashDumpDataReaderTests
    {
        //[Test()]
        //public async Task ReadRowsSafeAsyncTest()
        //{
        //    FlashDumpRowsReader.StreamAsyncFactoryDelegate streamFactory;
        //    var path = Path.GetTempFileName();
        //    {
        //        File.WriteAllBytes(path, RUSManagingToolTests.Properties.Resources.V1_ID22_100b_44Mb_Records);
        //        streamFactory = bs => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bs)
        //            .To<Stream>()
        //            .AsCompletedTask();
        //    }
        //    var dump = await FlashDump.OpenAsync(path, CancellationToken.None);
        //    var dataParser = new DataPacketParser(dump.DataRowDescriptors);
        //    var reader = await FlashDumpRowsReader.CreateReaderAsync(streamFactory, dataParser, CancellationToken.None);
        //    for (int i = 0; i < reader.RowsCount; i+=1000)
        //    {
        //        await reader.ReadRowsSafeAsync(i, 1000, CancellationToken.None);
        //    }
        //}

        //[Test()]
        //public void CreateReaderAsyncTest()
        //{
        //    Assert.Fail();
        //}
    }
}