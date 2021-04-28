using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetExtensions;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetTemplates;
using JetBrains.Rider.Model;
using JetBrains.Util;

// TODO: Somehow validate if custom path folder is a Beat Saber folder
namespace ReSharperPlugin.BSMT_Rider
{
    public class BeatSaberPathTemplateParameter : DotNetTemplateParameter
    {
        public BeatSaberPathTemplateParameter() : base("PathToBeatSaber", "Path to BeatSaber game directory",
            "Path to BeatSaber game directory")
        {
        }

        public override RdProjectTemplateContent CreateContent(DotNetProjectTemplateExpander expander,
            IDotNetTemplateContentFactory factory,
            int index, IDictionary<string, string> context)
        {
            var content = factory.CreateNextParameters(new[] {expander}, index + 1, context);
            var parameter = expander.TemplateInfo.GetParameter(Name);
            if (parameter == null)
            {
                return content;
            }

            var possiblePaths = BeatSaberPathUtils.GetInstallDir();
            var options = new List<RdProjectTemplateGroupOption>();

            if (!possiblePaths.IsNullOrEmpty())
            {
                var optionContext = new Dictionary<string, string>(context) {{Name, possiblePaths}};
                var content1 = factory.CreateNextParameters(new[] {expander}, index + 1, optionContext);

                options.Add(new RdProjectTemplateGroupOption(
                    possiblePaths,
                    possiblePaths,
                    null,
                    content1));
            }

            var customPathBox = new RdProjectTemplateTextParameter(Name, "Custom path", null, Tooltip,
                RdTextParameterStyle.FileChooser,
                content);

            options.Add(new RdProjectTemplateGroupOption(
                "Custom",
                !possiblePaths.IsNullOrEmpty() ? "Custom" : "Custom (Beat Saber installation was not found)",
                null,
                customPathBox));

            return new RdProjectTemplateGroupParameter(Name, "BeatSaberPath",
                !possiblePaths.IsNullOrEmpty() ? possiblePaths : string.Empty, null, options);
        }
    }

    [ShellComponent]
    public class BeatSaberPathParameterProvider : IDotNetTemplateParameterProvider
    {
        public int Priority => 50;

        public IReadOnlyCollection<DotNetTemplateParameter> Get()
        {
            return new[] {new BeatSaberPathTemplateParameter()};
        }
    }
}