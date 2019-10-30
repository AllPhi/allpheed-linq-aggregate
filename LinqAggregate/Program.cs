using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq; //no LINQ without this

namespace LinqAggregate
{
    class Program
    {
        class RangedValue
        {
            public DateTime Start { get; }
            public DateTime End { get; }
            public double Value { get; }

            public RangedValue(DateTime start, DateTime end, double value)
            {
                Start = start;
                End = end;
                Value = value;
            }
        }

        static void Main(string[] args)
        {
            var filePath = @"Aggregate.txt";
            var result = new List<RangedValue>();
            CreateFile(filePath);

            if (File.Exists(filePath))
            {
                var lines = File.ReadLines(filePath);
                Stopwatch sw = new Stopwatch();

                #region _- No LINQ -_

                sw.Start();
                var arrLines = lines.ToArray();
                Tuple<DateTime, double> previousLineValues = null;
                for (int i = 0; i < arrLines.Length; i++)
                {
                    var fields = arrLines[i].Split(',');
                    var dateTime = DateTime.ParseExact(fields[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                    var value = double.Parse(fields[1], CultureInfo.InvariantCulture);
                    var currentLineValues = new Tuple<DateTime, double>(dateTime, value);
                    if (previousLineValues == null)
                        previousLineValues = currentLineValues;
                    if (currentLineValues.Item1 > previousLineValues.Item1)
                    {
                        result.Add(new RangedValue(previousLineValues.Item1, currentLineValues.Item1, previousLineValues.Item2));
                        previousLineValues = currentLineValues;
                    }
                    if (i == arrLines.Length - 1)
                        result.Add(new RangedValue(currentLineValues.Item1, currentLineValues.Item1.AddDays(1), currentLineValues.Item2));
                }

                sw.Stop();
                Console.WriteLine($"Not using LINQ: {sw.ElapsedMilliseconds} ms");

                #endregion _- No LINQ -_

                #region _- LINQ -_
                
                result = new List<RangedValue>();
                sw.Reset();
                sw.Start();
                lines.ToArray().Aggregate((curr, next) =>
                {
                    var currValues = curr.Split(',');
                    var nextValues = next.Split(',');

                    var start = DateTime.ParseExact(currValues[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                    var end = string.IsNullOrEmpty(nextValues[0]) ? start.AddDays(1) : DateTime.ParseExact(nextValues[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                    var value = double.Parse(currValues[1], CultureInfo.InvariantCulture);

                    result.Add(new RangedValue(start, end, value));
                    return next;
                });
                sw.Stop();
                Console.WriteLine($"Using LINQ: {sw.ElapsedMilliseconds} ms");

                #endregion _- LINQ -_

                #region _- 2-stepped -_

                result = new List<RangedValue>();
                var temp = new List<Tuple<DateTime, double>>();
                sw.Reset();
                sw.Start();
                foreach (var line in lines)
                {
                    var fields = line.Split(',');
                    var dateTime = DateTime.ParseExact(fields[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                    var value = double.Parse(fields[1], CultureInfo.InvariantCulture);
                    temp.Add(new Tuple<DateTime, double>(dateTime, value));

                }
                temp.Aggregate((curr, next) =>
                {
                    result.Add(new RangedValue(curr.Item1, next.Item1, curr.Item2));
                    return next;
                });
                sw.Stop();
                Console.WriteLine($"Using 2-step LINQ: {sw.ElapsedMilliseconds} ms");

                #endregion _- 2-stepped -_

                #region _- All together now -_
   
                result = new List<RangedValue>();

                sw.Reset();
                sw.Start();
                ParseLines(lines).Aggregate((curr, next) =>
                {
                    result.Add(new RangedValue(curr.Item1, next.Item1, curr.Item2));
                    return next;
                });
                sw.Stop();
                Console.WriteLine($"Using LINQ + yield: {sw.ElapsedMilliseconds} ms");

                #endregion _- All together now -_

            }
            Console.ReadLine();
        }

        static void CreateFile(string filePath)
        {
            using (var file = new StreamWriter(filePath))
            {
                for (int i = 0; i < 2000000; i++)
                {
                    var date = DateTime.Now.Date.AddDays(i);
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        continue; //this is not a weekend job
                    file.WriteLine("{0:yyyy-MM-dd},{1}", date, i * 0.02);
                }
            }
        }

        static IEnumerable<Tuple<DateTime, double>> ParseLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var fields = line.Split(',');
                var dateTime = DateTime.ParseExact(fields[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                var value = double.Parse(fields[1], CultureInfo.InvariantCulture);
                yield return new Tuple<DateTime, double>(dateTime, value);
            }
        }

    
    }
}
