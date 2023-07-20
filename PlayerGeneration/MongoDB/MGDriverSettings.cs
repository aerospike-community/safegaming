using MongoDB.Driver;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static MongoDB.Driver.WriteConcern;

namespace PlayerGenerationMG
{
    public class MGDriverSettings
    {

        public MGDriverSettings()
        { }


        public bool? AllowInsecureTls;

        //public AutoEncryptionOptions AutoEncryptionOptions;

        public List<CompressorType> Compressors;

        public int? ConnectTimeout;

        public bool? DirectConnection;

        public int? HeartbeatInterval;

        public int? HeartbeatTimeout;

        public bool? IPv6;

        public bool? LoadBalanced;

        public int? LocalThreshold;

        //public LoggingSettings LoggingSettings;

        public int? MaxConnecting;

        public int? MaxConnectionIdleTime;

        public int? MaxConnectionLifeTime;

        public int? MaxConnectionPoolSize;

        public int? MinConnectionPoolSize;

        //public ReadConcern ReadConcern;

        //public UTF8Encoding ReadEncoding;

        //public ReadPreference ReadPreference;

        public bool? RetryReads;

        public bool? RetryWrites;

        public int? ServerSelectionTimeout;

        public int? SocketTimeout;

        public int? SrvMaxHosts;

        //public SslSettings SslSettings;

        public bool? UseTls;

        public int? WaitQueueTimeout;

        public MGWriteConcern WriteConcern;

        public void SetConnectionSettings(MongoClientSettings settings)
        {
            var thisFields = typeof(MGDriverSettings).GetFields();

            foreach (var fld in thisFields)
            {
                var prop = typeof(MongoClientSettings).GetProperty(fld.Name);
                var fldValue = fld.GetValue(this);
                
                if (fldValue is not null)
                {
                    if (fld.Name == "Compressors"
                            && fldValue is IEnumerable<CompressorType> cTypes)
                    {
                        prop.SetValue(settings, cTypes
                                                    .Where(c => c != CompressorType.Noop)
                                                    .Select(c => new CompressorConfiguration(c))
                                                    .ToList());
                    }
                    else if(fld.Name == "WriteConcern" && fldValue is not null)
                    {
                        prop.SetValue(settings,
                                        ((MGWriteConcern)fldValue).CreateWriteConcern());
                    }
                    else if (prop.PropertyType == typeof(TimeSpan))
                        prop.SetValue(settings,
                                        TimeSpan.FromMilliseconds((double)Convert.ChangeType(fldValue, typeof(double))));
                    else
                        prop.SetValue(settings, fldValue);
                }

            }
        }

    }

    public sealed class MGWriteConcern
    {
        public MGWriteConcern()
        { }

        public int? timeout;
        public bool? journal;
        public bool? fsync;
        public string WValue;

        public WriteConcern CreateWriteConcern()
        {
            Optional<TimeSpan?> wTimeout = default(Optional<TimeSpan?>);
            Optional<bool?> journal = default(Optional<bool?>);
            Optional<bool?> fsync = default(Optional<bool?>);
            Optional<WValue> wValue = default(Optional<WValue>);

            if(this.timeout.HasValue)
            {
                wTimeout = new Optional<TimeSpan?>(TimeSpan.FromMilliseconds(this.timeout.Value));
            }
            if(this.journal.HasValue) 
            { 
                journal = new Optional<bool?>(this.journal.Value);
            }
            if(this.fsync.HasValue) 
            {
                fsync = new Optional<bool?>(this.fsync.Value);
            }
            if(!string.IsNullOrEmpty(this.WValue))
            {
                if(int.TryParse(this.WValue, out var value))
                {
                    wValue = new Optional<WValue>(new WCount(value));
                }
                else
                {
                    wValue = new Optional<WValue>(new WMode(this.WValue));
                }
            }

            return new WriteConcern(wValue, wTimeout, journal, fsync);
        }
    }
    
}
