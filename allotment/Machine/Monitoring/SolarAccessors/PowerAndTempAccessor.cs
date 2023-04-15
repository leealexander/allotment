using Allotment.Machine.Monitoring.Models;
using NModbus;

namespace Allotment.Machine.SolarAccessors
{
    public class PowerAndTempAccessor
    {
        public static async Task FillAsync(byte slaveId, IModbusSerialMaster master, SolarReadingModel model)
        {
            var eRegisters = await master.ReadInputRegistersAsync(slaveId, 12544, 18);
            var batteryRegisters = await master.ReadInputRegistersAsync(slaveId, 13082, 3);
            double RegisterToValue(ushort[] array, int index) => (double)array[index] / 100D;
            double RegisterToValueX2(ushort[] array, int index) => (double)(((int)array[index+1] << 16) + (int)array[index]) / 100.0;

            model.SolarPanel.Voltage = RegisterToValue(eRegisters, Indexes.ArrayVoltage);
            model.SolarPanel.Current = RegisterToValue(eRegisters, Indexes.ArrayCurrent);
            model.SolarPanel.Watts = RegisterToValueX2(eRegisters, Indexes.ArrayPower);

            model.Load.Voltage = RegisterToValue(eRegisters, Indexes.loadVoltage);
            model.Load.Current = RegisterToValue(eRegisters, Indexes.LoadCurrent);
            model.Load.Watts = RegisterToValueX2(eRegisters, Indexes.LoadPower);

            model.Battery.Temperature = eRegisters[Indexes.BatteryTemp] / 100D;
            model.Battery.Voltage = RegisterToValue(batteryRegisters, 0);
            model.Battery.Current = RegisterToValue(eRegisters, Indexes.LoadCurrent);
            model.Battery.StateOfCharge = (await master.ReadInputRegistersAsync(slaveId, 12570, 1))[0];

            model.DeviceStatus.Temperature = eRegisters[Indexes.DeviceTemp] / 100D;
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
