using Allotment.DataStores.Models;
using System.Text.Json;

namespace Allotment.DataStores
{
    public interface ISettingsStore : IStateStore<SettingsModel>
    {
    }

    public class SettingsStore : StateStore<SettingsModel>, ISettingsStore
    {
    }
}
