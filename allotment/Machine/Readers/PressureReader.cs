using Allotment.DataStores;
using Allotment.Machine.Monitoring.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using System.Text.RegularExpressions;

namespace Allotment.Machine.Readers
{
    public interface IPressureReader: IAsyncDisposable
    {
        Task ListenAsync();
        Task<IEnumerable<WaterLevelReadingModel>> StopListeningAsync();
    }

    public class PressureReader : IPressureReader
    {
        private readonly ISettingsStore _settings;
        private IMqttClient? _mqttClient;
        private List<WaterLevelReadingModel>? _readings = null;
        private Regex _readingParser = new Regex(@"time\s*=\s*(\d+),\s*pressure\s*=\s*(\d+)", RegexOptions.IgnoreCase);

        public PressureReader(ISettingsStore settings)
        {
            _settings = settings;
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
        }

        public async Task ListenAsync()
        {
            if (_mqttClient != null)
            {
                throw new NotSupportedException($"Listen has already been called.");
            }

            var settings = (await _settings.GetAsync()).Irrigation.WaterLevelSensor.Mqtt;
            if (settings.Server == null || settings.Username == null || settings.Password == null || settings.WaterPressureTopic == null)
            {
                throw new NotSupportedException("MQTT Settings need to be applied before pressure readings can be read.");
            }

            _readings = new();
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("9e2c992521034d659a18ceb2c1fa09b7.s2.eu.hivemq.cloud", 8883)
                    .WithCredentials("allotment", "REW3ake!gbc6dra@baq")
                    .WithTls()
            .Build();

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                var match = _readingParser.Match(payload);
                if (match.Success)
                {
                    _readings.Add(new WaterLevelReadingModel
                    {
                        DateTakenUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(int.Parse(match.Groups[1].Value)),
                        Reading = int.Parse(match.Groups[1].Value)
                    });
                }
                return Task.CompletedTask;
            };

            await _mqttClient.ConnectAsync(mqttClientOptions);


            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(settings.WaterPressureTopic);
                    })
            .Build();

            await _mqttClient.SubscribeAsync(mqttSubscribeOptions);
        }

        public async Task<IEnumerable<WaterLevelReadingModel>> StopListeningAsync()
        {
            if (_mqttClient == null)
            {
                throw new NotSupportedException($"Listen method needs to be called first.");
            }
            if (_readings == null)
            {
                throw new NotSupportedException();
            }

            await DisconnectAsync();

            var readings = _readings;
            _readings = null;
            return readings;
        }

        private async Task DisconnectAsync()
        {
            if (_mqttClient != null)
            {
                await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build());
                _mqttClient.Dispose();
                _mqttClient = null;
            }
        }
    }
}
