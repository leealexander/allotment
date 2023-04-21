using Allotment.DataStores;
using Allotment.Machine.Monitoring.Models;
using Allotment.Utils;
using Moq;

namespace AllotmentTests.DataStores
{
    [TestClass]
    public class SolarStoreTests
    {
        private readonly Mock<IFileSystem> _fileSystem = new(); 
        private readonly SolarStore _store;

        public SolarStoreTests()
        {
            _store = new SolarStore(_fileSystem.Object);
        }

        [TestMethod]
        public async Task GetReadingsByHourAsync_ReadingList1Row_AveragesCorrectly() 
        {
            // arrange
            _fileSystem.Setup(x=>x.Exists(It.IsAny<string>())).Returns(true);
            _fileSystem.Setup(x => x.ReadAllLinesAsync(It.IsAny<string>(), default)).ReturnsAsync(new[]
            {
                SolarStore.ToCsv(new Allotment.Machine.Monitoring.Models.SolarReadingModel
                {
                    DateTakenUtc = new DateTime(2023, 1, 1, 0, 0, 0),
                    SolarPanel = new ElectricalVariables
                    {
                        Current = 10,
                        Voltage = 20,
                        Watts = 30
                    },
                    Load = new ElectricalVariables
                    {
                        Current = 40,
                        Voltage = 50,
                        Watts = 60
                    },
                    Battery = new Battery
                    {
                        Current = 70,
                        Voltage = 80,
                        StateOfCharge = 33,
                        Temperature = 19
                    }
                })
            });

            // act
            var results = await _store.GetReadingsByHourAsync();

            // assert
            var tw = results[0];
            Assert.AreEqual(10, tw.SolarPanel.Current);
            Assert.AreEqual(20, tw.SolarPanel.Voltage);
            Assert.AreEqual(30, tw.SolarPanel.Watts);


            Assert.AreEqual(40, tw.Load.Current);
            Assert.AreEqual(50, tw.Load.Voltage);
            Assert.AreEqual(60, tw.Load.Watts);

            Assert.AreEqual(70, tw.Battery.Current);
            Assert.AreEqual(80, tw.Battery.Voltage);
            Assert.AreEqual(33, tw.Battery.StateOfCharge);
            Assert.AreEqual(19, tw.Battery.Temperature);
        }
        [TestMethod]
        public async Task GetReadingsByHourAsync_ReadingListMultiRow_AveragesCorrectly()
        {
            // arrange
            _fileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            _fileSystem.Setup(x => x.ReadAllLinesAsync(It.IsAny<string>(), default)).ReturnsAsync(new[]
            {
                SolarStore.ToCsv(new Allotment.Machine.Monitoring.Models.SolarReadingModel
                {
                    DateTakenUtc = new DateTime(2023, 1, 1, 0, 0, 0),
                    SolarPanel = new ElectricalVariables
                    {
                        Current = 10,
                        Voltage = 20,
                        Watts = 30
                    },
                    Load = new ElectricalVariables
                    {
                        Current = 40,
                        Voltage = 50,
                        Watts = 60
                    },
                    Battery = new Battery
                    {
                        Current = 70,
                        Voltage = 80,
                        StateOfCharge = 33,
                        Temperature = 42
                    }
                }),
                SolarStore.ToCsv(new Allotment.Machine.Monitoring.Models.SolarReadingModel
                {
                    DateTakenUtc = new DateTime(2023, 1, 1, 0, 1, 0),
                    SolarPanel = new ElectricalVariables
                    {
                        Current = 20,
                        Voltage = 30,
                        Watts = 40
                    },
                    Load = new ElectricalVariables
                    {
                        Current = 50,
                        Voltage = 60,
                        Watts = 70
                    },
                    Battery = new Battery
                    {
                        Current = 80,
                        Voltage = 90,
                        StateOfCharge = 66,
                        Temperature = 82
                    }
                })
            });

            // act
            var results = await _store.GetReadingsByHourAsync();

            // assert
            var tw = results[0];
            Assert.AreEqual(15, tw.SolarPanel.Current);
            Assert.AreEqual(25, tw.SolarPanel.Voltage);
            Assert.AreEqual(35, tw.SolarPanel.Watts);


            Assert.AreEqual(45, tw.Load.Current);
            Assert.AreEqual(55, tw.Load.Voltage);
            Assert.AreEqual(65, tw.Load.Watts);

            Assert.AreEqual(75, tw.Battery.Current);
            Assert.AreEqual(85, tw.Battery.Voltage);
            Assert.AreEqual(49, tw.Battery.StateOfCharge);
            Assert.AreEqual(62, tw.Battery.Temperature);
        }
    }
}
