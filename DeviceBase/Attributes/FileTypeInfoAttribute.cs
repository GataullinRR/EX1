using DeviceBase.Devices;
using DeviceBase.Helpers;
using System;
using System.Linq;
using Utilities;
using Utilities.Extensions;

namespace DeviceBase.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class FileTypeInfoAttribute : Attribute
    {
        /// <summary>
        /// Name in packet
        /// </summary>
        internal string FileTypePointerName { get; }
        public string FileName { get; }
        public Command RequestAddress { get; }

        public FileTypeInfoAttribute(string fileTypePointerName, string name, FileType fileType)
        {
            FileTypePointerName = fileTypePointerName;
            FileName = name;

            var info = EnumUtils
                .GetValues<Command>()
                .Select(v => (RequestAddress: v, Info: v.GetInfo()))
                .Where(i => i.Info != null)
                .Where(i => i.Info.FileType == fileType)
                .Single();
            RequestAddress = info.RequestAddress;
        }
    }
}
