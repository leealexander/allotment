using Allotment.Machine.Monitoring.Models;
using NModbus;
using System.Collections;

namespace Allotment.Machine.SolarAccessors
{
    public class StatusAccesssor
    {
        private static StringStatusValue[] _arrayTaxonomy =
        {
            new() { Description = "Input", Health = Health.Good  },
            new() { Description = "CutOut", Health = Health.NotApplicable  },
            new() { Description = "ChargeMOSTShortCircuit", Health = Health.Bad  },
            new() { Description = "AntireverseMOSTShortCircuit", Health = Health.Bad  },
            new() { Description = "MOSTOpenCircuit", Health = Health.Bad  },
            new() { Description = "OutputOverCurrent", Health = Health.Bad  },
            new() { Description = "NotConnectToController", Health = Health.NotApplicable  },
            new() { Description = "PhotocellVoltTooHigh", Health = Health.Bad  },
            new() { Description = "PhotocellVoltError", Health = Health.Bad  },
        };

        private static StringStatusValue[] _chargeTaxonomy = 
        {
            new() { Description = "Not charging", Health = Health.NotApplicable  },
            new() { Description = "Float charge", Health = Health.Good  },
            new() { Description = "Raising charge", Health = Health.Good  },
            new() { Description = "Equalizing charge", Health = Health.Good  },
        };
        private static StringStatusValue[] _batteryStatusTaxonomy =
        {
            new() { Description = "Normal", Health = Health.Good  },
            new() { Description = "Overvoltage", Health = Health.Bad  },
            new() { Description = "Undervoltage", Health = Health.NotApplicable  },
            new() { Description = "Overdischarge", Health = Health.Bad  },
            new() { Description = "BatteryError", Health = Health.Bad  },
            new() { Description = "OverMaxTemp", Health = Health.Bad  },
            new() { Description = "UnderMinTemp", Health = Health.Bad  },
            new() { Description = "InnerResistanceError", Health = Health.Bad  },
        };
        private static StringStatusValue[] _loadStatusTaxonomy =
        {
            new() { Description = "On", Health = Health.Good  },
            new() { Description = "Off", Health = Health.NotApplicable  },
            new() { Description = "Overload", Health = Health.Bad  },
            new() { Description = "ShortCircuit", Health = Health.Bad  },
            new() { Description = "MOSTShortCircuit", Health = Health.Bad  },
            new() { Description = "OutputOvervoltage", Health = Health.Bad  },
            new() { Description = "BoosterOvervoltage", Health = Health.Bad  },
            new() { Description = "HighSideShortCircuit", Health = Health.Bad  },
            new() { Description = "OutputVoltageError", Health = Health.Bad  },
            new() { Description = "UnableToStopDischarging", Health = Health.Bad  },
            new() { Description = "UnableToDischarge", Health = Health.NotApplicable },
            new() { Description = "LoadOpenCircuit", Health = Health.NotApplicable  },
        };
        private static StringStatusValue[] _controllerStatusTaxonomy =
        {
            new() { Description = "Normal", Health = Health.Good  },
            new() { Description = "Overheating", Health = Health.Bad  },
            new() { Description = "RatedVoltageError", Health = Health.Bad  },
            new() { Description = "ThreeWayImbalance", Health = Health.Bad  },
        };

  
        public static async Task FillAsync(byte slaveId, IModbusSerialMaster master, DeviceStatus model)
        {
            var rawData = new List<ushort>();

            rawData.AddRange(await master.ReadInputRegistersAsync(slaveId, 12800, 3));
            rawData.Add(System.Convert.ToUInt16((await master.ReadInputsAsync(slaveId, 8192, 1))[0]));

            var data = Convert(rawData);

            model.SolarPanel = ConvertRawValue("SolarPanel", _arrayTaxonomy, data, 0);
            model.Charge = ConvertRawValue("Charge", _chargeTaxonomy, data, 1);
            model.Battery = ConvertRawValue("Battery", _batteryStatusTaxonomy, data, 2);
            model.Load = ConvertRawValue("Load", _loadStatusTaxonomy, data, 3);
            model.Controller = ConvertRawValue("Controller", _controllerStatusTaxonomy, data, 4);
        }


        private static StringStatusValue ConvertRawValue(string description, StringStatusValue[] taxonomy, int[] data, int index)
        {
            if(index < 0 || index >= data.Length)
            {
                throw new NotSupportedException($"Invalid index for '{description}'");
            }
            var localIndex = data[index];
            if (localIndex < 0 || localIndex >= taxonomy.Length)
            {
                throw new NotSupportedException($"Invalid index for '{description}'");
            }

            return taxonomy[localIndex];
        }

        public static int[] Convert(List<ushort> data)
        {
            var statusIndexes = new int[5];
            var num1 = (int)data[0] & 15;
            var num2 = ((int)data[0] & 240) >> 4;
            var bitArray1 = new BitArray(BitConverter.GetBytes(data[0]));
            var bitArray2 = new BitArray(BitConverter.GetBytes(data[1]));
            var bitArray3 = new BitArray(BitConverter.GetBytes(data[2]));
            statusIndexes[1] = ((int)data[1] & 12) >> 2;
            statusIndexes[4] = !bitArray1[15] ? (!bitArray2[6] ? (int)data[3] : 3) : 2;
            statusIndexes[0] = !bitArray2[13] ? (!bitArray2[11] ? (!bitArray2[12] ? (!bitArray2[10] ? (((int)data[1] & 49152) >> 14 == 0 ? (statusIndexes[1] == 0 ? 1 : 0) : (((int)data[1] & 49152) >> 14) + 5) : 5) : 4) : 3) : 2;
            if (num1 != 0 || num2 != 0)
            {
                if (bitArray1[8])
                    statusIndexes[2] = 7;
                else if (num1 != 0)
                    statusIndexes[2] = num1;
                else if (num2 != 0)
                    statusIndexes[2] = num2 + 4;
            }
            else
            {
                statusIndexes[2] = 0;
            }
            if (bitArray3[1])
            {
                if (bitArray2[8] || bitArray3[11])
                {
                    statusIndexes[3] = 3;
                }
                else if (bitArray2[7])
                {
                    statusIndexes[3] = 4;
                }
                else if (bitArray2[9])
                {
                    statusIndexes[3] = 5;
                }
                else if (bitArray2[5])
                {
                    statusIndexes[3] = 12;
                }
                else if (bitArray3[12] && bitArray3[13])
                {
                    statusIndexes[3] = 2;
                }
                else
                {
                    for (int index = 4; index < 7; ++index)
                    {
                        if (bitArray3[index])
                        {
                            statusIndexes[3] = index + 2;
                            break;
                        }
                    }
                    for (int index = 8; index < 11; ++index)
                    {
                        if (bitArray3[index])
                        {
                            statusIndexes[3] = index + 1;
                            break;
                        }
                    }
                }
            }
            else
            {
                statusIndexes[3] = bitArray3[0] ? 0 : 1;
            }
            return statusIndexes;
        }

    }
}
