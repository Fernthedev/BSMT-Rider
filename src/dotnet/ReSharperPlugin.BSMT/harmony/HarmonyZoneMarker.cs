using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;

namespace ReSharperPlugin.BSMT_Rider.harmony;

[ZoneMarker]
public class HarmonyZoneMarker : IZone, IRequire<PsiFeaturesImplZone>, IRequire<ITextControlsZone>, IRequire<IProjectModelZone>, IRequire<IDocumentModelZone>
{
    
}