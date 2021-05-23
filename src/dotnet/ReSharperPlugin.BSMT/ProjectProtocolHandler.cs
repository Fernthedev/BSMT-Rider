using JetBrains.Application;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd;
using JetBrains.Rd.Tasks;
using JetBrains.Util;
using ReSharperPlugin.BSMT_Rider.Rider.Model;

namespace ReSharperPlugin.BSMT_Rider
{
    [ShellComponent]
    public class ProjectProtocolHandler
    {
        public BSMT_RiderModel BsmtRiderModel { get; private set; }

        public ProjectProtocolHandler(Lifetime lifetime, IProtocol protocol, ILogger logger)
        {
            logger.Log(LoggingLevel.INFO,$"{protocol.Name} is protocol");
            BsmtRiderModel = new BSMT_RiderModel(lifetime, protocol);
            BSMT_RiderModel.RegisterDeclaredTypesSerializers(protocol.Serializers);

            BsmtRiderModel.FoundBeatSaberLocations
                .Set((t, _) => RdTask<string[]>.Successful(BeatSaberPathUtils.GetInstallDir().ToArray()));
        }
    }
}