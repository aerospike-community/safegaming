using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.IO;
using HdrHistogram;
using Common;
using System.Runtime.InteropServices;

namespace PlayerCommon
{
    public struct PrefStat
    {
        /// <summary>
        /// The Sequence Id of when this was captured
        /// </summary>
        public long SequenceNbr;
        /// <summary>
        /// The elapsed time since the application was loaded
        /// </summary>
        [JsonConverter(typeof(TimespanConverter))]
        public TimeSpan AppElapsedTime;
        /// <summary>
        /// Timing of the measured event
        /// </summary>
        [JsonConverter(typeof(TimespanConverterMS))]
        public TimeSpan Timing;
        /// <summary>
        /// Event (e.g., Put, Get)
        /// </summary>
        public string Event;
        /// <summary>
        /// Taken in System (e.g., Aerospike/MongoDB)
        /// </summary>
        public string System;
        /// <summary>
        /// The name of the targeted Set, Table, Document, etc.
        /// </summary>
        public string Setname;
        /// <summary>
        /// Name of the calling function where the event occurred
        /// </summary>
        public string FuncName;
        /// <summary>
        /// Associated Primary Key, if there is one...
        /// </summary>
        public string PK;

    }

    public static class PrefStats
    {
        [Flags]
        public enum CaptureTypes
        {
            Disabled = 0x0000,
            CSV = 0x0001,
            JSON = Detail | 0x0010,
            HGRM = Histogram | 0x0100,
            Detail = 0x00010000,
            Histogram = 0x00100000
        }

        public static volatile bool EnableEvents = false;
        public static CaptureTypes CaptureType { get; set; } = CaptureTypes.Disabled; 
        public static Stopwatch RunningStopwatch { get; }  = Stopwatch.StartNew();
        private static long SequenceNbr = 0;
        public static ConcurrentQueue<PrefStat> ConcurrentCollection { get; } = new();
        public static HistogramBase HdrHistogram { get; private set;  }

        /// <summary>
        /// Format: Days.Hours:Minutes:Seconds:Milliseconds
        /// </summary>
        public const string TimeSpanFormatString = @"hh\:mm\:ss\.fffffff";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StopRecord(this Stopwatch stopWatch, string type, string systemName, string setName, string funcName, string pk)
        {
            stopWatch.Stop();
            if (EnableEvents)
            {
                var currentSeqNbr = Interlocked.Increment(ref SequenceNbr);

                if (CaptureType.HasFlag(CaptureTypes.Detail))
                {
                    ConcurrentCollection.Enqueue(new PrefStat
                    {
                        SequenceNbr = currentSeqNbr,
                        AppElapsedTime = RunningStopwatch.Elapsed,
                        Timing = stopWatch.Elapsed,
                        Event = type,
                        System = systemName,
                        Setname = setName,
                        FuncName = funcName,
                        PK = pk
                    });
                }

                if(CaptureType.HasFlag(CaptureTypes.Histogram))
                {
                    HdrHistogramRecordValue(stopWatch.ElapsedTicks);                    
                }

                if (setName != null
                        && Settings.Instance.WarnMaxMSLatencyDBExceeded > 0
                        && stopWatch.ElapsedMilliseconds > Settings.Instance.WarnMaxMSLatencyDBExceeded)
                    Logger.Instance.WarnFormat("{0}.{1} Run Exceeded Latency Threshold for Key {2}. Latency: {3}",
                                                setName,
                                                funcName,
                                                pk,
                                                stopWatch.ElapsedMilliseconds);
            }
        }

