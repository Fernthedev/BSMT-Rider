using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features;
using JetBrains.Rider.Model;
using ReSharperPlugin.BSMT_Rider.Rider.Model;

namespace ReSharperPlugin.BSMT_Rider
{
    [SolutionComponent]
    public class ProjectProtocolHandler
    {
        public ProjectProtocolHandler(ISolution solution)
        {
            var model = solution.GetProtocolSolution().GetBSMT_RiderModel();
            model.FoundBeatSaberLocations
                .Set((t, _) => RdTask<string[]>.Successful(BeatSaberPathUtils.GetInstallDir().ToArray()));
        }
    }
}