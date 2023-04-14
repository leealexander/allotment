using EpeverReader.Stats;
using NModbus;
using NModbus.Extensions.Enron;
using System.Net;

namespace EpeverReader.Accessors
{
    public class PowerAndTempAccessor
    {
        public static async Task<PowerAndTemp> AccessAsync(byte slaveId, IModbusSerialMaster master)
        {
            var dataList = new List<ushort>();
            var eRegisters = await master.ReadInputRegistersAsync(slaveId, 12544, 18);
            var batteryRegisters = await master.ReadInputRegistersAsync(slaveId, 13082, 3);
            double RegisterToValue(ushort[] array, int index) => (double)array[index] / 100D;
            double RegisterToValueX2(ushort[] array, int index) => (double)(((int)array[index+1] << 16) + (int)array[index]) / 100.0;
            var result = new PowerAndTemp
            {
                SolarPanel = new ElectricalVariables 
                {
                    Voltage = RegisterToValue(eRegisters, Indexes.ArrayVoltage),
                    Current = RegisterToValue(eRegisters, Indexes.ArrayCurrent),
                    Watts = RegisterToValueX2(eRegisters, Indexes.ArrayPower),
                },
                Load = new ElectricalVariables
                {
                    Voltage = RegisterToValue(eRegisters, Indexes.loadVoltage),
                    Current = RegisterToValue(eRegisters, Indexes.LoadCurrent),
                    Watts = RegisterToValueX2(eRegisters, Indexes.LoadPower),
                },
                Battery = new Battery
                {
                    Temperature = eRegisters[Indexes.BatteryTemp] / 100D,
                    Voltage = RegisterToValue(batteryRegisters, 0),
                    Current = RegisterToValueX2(batteryRegisters,1),
                    StateOfCharge = (await master.ReadInputRegistersAsync(slaveId, 12570, 1))[0]
                },
                Device = new Device { Temperature = eRegisters[Indexes.DeviceTemp] / 100D }
            };

            return result;
        }


        private static class Indexes
        {
            public const int ArrayVoltage = 0;
            public const int ArrayCurrent = 1;
            public const int ArrayPower = 2;
            public const int loadVoltage = 12;
            public const int LoadCurrent = 13;
            public const int LoadPower = 14;
            public const int BatteryTemp = 16;
            public const int DeviceTemp = 17;
        }
    }
}
