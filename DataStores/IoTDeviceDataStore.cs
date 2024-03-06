namespace Miot.DataStores;

public class IoTDeviceDataStore
{
    private readonly HashSet<string> _devicesData = [];

    public void AddIoTDeviceData(string jsonData)
    {
        _devicesData.Add(jsonData);
    }

    public IEnumerable<string> GetDevicesData()
    {
        return _devicesData.ToList();
    }
}