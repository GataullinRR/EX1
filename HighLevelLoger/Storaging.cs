using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace Common
{
    public static class Storaging
    {
        static readonly string TEMP_FILES_DIR = Path.Combine(Environment.CurrentDirectory, "TempFiles");
        static int _filesCounter = -1;

        static Storaging()
        {
            if (!Directory.Exists(TEMP_FILES_DIR))
            {
                Directory.CreateDirectory(TEMP_FILES_DIR);
            }
            foreach (var path in Directory.GetFiles(TEMP_FILES_DIR))
            {
                CommonUtils.Try(() => File.Delete(path));
            }
            _filesCounter += Directory.GetFiles(TEMP_FILES_DIR).Length;
        }

        public static FileStream GetTempFileStream()
        {
            return new FileStream(GetTempFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        }

        public static string GetTempFilePath()
        {
            var fileIndex = Interlocked.Increment(ref _filesCounter);
            return Path.Combine(TEMP_FILES_DIR, fileIndex.ToString() + ".tmp");
        }
    }
}
