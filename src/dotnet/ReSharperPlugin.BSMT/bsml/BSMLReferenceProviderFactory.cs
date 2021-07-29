using System.IO;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Paths;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    [ReferenceProviderFactory]
    public class BsmlReferenceProviderFactory : IReferenceProviderFactory
    {
        private readonly ILogger _logger;// = JetBrains.Util.Logging._logger.GetLogger(nameof(BsmlReferenceProviderFactory));

        public BsmlReferenceProviderFactory(Lifetime lifetime, ILogger logger)
        {
            Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName!);
            _logger = logger;
        }



        public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
        {


            // if (sourceFile.PrimaryPsiLanguage.Is<XmlLanguage>() && sourceFile.IsValid() && sourceFile.Name.EndsWith(".bsml"))
            _logger.Info($"source file language factory");

            var project = sourceFile.GetProject();

            if (project == null)
                return null;

            if (!sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>()) return null;





            _logger.Info($"Looking for file {sourceFile.GetLocation().Parent.FullPath} {FileSystemPath.ParseRelativelyTo(Path.ChangeExtension(sourceFile.Name, "bsml"), sourceFile.GetLocation().Parent).FullPath}");

            // TODO: Find through attribute
            IPsiSourceFile sourceFileInProject = project.GetPsiSourceFileInProject(
                FileSystemPath.ParseRelativelyTo(Path.ChangeExtension(sourceFile.Name, "bsml"), sourceFile.GetLocation().Parent));

            _logger.Info($"Xml Source file {(sourceFileInProject is null ? "null" : "not null")}");

            if (sourceFileInProject?.PrimaryPsiLanguage is UnknownLanguage)
                return null;

            var xmlFile = sourceFileInProject?.GetPsiFiles<XmlLanguage>().SafeOfType<IXmlFile>().SingleOrDefault();

            _logger.Info($"XML File is {(xmlFile is null ? "null" : "not null")}");


            return xmlFile != null ? new BSMLReferenceFactory(xmlFile) : null;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }
}