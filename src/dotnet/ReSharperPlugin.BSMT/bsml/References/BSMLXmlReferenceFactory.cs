using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BsmlXmlReferenceFactory : IReferenceFactory
    {
        private readonly IXmlFile _xmlFile;
        private readonly BSMLFileManager _bsmlFileManager;

        /// <summary>
        /// C# classes that reference this BSML file
        /// </summary>
        private readonly List<ITypeDeclaration> _typeDeclarations = new();

        private readonly SemaphoreSlim _slim = new(1, 1);

        public BsmlXmlReferenceFactory(IXmlFile xmlFile, BSMLFileManager bsmlFileManager)
        {
            _xmlFile = xmlFile;
            _bsmlFileManager = bsmlFileManager;
        }

        private async Task UpdateReferencesAsync()
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

        private void UpdateReferences()
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

            _typeDeclarations.AddRange(_bsmlFileManager.FindClassesAssociatedToBSMLFile(_xmlFile));

            _slim.Release();
        }


        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
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