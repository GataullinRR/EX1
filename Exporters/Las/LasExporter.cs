using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Types;
using Utilities.Extensions;
using System.IO;
using Vectors;
using System.Threading;

namespace Exporters.Las
{
    class LasExporter
    {
        const double NULL = -999.25;
        const string POINT_FORMAT = "F2";
        const string HEADER_START = @"~VERSION INFORMATION
  VERS.      2.0                  : CWLS LOG ASCII STANDARD -VERSION 2.0
  WRAP.      NO                   : ONE LINE PER DEPTH STEP       
~WELL INFORMATION
# MNEM.UNIT  DATA                 : DESCRIPTION                   
# ----.----- -------------------- : ------------------------------
  STRT.M     1                    : START DEPTH                   
  STOP.M     {0,-21}: STOP DEPTH                    
  STEP.M     1                    : STEP                          
  NULL.      -999.25              : NULL VALUE                    
  COMP.                           : COMPANY                       
  WELL.                           : WELL                          
  FLD .                           : FIELD                         
  LOC .                           : LOCATION                      
  PROV.                           : PROVINCE                      
  SRVC.                           : SERVICE COMPANY               
  DATE.                           : LOG DATE                      
  UWI .                           : UNIQUE WELL ID                
~CURVE INFORMATION
# MNEM.UNIT  DATA                 : DESCRIPTION                   
# ----.----- -------------------- : ------------------------------
  DEPT.M                          : DEPTH                         ";

        const string CURVE_NAME_FORMAT = @"  {0,-32}:                               ";
        const string END_OF_HEADER = @"~PARAMETER INFORMATION
# MNEM.UNIT  DATA                 : DESCRIPTION                   
# ----.----- -------------------- : ------------------------------
~OTHER

~A";
        
        public async Task SaveToAsync(Stream stream, IList<string> curveNames, IEnumerable<double[]> curveRows, int rowsCount, AsyncOperationInfo asyncOperationInfo)
        {
            await ThreadingUtils.ContinueAtDedicatedThread(asyncOperationInfo);

            //curveRows = curveRows.MakeCached(); 
            var rowFormat = getRowFormat();
            using (var sw = new StreamWriter(stream))
            {
                sw.WriteLine(HEADER_START.Format(rowsCount));
                foreach (var curveName in curveNames)
                {
                    sw.WriteLine(CURVE_NAME_FORMAT.Format(curveName + "."));
                }
                sw.WriteLine(END_OF_HEADER);

                var stringRows = curveRows
                    .AsParallel()
                    .AsOrdered()
                    .WithDegreeOfParallelism(Environment.ProcessorCount)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .WithCancellation(asyncOperationInfo)
                    .Select((row, i) => string.Format(rowFormat, 
                        (i + 1D).ToSequence()
                        .Concat(row)
                        .Select(v => (object)v.Exchange(double.NaN, NULL))
                        .ToArray()))
                    .Select(row => row.Replace(",", "."));
                foreach (var row in stringRows)
                {
                    asyncOperationInfo.CancellationToken.ThrowIfCancellationRequested();

                    sw.WriteLine(row);

                    asyncOperationInfo.Progress.AddProgress(1D / (rowsCount - 1), 0.3);
                }
            }

            string getRowFormat()
            {
                var valueRanges = new Interval[(curveRows.FirstOrDefault()?.Length ?? 0) + 1];
                var rowI = 0;
                foreach (var row in curveRows.SkipNulls())
                {
                    asyncOperationInfo.CancellationToken.ThrowIfCancellationRequested();
                    asyncOperationInfo.Progress.AddProgress(1D / (rowsCount - 1), 0.7);

                    foreach (var pI in row.Length.Range())
                    {
                        var point = row[pI].Exchange(double.NaN, NULL);
                        valueRanges[pI + 1] = valueRanges[pI + 1].ExpandToContain(point);
                    }
                    rowI++;
                }
                valueRanges[0] = new Interval(0, rowsCount);
                return valueRanges
                    .Select(r => Math.Max(r.From.ToStringInvariant(POINT_FORMAT).Length, r.To.ToStringInvariant(POINT_FORMAT).Length) + 1)
                    .Select((w, i) => i == 0 ? $"{{0,{w}}}" : $"{{{i},{w}:{POINT_FORMAT}}}")
                    .Aggregate("");
            }
        }
    }
}
