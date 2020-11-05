using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using UtilitiesStandard;

namespace UpdateCreator
{
    class Program
    {
        static readonly string _path;

        static void Main(string[] args)
        {

            var exeFilePath = Path.Combine(_path, "Debug", "RUS-MT.exe");
            var version = FileVersionInfo.GetVersionInfo(exeFilePath).FileVersion;

            var updateFolderRoot = Path.Combine(_path, version);
            var applicationFilePath = Path.Combine(updateFolderRoot, "App.V1.zip");
            ZipFile.CreateFromDirectory(Path.Combine(_path, "Debug"), applicationFilePath, CompressionLevel.Optimal, false);
            var appFileSignature = SecurityUtils.GetSignature(File.ReadAllBytes(applicationFilePath), Properties.Resources.PrivateRSA4096Key);
            

            var updateInfoFilePath = Path.Combine(updateFolderRoot, "App.V1.zip");
            File.Copy(updateInfoFilePath, Path.Combine(updateFolderRoot, version));
        }
    }
}
