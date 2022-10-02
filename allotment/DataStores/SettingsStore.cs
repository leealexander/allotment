using Allotment.DataStores.Models;
using System.Text.Json;

namespace Allotment.DataStores
{
    public interface ISettingsStore
    {
        Task<SettingsModel> GetAsync();
        Task StoreAsync(SettingsModel model);
    }

    public class SettingsStore : DataStore, ISettingsStore
    {
        public SettingsStore() : base("settings.json")
        {

        }

        public async Task<SettingsModel> GetAsync()
        {
            var fileName = GetFilename();
            if (File.Exists(fileName))
            {
                using var stream = File.OpenRead(fileName);
                if (stream != null)
                {
                    var result = await JsonSerializer.DeserializeAsync<SettingsModel>(stream);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return new SettingsModel();
        }

        public async Task StoreAsync(SettingsModel model)
        {
            using FileStream createStream = File.Create(GetFilename());
            await JsonSerializer.SerializeAsync(createStream, model);
        }
    }
}