        private static long MaxHdrHistogramTickErrorValue = Settings.Instance.HGHighestTickValue;
        private static long MinHdrHistogramTickErrorValue = Settings.Instance.HGLowestTickValue;
        private static int HdrHistogramTickErrorCnt = 0;
        private static void HdrHistogramRecordValue(long elapsedTicks)
        {
            try
            {
                HdrHistogram.RecordValue(elapsedTicks);
            }
            catch (IndexOutOfRangeException)
            {
                HdrHistogramTickErrorCnt++;
                if (elapsedTicks > Interlocked.Read(ref MaxHdrHistogramTickErrorValue))
                {
                    Interlocked.Exchange(ref MaxHdrHistogramTickErrorValue, elapsedTicks);
                    Logger.Instance.Warn($"HdrHistogram application setting \"HGHighestTickValue\" needs to be greater than {elapsedTicks:###,###,###,##0}. Error Cnt: {HdrHistogramTickErrorCnt}");
                }
                else if (elapsedTicks < Interlocked.Read(ref MinHdrHistogramTickErrorValue))
                {
                    Interlocked.Exchange(ref MinHdrHistogramTickErrorValue, elapsedTicks);
                    Logger.Instance.Warn($"HdrHistogram application setting \"HGLowestTickValue\" needs to be less than {elapsedTicks:###,###,###,##0}. Error Cnt: {HdrHistogramTickErrorCnt}");
                }               
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StopRecord(this Stopwatch stopWatch, string type, string systemName, string setName, string funcName, long pk)
            => StopRecord(stopWatch, type, systemName, setName, funcName, pk.ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StopRecord(this Stopwatch stopWatch, string type, string systemName, string setName, string funcName, int pk)
           => StopRecord(stopWatch, type, systemName, setName, funcName, pk.ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StopRecord(this Stopwatch stopWatch, string type, string systemName, string setName, string funcName, object pk)
            => StopRecord(stopWatch, type, systemName, setName, funcName, pk?.ToString());
        

        public static void ToCSV(string csvFile)
        {
            if (!string.IsNullOrEmpty(csvFile)
                    && CaptureType.HasFlag(CaptureTypes.CSV)
                    && CaptureType.HasFlag(CaptureTypes.Detail))
            {
                var csvString = new StringBuilder();

                using var sw = new StreamWriter(csvFile);

                csvString.Append(nameof(PrefStat.AppElapsedTime))
                        .Append(',')
                        .Append(nameof(PrefStat.SequenceNbr))
                        .Append(',')
                        .Append(nameof(PrefStat.Timing))
                        .Append("(ms)")
                        .Append(',')
                        .Append(nameof(PrefStat.System))
                        .Append(',')
                        .Append(nameof(PrefStat.Setname))
                        .Append(',')
                        .Append(nameof(PrefStat.Event))
                        .Append(',')
                        .Append(nameof(PrefStat.FuncName))
                        .Append(',')
                        .Append(nameof(PrefStat.PK));

                sw.WriteLine(csvString);
                csvString.Clear();

                foreach (var stat in ConcurrentCollection)
                {
                    csvString.Append(stat.AppElapsedTime.ToString(TimeSpanFormatString))
                                .Append(',')
                                .Append(stat.SequenceNbr)
                                .Append(',')
                                .Append(stat.Timing.TotalMilliseconds)
                                .Append(',')
                                .Append(stat.System)
                                .Append(',')
                                .Append(stat.Setname)
                                .Append(',')
                                .Append(stat.Event)
                                .Append(',')
                                .Append(stat.FuncName)
                                .Append(',')
                                .Append(stat.PK);
                    sw.WriteLine(csvString);
                    csvString.Clear();
                }
            }
        }

        public static void ToJson(string jsonTimingFile)
        {
            if (!string.IsNullOrEmpty(jsonTimingFile)
                    && CaptureType.HasFlag(CaptureTypes.JSON))
            {
                File.WriteAllText(jsonTimingFile,
                                    JsonSerializer.Serialize(ConcurrentCollection,
                                                                new JsonSerializerOptions()
                                                                {
                                                                    WriteIndented = true,
                                                                    IncludeFields = true,
                                                                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                                                                }));
            }
        }

        /// <summary>
        /// Creates a HdrHistogram
        /// <see cref="https://github.com/HdrHistogram"/>
        /// </summary>
        /// <param name="precision">
        ///     The number of significant decimal digits to which the histogram will maintain
        ///     value resolution and separation. Must be a non-negative integer between 0 and
        ///     5. Default is 3
        /// </param>
        /// <param name="lowest">
        ///     The lowest tick value that can be tracked (distinguished from 0) by the histogram.
        ///     Must be a positive integer that is >= 1. May be internally rounded down to nearest
        ///     power of 2.
        /// </param>
        /// <param name="highest">
        ///     The highest tick value to be tracked by the histogram. Must be a positive integer
        ///     that is >= (2 * HdrHistogram.HistogramFactory.LowestTrackableValue).
        ///     The default is 10 mins.
        /// </param>
        public static void CreateHistogram(int precision = 3,
                                            long lowest = 1,                                             
                                            long highest = 6000000000)
        {
            HdrHistogram = HistogramFactory
                            .With64BitBucketSize() //LongHistogram
                            .WithValuesFrom(lowest)
                            .WithValuesUpTo(highest)
                            .WithPrecisionOf(precision)
                            .WithThreadSafeWrites()
                            .WithThreadSafeReads()
                            .Create()
                            .GetIntervalHistogram();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="console"></param>
        /// <param name="logger"></param>
        /// <param name="file"></param>
        /// <param name="percentileTicksPerHalfDistance">
        /// The number of reporting points per exponentially decreasing half-distance
        /// </param>
        /// <param name="outputValueUnitScalingRatio">
        /// The scaling factor by which to divide histogram recorded values units in output.
        /// Use the HdrHistogram.OutputScalingFactor constant values to help choose an appropriate output measurement.
        /// <seealso cref="OutputScalingFactor"/>
        /// </param>
        /// <returns></returns>
        public static string OutputHistogram(Common.ConsoleWriter console,
                                                Common.Logger logger,
                                                string file = null,
                                                int percentileTicksPerHalfDistance = 5,
                                                double outputValueUnitScalingRatio = 1.0)
        {
            if(HdrHistogram is not null
                && CaptureType.HasFlag(CaptureTypes.Histogram))
            {
                var writer = new StringWriter();

                HdrHistogram.OutputPercentileDistribution(writer,
                                                            percentileTicksPerHalfDistance,
                                                            outputValueUnitScalingRatio);
                var histOutputResult = writer.ToString();

                if (HdrHistogramTickErrorCnt > 0)
                {
                    histOutputResult += $"\nWarning: The HdrHistogram could be incomplete!\nCheck Application Log for HDRHistorgram Errors ({HdrHistogramTickErrorCnt})!";                    
                }

                logger?.Info($"HdrHistogram Output:\n{histOutputResult}");
                console?.WriteLine(histOutputResult);
               
                if(file is not null)
                {
                    if (CaptureType.HasFlag(CaptureTypes.HGRM))
                    {
                        File.WriteAllText(file, histOutputResult);
                    }
                    else if (CaptureType.HasFlag(CaptureTypes.CSV)
                                && !CaptureType.HasFlag(CaptureTypes.Detail))
                    {
                        using var fileWriter = new StreamWriter(file);

                        HdrHistogram.OutputPercentileDistribution(fileWriter,
                                                                        percentileTicksPerHalfDistance,
                                                                        outputValueUnitScalingRatio,
                                                                        true);
                    }
                }
                return histOutputResult;
            }
            return string.Empty;
        }
    }

    public class TimespanConverterMS : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TimeSpan.FromMilliseconds((double)reader.GetDouble());

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.TotalMilliseconds);
    }

    public class TimespanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TimeSpan.ParseExact(reader.GetString(), PrefStats.TimeSpanFormatString, null);

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(PrefStats.TimeSpanFormatString));
    }
}
