using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extensions;
using Utilities.Types;

namespace VirtualDevice.Devices
{
    class FTDIBox
    {
        const int FLASH_DUMP_SIZE = 30 * 1024 * 1024;
        static readonly byte[] ROW_START_MARKER = new byte[] { 0x01, 0x00, 0x0E, 0x05, 0x05, 0x57, 0xD7, 0xFD };
        bool _inHighSpeedMode;

        [FTDIBoxCommandHandler(new byte[] { 0, 16 }, 2)]
        public IEnumerable<byte> HS_SetLowSpeedMode(byte[] requestBody)
        {
            if (_inHighSpeedMode)
            {
                _inHighSpeedMode = false;

                return new byte[] { 0, 16 };
            }
            else
            {
                return new byte[] { };
            }
        }

        [FTDIBoxCommandHandler(new byte[] { 92, 12 }, 2)]
        public IEnumerable<byte> LS_SetHighSpeedMode(byte[] requestBody)
        {
            if (!_inHighSpeedMode)
            {
                _inHighSpeedMode = true;

                return new byte[] { 92, 12 };
            }
            else
            {
                return new byte[] { };
            }
        }

        [FTDIBoxCommandHandler(new byte[] { 0, 20 }, 2)]
        public IEnumerable<byte> HS_ActivateChannel4(byte[] requestBody)
        {
            if (_inHighSpeedMode)
            {
                return new byte[] { 0, 20 };
            }
            else
            {
                return new byte[] { };
            }
        }
        [FTDIBoxCommandHandler(new byte[] { 92, 10 }, 2)]
        public IEnumerable<byte> LS_ActivateChannel4(byte[] requestBody)
        {
            if (!_inHighSpeedMode)
            {
                return new byte[] { 92, 10 };
            }
            else
            {
                return new byte[] { };
            }
        }

        [FTDIBoxCommandHandler(new byte[] { 9, 128, 170, 206, 0, 0, 0, 0, 0, 2 }, 10)]
        public IEnumerable<byte> HS_GetFlashLength(byte[] requestBody)
        {
            if (_inHighSpeedMode)
            {
                return new Enumerable<byte>() 
                { 
                    255, 4, 
                    (FLASH_DUMP_SIZE / 2048).SerializeAsNumber(false).Skip(1), 
                    3 
                };
            }
            else
            {
                return new byte[] { };
            }
        }

        [FTDIBoxCommandHandler(new byte[] { 9, 128, 170, 206, 0, 0, 0, 0, 0, 3 }, 10)]
        public IEnumerable<byte> HS_ReadAllFlash(byte[] requestBody)
        {
            if (_inHighSpeedMode)
            {
                //return File.Open(Path.Combine(@"C:\Users\Radmir\Documents\TEMP", "тест 8 - Copy.bin"), FileMode.Open).ReadToEnd();
                return (FLASH_DUMP_SIZE / sizeof(ushort)).Range().Select(v => (ushort)v).ToArray()
                    .AsParallel()
                    .AsOrdered()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .WithDegreeOfParallelism(Environment.ProcessorCount)
                    .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                    .Select(v => v.SerializeAsNumber(true))
                    .Flatten()
                    .GroupBy(100)
                    .Select(dp => ROW_START_MARKER.Concat(((byte)0).Repeat(6)).Concat(new byte[] { 0xAB, 0xCD, 0xEF, Global.Random.NextByte(0, 25) }).Concat(dp))
                    .Concat(new byte[] { 255, 4, 0, 0, 0, 0 })
                    .Flatten()
                    .ToArray();
            }
            else
            {
                return new byte[] { };
            }
        }

        readonly SalachovDeviceSet _devices = new SalachovDeviceSet();
        [FTDIBoxCommandHandler(new byte[] { 0xFF, 0x00 })]
        public IEnumerable<byte> LS_DeviceRequest(byte[] requestBody)
        {
            if (!_inHighSpeedMode)
            {
                _devices.Write(requestBody);

                return _devices.PopInputBuffer();
            }
            else
            {
                return new byte[] { };
            }
        }
    }
}
