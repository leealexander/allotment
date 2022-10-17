using Allotment.DataStores.Models;
using Allotment.Machine.Models;
using System.Collections.Generic;
using System.Text.Json;
using UnitsNet.Units;

namespace Allotment.DataStores
{
    public interface ILogsStore
    {
        Task<IEnumerable<LogEntryModel>> GetDayLogsAsync(DateOnly ?dt = null);
        Task StoreAsync(LogEntryModel model);
    }

    public class LogsStore : DataStore, ILogsStore
    {
        public LogsStore() : base("logs/$date.csv")
        {
        }


        public async Task<IEnumerable<LogEntryModel>> GetDayLogsAsync(DateOnly? dt = null)
        {
            var fileName = GetFilename(dt);
            if (File.Exists(fileName))
            {
                return (from fl in await File.ReadAllLinesAsync(fileName)
                        let split = fl.Split(',')
                        where split.Length == 3
                        select new LogEntryModel
                        {
                            EventDateUtc = DateTime.Parse(split[0]).ToUniversalTime(),
                            Area = split[1],
                            Message = split[2],
                        }).ToList();
            }

            return Enumerable.Empty<LogEntryModel>();
        }

        public async Task StoreAsync(LogEntryModel model)
        {
            await File.AppendAllLinesAsync(GetFilename(), new[] { $"{model.EventDateUtc:o},{model.Area},{model.Message}" });
        }
    }
}
