using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetExtensions;
using JetBrains.ReSharper.Host.Features.ProjectModel.ProjectTemplates.DotNetTemplates;
using JetBrains.Rider.Model;
using JetBrains.Util;


namespace ReSharperPlugin.BSMT_Rider
{
    public class ModVersionTemplateParameter : DotNetTemplateParameter
    {
        public ModVersionTemplateParameter() : base("ModVersion", "Mod Version",
            "Version of mod")
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

            return new RdProjectTemplateTextParameter(Name, "ModVersion",
                "1.0.0", "Version of mod", 0, content);
        }
    }

    [ShellComponent]
    public class ModVersionTemplateParameterProvider : IDotNetTemplateParameterProvider
    {
        public int Priority => 50;

        public IReadOnlyCollection<DotNetTemplateParameter> Get()
        {
            return new[] {new ModVersionTemplateParameter()};
        }
    }
}