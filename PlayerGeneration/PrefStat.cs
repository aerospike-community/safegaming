using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.IO;

namespace PlayerGeneration
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
        [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
        public TimeSpan AppElapsedTime;
        /// <summary>
        /// Timing of the measured event
        /// </summary>
        [JsonConverter(typeof(TimespanConverterMS))]
        [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
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
        public enum FileFormats
        {
            CSV = 0,
            JSON
        }

        public static bool EnableTimings { get; set;  } = true;
        public static FileFormats FileFormat { get; set; } = FileFormats.CSV; 
        public static Stopwatch RunningStopwatch { get; }  = Stopwatch.StartNew();
        private static long SequenceNbr = 0;
        public static ConcurrentQueue<PrefStat> ConcurrentCollection { get; } = new();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StopRecord(this Stopwatch stopWatch, string type, string systemName, string setName, string funcName, string pk)
        {
            stopWatch.Stop();
            if(EnableTimings)
                ConcurrentCollection.Enqueue(new PrefStat
                { SequenceNbr = Interlocked.Increment(ref SequenceNbr),
                    AppElapsedTime = RunningStopwatch.Elapsed,
                    Timing = stopWatch.Elapsed,
                    Event = type,
                    System = systemName,
                    Setname = setName,
                    FuncName = funcName,
                    PK = pk
                });
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
                csvString.Append(stat.AppElapsedTime.ToString(TimespanConverter.TimeSpanFormatString))
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

        public static void ToJson(string jsonTimingFile)
        {
            var serializer = new JsonSerializer()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            using var sw = new StreamWriter(jsonTimingFile);
            using var writer = new JsonTextWriter(sw);
            
            serializer.Serialize(writer, ConcurrentCollection);                
            
        }
    }

    public class TimespanConverterMS : JsonConverter<TimeSpan>
    {
        
        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            writer.WriteValue(value.TotalMilliseconds);
        }

        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return TimeSpan.FromMilliseconds((double)reader.Value);
        }
    }

    public class TimespanConverter : JsonConverter<TimeSpan>
    {
        /// <summary>
        /// Format: Days.Hours:Minutes:Seconds:Milliseconds
        /// </summary>
        public const string TimeSpanFormatString = @"hh\:mm\:ss\.fffffff";

        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            var timespanFormatted = $"{value.ToString(TimeSpanFormatString)}";
            writer.WriteValue(timespanFormatted);
        }

        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            TimeSpan.TryParseExact((string)reader.Value, TimeSpanFormatString, null, out TimeSpan parsedTimeSpan);
            return parsedTimeSpan;
        }
    }
}
