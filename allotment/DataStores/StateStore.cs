using System.Text.Json;

namespace Allotment.DataStores
{
    public interface IStateStore<TModel> where TModel : class, new()
    {
        Task<TModel> GetAsync();
        TModel Get();

        Task StoreAsync(TModel model);
    }

    public class StateStore<TModel> : DataStore, IStateStore<TModel> where TModel : class, new()
    {
        public StateStore() : base(typeof(TModel).Name)
        {

        }

        public async Task<TModel> GetAsync()
        {
            var fileName = GetFilename();
            if (File.Exists(fileName))
            {
                using var stream = File.OpenRead(fileName);
                if (stream != null)
                {
                    var result = await JsonSerializer.DeserializeAsync<TModel>(stream);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return new TModel();
        }

        public TModel Get()
        {
            var fileName = GetFilename();
            if (File.Exists(fileName))
            {
                using var stream = File.OpenRead(fileName);
                if (stream != null)
                {
                    var result = JsonSerializer.Deserialize<TModel>(stream);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return new TModel();
        }

        public async Task StoreAsync(TModel model)
        {
            using FileStream createStream = File.Create(GetFilename());
            await JsonSerializer.SerializeAsync(createStream, model);
        }
    }
}
