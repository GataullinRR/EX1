using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var data = File
                .ReadAllLines(@"C:\Users\Radmir\Desktop\Work\RUS\Test data\2019-09-30 15-16 InGK SENS raw 2.csv")
                .Select(line =>
                {
                    if (line.Contains("INC"))
                    {
                        return line;
                    }
                    else
                    {
                        var points = line.Split(';');
                        if (points.Length > 10)
                        {
                            points[3] = (-2 * double.Parse(points[3])).ToString();
                            //points[8] = (-double.Parse(points[8])).ToString();
                        }

                        return points.Aggregate("", (acc, val) => acc + val + ';');
                    }
                }).ToArray();
            var writer = new StreamWriter(IOUtils.CreateFile(Path.Combine(Environment.CurrentDirectory, @"2019-09-30 15-16 InGK SENS raw (3-ROT).txt")));
            foreach (var item in data)
            {
                writer.WriteLine(item.Replace(";;", ";"));
            }
            writer.Flush();
        }
    }
}
