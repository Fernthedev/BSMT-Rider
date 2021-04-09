using JetBrains.Application.BuildScript.Application.Zones;

namespace ReSharperPlugin.BSMT_Rider
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IBSMT_RiderZone>
    {
    }
}