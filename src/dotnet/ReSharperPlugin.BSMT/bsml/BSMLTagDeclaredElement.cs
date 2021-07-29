using System;
using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util.DataStructures;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    // This is a reference to the declaration.
    // We might want to cache BSMLTagDeclaration
    public class BSMLTagElement : IDeclaredElement
    {
        private readonly IXmlTag _tag;
        private readonly string _id;

        private readonly Lazy<BSMLTagDeclaration> _bsmlTagDeclaration;

        public BSMLTagElement(IXmlTag tag, string id)
        {
            _tag = tag;
            _id = id;

            _bsmlTagDeclaration = new Lazy<BSMLTagDeclaration>(() => new BSMLTagDeclaration(_tag, this));
        }


        public DeclaredElementType GetElementType()
        {
            return BSMLTagElementType.BSML_TAG;
        }

        public bool IsValid()
        {
            return _tag.IsValid();
        }

        public bool IsSynthetic()
        {
            return _bsmlTagDeclaration.Value.IsSynthetic();
        }

        public IList<IDeclaration> GetDeclarations()
        {
            return new List<IDeclaration> {_bsmlTagDeclaration.Value};
        }

        public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
        {
            return !HasDeclarationsIn(sourceFile) ? new List<IDeclaration>() : GetDeclarations();
        }

        public HybridCollection<IPsiSourceFile> GetSourceFiles()
        {
            return new(_tag.GetSourceFile()!);
        }

        public bool HasDeclarationsIn(IPsiSourceFile sourceFile)
        {
            return _tag.GetSourceFile()!.Equals(sourceFile);
        }

        public IPsiServices GetPsiServices()
        {
            return _tag.GetPsiServices();
        }

        public XmlNode GetXMLDoc(bool inherit)
        {
            return null;
        }

        public XmlNode GetXMLDescriptionSummary(bool inherit)
        {
            return null;
        }

        public string ShortName => _id;

        public bool CaseSensitiveName => true;
        public PsiLanguageType PresentationLanguage => CSharpLanguage.Instance; //XmlLanguage.Instance;
    }
}