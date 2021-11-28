using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BsmlXmlReferenceFactory : IReferenceFactory
    {
        private readonly IXmlFile _xmlFile;
        private readonly BsmlFileManager _bsmlFileManager;
        private readonly BsmlReferenceProviderFactory _bsmlReferenceProviderFactory;
        private BsmlXmlData? _bsmlXmlData;
        
        /// <summary>
        /// C# classes that reference this BSML file
        /// </summary>
        private readonly List<ITypeDeclaration> _typeDeclarations = new();
        private readonly Dictionary<ITypeDeclaration, BSMLCSReferenceFactory> _bsmlcsReferenceFactories = new();

        private readonly SemaphoreSlim _slim = new(1, 1);

        public BsmlXmlReferenceFactory(IXmlFile xmlFile, BsmlFileManager bsmlFileManager, BsmlReferenceProviderFactory bsmlReferenceProviderFactory)
        {
            _xmlFile = xmlFile;
            _bsmlFileManager = bsmlFileManager;
            _bsmlReferenceProviderFactory = bsmlReferenceProviderFactory;
        }

        public async Task UpdateReferencesAsync()
        {
            // Do not update again unnecessarily, just wait for the first thread to finish
            if (!await _slim.WaitAsync(0))
            {
                if (await _slim.WaitAsync(7000))
                {
                    _slim.Release();
                }
                else
                {
                    Console.WriteLine("h");
                }

                return;
            }

            await _slim.WaitAsync();

            UpdateReferencesInternal();
        }

        public void UpdateReferences()
        {
            // Do not update again unnecessarily, just wait for the first thread to finish
            if (!_slim.Wait(0))
            {
                if (_slim.Wait(7000))
                {
                    _slim.Release();
                }
                else
                {
                    Console.WriteLine("h");
                }
                return;
            }

            UpdateReferencesInternal();
        }

        /// <summary>
        /// Reads the class' [ViewDefinition("assemblyName")] attribute, then finds the XML file and
        /// associates the attribute's attached class to the file
        /// </summary>
        private void UpdateReferencesInternal()
        {
            var project = _xmlFile.GetProject();
            if (project is null)
                return;

            var sourceFile = _xmlFile.GetSourceFile();

            if (sourceFile is null)
                return;
            

            // Find C# classes that reference this file, then get their reference factory

            _typeDeclarations.Clear();
            _typeDeclarations.AddRange(_bsmlFileManager.FindClassesAssociatedToBsmlFile(_xmlFile));
            
            _bsmlcsReferenceFactories.Clear();
            foreach (var typeDeclaration in _typeDeclarations)
            {
                var csSourceFile = typeDeclaration.GetSourceFile();
                var file = csSourceFile?.GetTheOnlyPsiFile<CSharpLanguage>();

                if (csSourceFile is null || !csSourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>() || file is not ICSharpFile cSharpFile) continue;
                
                // should never be null
                var factory = _bsmlReferenceProviderFactory.GetCsReferenceFactory(cSharpFile)!;
                _bsmlcsReferenceFactories[typeDeclaration] = factory;
            }
            
            _bsmlXmlData = _bsmlFileManager.ParseBsml(_xmlFile);
            
            _slim.Release();
        }


        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            UpdateReferences();
            // TODO: Finish
            return ReferenceCollection.Empty;
        }

        private ReferenceCollection BsmlToSourceReference(ILiteralExpression literal)
        {
            return ReferenceCollection.Empty;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            return false;
        }
    }
}