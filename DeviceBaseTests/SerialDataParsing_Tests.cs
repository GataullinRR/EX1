using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using NUnit.Framework;
using System.IO;
using System.Threading;
using DeviceBase;

namespace DeviceBaseTests
{
    [TestFixture()]
    public class SerialDataParsing_Tests
    {
        //class ddd : ISerialPort
        //{
        //    public string PortName => "SOME PORT";

        //    public Stream IOStream => new MemoryStream();

        //    public SemaphoreSlim Locker => new SemaphoreSlim(1, 1);

        //    public byte[] ClearReadBuffer()
        //    {
        //        return new byte[0];
        //    }
        //}

        //[Test()]
        //public async Task SeamlessSerializationForDPEntityDescriptor()
        //{
        //    //var port = new ddd();
        //    //var dddd = new COMPortRUSConnectionInterface(port);
        //    //var requestTask = dddd.RequestAsync(Request.CreateReadRequest(0, 0, AnswerLength.UNKNOWN));
        //    //port.

        //}
    }
}
