using MVVMUtilities.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Utilities;
using Utilities.Interfaces;
using Utilities.Types;
using Utilities.Extensions;
using YandexDiskPublicAPI;
using WPFUtilities.Types;
using System.Linq;
using System.Security.Cryptography;
using UtilitiesStandard;
using Logging;
using System.Threading.Tasks;

namespace UpdateManager.ViewModels
{
    class MainVM
    {
        public EnhancedObservableCollection<VersionInfoVM> Verisons { get; } = new EnhancedObservableCollection<VersionInfoVM>();
        public ActionCommand DownloadAndInstall { get; }
        public ActionCommand Cancel { get; }

        CancellationTokenSource _cts = new CancellationTokenSource();

        public MainVM()
        {
            populateVersionsAsync();

            async void populateVersionsAsync()
            {
                const string VERSION_INFO_FILE_NAME = "Info.V1.txt";
                const string SIGNATURES_FILE_NAME = "Signatures.V1.txt";
                const string APPLICATION_FILE_NAME = "App.V1.zip";
                const double MAX_VERSION_INFO_FILE_SIZE = 0.1 * 1024 * 1024;
                const double MAX_SIGNATURES_FILE_SIZE = 0.1 * 1024 * 1024;

                var updatesFolder = await YandexDisk.GetRootAsync("https://yadi.sk/d/SO6cu-S-NQB4_A");
                foreach (var updateDirectoryTask in updatesFolder.EnumerateDirectoriesAsync(CancellationToken.None))
                {
                    var updateDirectory = await updateDirectoryTask;
                    var versionInfoFile = await updateDirectory.GetFileAsync(new UPath(new PathFormat("\\"), VERSION_INFO_FILE_NAME));
                    var signaturesFile = await updateDirectory.GetFileAsync(new UPath(new PathFormat("\\"), SIGNATURES_FILE_NAME));

                    if (signaturesFile.Size > MAX_SIGNATURES_FILE_SIZE)
                    {
                        break;
                    }
                    else if (versionInfoFile.Size > MAX_VERSION_INFO_FILE_SIZE)
                    {
                        break;
                    }

                    var downloadDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    var downloadDir = new DiskDirectory(downloadDirPath, IOAccess.FULL);
                    Dictionary<string, byte[]> signatures;
                    using (var signaturesFileStream = await versionInfoFile.OpenAsync(FileOpenMode.ONLY_OPEN, _cts.Token))
                    {
                        var signaturesFileData = signaturesFileStream.ReadExact(signaturesFile.Size);
                        signatures = Encoding.UTF8.GetString(signaturesFileData)
                            .Split(Global.NL)
                            .Select(kvp => kvp.Split(";"))
                            .Where(kvp => kvp.Length == 2)
                            .ToDictionary(kvp => kvp[0], kvp => kvp[1].FromBase64());
                    }
                    if (!signatures.Keys.ContainsAll(VERSION_INFO_FILE_NAME, SIGNATURES_FILE_NAME, APPLICATION_FILE_NAME))
                    {
                        break;
                    }

                    string[] changes;
                    using (var versionInfoFileStream = await versionInfoFile.OpenAsync(FileOpenMode.ONLY_OPEN, _cts.Token))
                    {
                        var versionInfoFileData = versionInfoFileStream.ReadExact(versionInfoFile.Size);
                        var isMatch = SecurityUtils.VerifySignatureHash(
                            versionInfoFileData, 
                            signatures[VERSION_INFO_FILE_NAME], 
                            Properties.Resources.PublicRSA4096Key);
                        if (!isMatch)
                        {
                            break;
                        }
                        changes = Encoding.UTF8.GetString(versionInfoFileData)
                            .Split(Global.NL)
                            .ToArray();
                    }

                    Verisons.Add(new VersionInfoVM(updateDirectory.Name, changes, signatures));
                }
            }
        }

    }
}
