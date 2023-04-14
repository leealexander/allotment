using EpeverReader.Accessors;
using NModbus;
using NModbus.Serial;
using System.IO.Ports;

using var serialPort = new SerialPort("COM4", baudRate: 115200);
serialPort.DataBits = 8;
serialPort.StopBits = StopBits.One;
serialPort.Parity = Parity.None;
serialPort.Open();

var factory = new ModbusFactory();

IModbusSerialMaster master = factory.CreateRtuMaster(serialPort);


byte slaveId = 1;
var status = await StatusAccesssor.AccessAsync(slaveId, master);
var ev = await PowerAndTempAccessor.AccessAsync(slaveId, master);

Console.WriteLine($"****Status****");
Console.WriteLine($"Load = {status.Load.Description}");
Console.WriteLine($"Battery = {status.Battery.Description}");
Console.WriteLine($"Controller = {status.Controller.Description}");

Console.WriteLine();
Console.WriteLine($"****Power & Temp****");
Console.WriteLine($"Load Voltage = {ev.Load.Voltage}v");
Console.WriteLine($"Load Current = {ev.Load.Current}a");
Console.WriteLine($"Load Power = {ev.Load.Watts}w");

Console.WriteLine();
Console.WriteLine($"****Battery****");
Console.WriteLine($"Battery Voltage = {ev.Battery.Voltage}v");
Console.WriteLine($"Battery Temp = {ev.Battery.Temperature}c");

Console.WriteLine();
Console.WriteLine($"****Device****");
Console.WriteLine($"Device Temp = {ev.Device.Temperature}c");


serialPort.Close();



