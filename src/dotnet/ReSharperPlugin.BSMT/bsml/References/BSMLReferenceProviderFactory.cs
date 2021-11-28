using System.Collections.Generic;
using System.IO;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
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
        private readonly ILogger _logger;

        private readonly Dictionary<ICSharpFile, BSMLCSReferenceFactory> _referenceCsFactories = new();
        private readonly Dictionary<IXmlFile, BsmlXmlReferenceFactory> _referenceXmlFactories = new();

        private readonly BsmlFileManager _bsmlFileManager = new();

        public BsmlReferenceProviderFactory(Lifetime lifetime, ILogger logger)
        {
            Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName!);
            _logger = logger;
        }



        // This code is such a mess
        // I hate it
        public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
        {
            _logger.Info("source file language factory");

            var project = sourceFile.GetProject();

            if (project == null)
                return null;

            if (sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>() && file is ICSharpFile cSharpFile)
            {
                _logger.Info(
                    $"Looking for file {sourceFile.GetLocation().Parent.FullPath} {VirtualFileSystemPath.ParseRelativelyTo(Path.ChangeExtension(sourceFile.Name, "bsml"), sourceFile.GetLocation().Parent).FullPath}");

                if (!_referenceCsFactories.TryGetValue(cSharpFile, out var factory))
                {
                    _referenceCsFactories[cSharpFile] = factory = new BSMLCSReferenceFactory(cSharpFile, _bsmlFileManager, this);
                }

                return factory;
            }

            if ((sourceFile.PrimaryPsiLanguage.Is<XmlLanguage>() || sourceFile.PrimaryPsiLanguage.Is<BSMLLanguage>()) && file is IXmlFile xmlFile)
            {
                if (!_referenceXmlFactories.TryGetValue(xmlFile, out var factory))
                {
                    _referenceXmlFactories[xmlFile] = factory = new BsmlXmlReferenceFactory(xmlFile, _bsmlFileManager, this);
                }

                return factory;
            }

            return null;
        }

        public BsmlXmlReferenceFactory? GetXmlReferenceFactory(IXmlFile file)
        {
            return !_referenceXmlFactories.TryGetValue(file, out var factory) ? null : factory;
        }
        
        public BSMLCSReferenceFactory? GetCsReferenceFactory(ICSharpFile file)
        {
            return !_referenceCsFactories.TryGetValue(file, out var factory) ? null : factory;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }
}