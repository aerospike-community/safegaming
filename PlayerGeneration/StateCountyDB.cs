using System;
using System.Collections.Generic;
using System.Text;

namespace PlayerGeneration
{
    public sealed class County
    {
        
        public string Name { get; set; }
        public string NameNormized { get; set; }
        public IEnumerable<string> Cities { get; set; }
        public IEnumerable<uint> ZipCodes { get; set; }
        public IEnumerable<ushort> AreaCodes { get; set; }
        public IEnumerable<string> TimeZones { get; set; }
        public IEnumerable<TimeSpan?> TZOffsets { get; set; }
        public IEnumerable<string> LocCodes { get; set; }
        public long AreaLandSqMeters { get; set; }
        public long AreaWaterSqMeters { get; set; }
        public decimal AreaWaterSqMiles { get; set; }
        public decimal? AreaLandSqMiles { get; set; }
        public long HousingCount { get; set; }
        public long PopulationCount { get; set; }
        public int FIPSCode { get; set; }
        public bool OnlineGaming { get; set; }
        public Tiers HouseIncomeTier { get; set; }
        public Tiers PopulationTier { get; set; }
        
    }

    public sealed class State
    {
        public string Name { get; set; }
        public IEnumerable<County> Counties { get; set; }

        public long AreaLandSqMeters { get; set; }
        public long AreaWaterSqMeters { get; set; }
        public decimal AreaWaterSqMiles { get; set; }
        public decimal AreaLandSqMiles { get; set; }
        public long HousingCount { get; set; }
        public long PopulationCount { get; set; }
        public int FIPSCode { get; set; }

    }


}
