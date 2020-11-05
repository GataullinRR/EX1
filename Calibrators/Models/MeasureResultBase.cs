using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calibrators.Models
{
    abstract class MeasureResultBase
    {
        protected const string CSV_SEPARATOR = ";";
        protected const string CSV_ENCODING = "windows-1251";

        public abstract Task<(string FileName, IEnumerable<byte> Content)> SerializeAsync();
    }
}