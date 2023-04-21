using Allotment.DataStores.Models;
using Allotment.Utils;
using System.Text.Json;

namespace Allotment.DataStores
{
    public interface ISettingsStore : IStateStore<SettingsModel>
    {
    }

    public class SettingsStore : StateStore<SettingsModel>, ISettingsStore
    {
        public SettingsStore(IFileSystem fileSystem)
            : base(fileSystem) { }
    }
}
