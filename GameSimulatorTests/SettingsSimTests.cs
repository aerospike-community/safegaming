using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameSimulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameSimulator.Tests
{
    [TestClass()]
    public class SettingsSimTests
    {
        public class TestSettings : PlayerCommon.Settings
        {
            public TestSettings(string appJsonFile = "appsettings.json")
            : base(appJsonFile)
            {
                PlayerCommon.Settings.GetSetting(this.ConfigurationBuilderFile,
                                                    ref this.instanceTestSettings,
                                                    "TestSettings");
            }

            public class TestSettingsCls1
            {                
                public string stringFld;
                public int intFld;
                public decimal decimalFld;
                public double doubleFld;
                public long longFld;
                public List<int> listInts;
                public List<string> stringList;
                public TestSettingsCls instanceTestSettings;
            }

            public class TestSettingsCls
            {
                public string stringFld;
                public int intFld;
                public decimal decimalFld;
                public double doubleFld;
                public long longFld;
                public List<int> listInts;
                public List<string> stringList;
                public DateTime datetimeFld;
                public DateTimeOffset datetimeOffsetFld;
                public TimeSpan timeSpanFld;
                public DateTimeKind dateTimeKindFld = DateTimeKind.Unspecified;

                public TestSettingsCls1 testSettingsCls;

                public bool boolFld;
            }

            public TestSettingsCls instanceTestSettings;
        }

        [TestMethod()]
        public void SettingsSimTest()
        {
            var testAppSettingSim = new SettingsSim("appSettings1.json");

            Assert.IsNotNull(testAppSettingSim);
            
            var testAppSetting2 = new TestSettings("appSettings2.json");

            Assert.AreEqual(-1, testAppSetting2.MaxDegreeOfParallelism);
            Assert.AreEqual(-1, testAppSetting2.WorkerThreads);
            Assert.AreEqual(1000, testAppSetting2.CompletionPortThreads);
            Assert.IsFalse(testAppSetting2.IgnoreFaults);
            Assert.IsFalse(testAppSetting2.TimeEvents);
            Assert.AreEqual(100, testAppSetting2.WarnMaxMSLatencyDBExceeded);
            Assert.IsTrue(testAppSetting2.EnableHistogram);
            Assert.IsNull(testAppSetting2.HGRMFile);
            Assert.AreEqual(3,testAppSetting2.HGPrecision);
            Assert.AreEqual(1L,testAppSetting2.HGLowestTickValue);
            Assert.AreEqual(100000000L, testAppSetting2.HGHighestTickValue);
            Assert.AreEqual(5, testAppSetting2.HGReportPercentileTicksPerHalfDistance);
            Assert.AreEqual("Milliseconds", testAppSetting2.HGReportTickToUnitRatio);
            Assert.AreEqual(HdrHistogram.OutputScalingFactor.TimeStampToMilliseconds, testAppSetting2.HGReportUnitRatio);
            Assert.IsNull(testAppSetting2.TimingCSVFile);
            Assert.IsNull(testAppSetting2.TimingJsonFile);
            Assert.AreEqual("yyyy-MM-ddTHH:mm:ss.ffffzzz", testAppSetting2.TimeStampFormatString);
            Assert.AreEqual("yyyy-MM-ddTHH:mm:ss.ffff", testAppSetting2.TimeZoneFormatWoZone);
            Assert.IsNull(testAppSetting2.DBConnectionString);

            Assert.IsNotNull(testAppSettingSim);
            Assert.AreEqual("abcdef", testAppSetting2.instanceTestSettings.stringFld);
            Assert.AreEqual(1, testAppSetting2.instanceTestSettings.intFld);
            Assert.AreEqual(12.34m, testAppSetting2.instanceTestSettings.decimalFld);
            Assert.AreEqual(56.78d, testAppSetting2.instanceTestSettings.doubleFld);
            Assert.AreEqual(123456L, testAppSetting2.instanceTestSettings.longFld) ;
            CollectionAssert.AreEquivalent(new List<int>() { 1, 2, 3, 4, 5 }, testAppSetting2.instanceTestSettings.listInts);            
            CollectionAssert.AreEquivalent(new List<string>() { "a", "b", "c"}, testAppSetting2.instanceTestSettings.stringList);
            Assert.AreEqual(DateTime.Parse("2023-01-01 14:20:23.456"), testAppSetting2.instanceTestSettings.datetimeFld);
            Assert.AreEqual(DateTimeOffset.Parse("2023-01-01 14:20:23.456+07"), testAppSetting2.instanceTestSettings.datetimeOffsetFld);
            Assert.AreEqual(TimeSpan.Parse("14:20:23.456"), testAppSetting2.instanceTestSettings.timeSpanFld);
            Assert.AreEqual(DateTimeKind.Local, testAppSetting2.instanceTestSettings.dateTimeKindFld);

            Assert.IsNotNull(testAppSetting2.instanceTestSettings.testSettingsCls);
            Assert.AreEqual("ghijk", testAppSetting2.instanceTestSettings.testSettingsCls.stringFld);
            Assert.AreEqual(2, testAppSetting2.instanceTestSettings.testSettingsCls.intFld);
            Assert.AreEqual(567.890m, testAppSetting2.instanceTestSettings.testSettingsCls.decimalFld);
            Assert.AreEqual(910.123d, testAppSetting2.instanceTestSettings.testSettingsCls.doubleFld);
            Assert.AreEqual(7890L, testAppSetting2.instanceTestSettings.testSettingsCls.longFld);
            CollectionAssert.AreEquivalent(new List<int>() { 6, 7, 8, 9 }, testAppSetting2.instanceTestSettings.testSettingsCls.listInts);
            CollectionAssert.AreEquivalent(new List<string>() { "d", "e", "f" }, testAppSetting2.instanceTestSettings.testSettingsCls.stringList);
            
            Assert.IsNotNull(testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings);
            Assert.AreEqual("lmnop", testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.stringFld);
            Assert.AreEqual(3, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.intFld);
            Assert.AreEqual(890.123m, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.decimalFld);
            Assert.AreEqual(101.234d, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.doubleFld);
            Assert.AreEqual(89012, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.longFld);
            CollectionAssert.AreEquivalent(new List<int>() { 9, 0, 1, 0 }, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.listInts);
            CollectionAssert.AreEquivalent(new List<string>() { "g", "h", "i" }, testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.stringList);

            Assert.IsNull(testAppSetting2.instanceTestSettings.testSettingsCls.instanceTestSettings.testSettingsCls);

            Assert.IsTrue(testAppSetting2.instanceTestSettings.boolFld);

            Assert.AreEqual(1, PlayerCommon.Settings.NotFoundSettingClassProps.Count);
            Assert.AreEqual("TestSettings:NotValidProperty", PlayerCommon.Settings.NotFoundSettingClassProps.First());
        }
    }
}