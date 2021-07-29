using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Text;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    // Why do I need to do this? please I want to use an existing XML class
    // IDeclaration is where the tag is in a file. Allows us
    // to ctrl-click to it
    public class BSMLTagDeclaration : IXmlTag, IDeclaration
    {
        private readonly BSMLTagElement _tagElement;
        private readonly IXmlTag _tag;

        // private IXmlAttribute idAttribute => _tag.GetAttributes()
            // .ToList().FirstOrDefault(attribute => attribute.AttributeName == "id");

        public BSMLTagDeclaration(IXmlTag tag, BSMLTagElement tagElement)
        {
            _tag = tag;
            _tagElement = tagElement;
        }

        public IPsiServices GetPsiServices()
        {
            return _tag.GetPsiServices();
        }

        public IPsiModule GetPsiModule()
        {
            return _tag.GetPsiModule();
        }

        public IPsiSourceFile GetSourceFile()
        {
            return _tag.GetSourceFile();
        }

        public ReferenceCollection GetFirstClassReferences()
        {
            return _tag.GetFirstClassReferences();
        }

        public void ProcessDescendantsForResolve(IRecursiveElementProcessor processor)
        {
            _tag.ProcessDescendantsForResolve(processor);
        }

        public TTreeNode GetContainingNode<TTreeNode>(bool returnThis = false) where TTreeNode : ITreeNode
        {
            return _tag.GetContainingNode<TTreeNode>(returnThis);
        }

        public bool Contains(ITreeNode other)
        {
            return _tag.Contains(other);
        }

        public bool IsPhysical()
        {
            return _tag.IsPhysical();
        }

        public bool IsValid()
        {
            return _tag.IsValid();
        }

        public bool IsFiltered()
        {
            return _tag.IsFiltered();
        }

        public DocumentRange GetNavigationRange()
        {
            return _tag.GetNavigationRange();
        }

        public TreeOffset GetTreeStartOffset()
        {
            return _tag.GetTreeStartOffset();
        }

        public int GetTextLength()
        {
            return _tag.GetTextLength();
        }

        public StringBuilder GetText(StringBuilder to)
        {
            return _tag.GetText(to);
        }

        public IBuffer GetTextAsBuffer()
        {
            return _tag.GetTextAsBuffer();
        }

        public string GetText()
        {
            return _tag.GetText();
        }

        public ITreeNode FindNodeAt(TreeTextRange treeRange)
        {
            return _tag.FindNodeAt(treeRange);
        }

        public IReadOnlyCollection<ITreeNode> FindNodesAt(TreeOffset treeOffset)
        {
            return _tag.FindNodesAt(treeOffset);
        }

        public ITreeNode FindTokenAt(TreeOffset treeTextOffset)
        {
            return _tag.FindTokenAt(treeTextOffset);
        }

        public ITreeNode Parent => _tag.Parent;
        public ITreeNode FirstChild => _tag.Parent;
        public ITreeNode LastChild => _tag.LastChild;
        public ITreeNode NextSibling => _tag.NextSibling;
        public ITreeNode PrevSibling => _tag.PrevSibling;
        public NodeType NodeType => _tag.NodeType;
        public PsiLanguageType Language => _tag.Language; // todo: set to BSML?
        public NodeUserData UserData => _tag.UserData;
        public NodeUserData PersistentUserData => _tag.PersistentUserData;
        public XmlNode GetXMLDoc(bool inherit)
        {
            return null;
        }

        public TReturn AcceptVisitor<TContext, TReturn>(IXmlTreeVisitor<TContext, TReturn> visitor, TContext context)
        {
            return _tag.AcceptVisitor(visitor, context);
        }

        public XmlTokenTypes XmlTokenTypes => _tag.XmlTokenTypes;
        public IXmlTag GetTag(Predicate<IXmlTag> predicate)
        {
            return _tag.GetTag(predicate);
        }

        public TreeNodeEnumerable<T> GetTags<T>() where T : class, IXmlTag
        {
            return _tag.GetTags<T>();
        }

        public TreeNodeCollection<T> GetTags2<T>() where T : class, IXmlTag
        {
            return _tag.GetTags2<T>();
        }

        public IList<T> GetNestedTags<T>(string xpath) where T : class, IXmlTag
        {
            return _tag.GetNestedTags<T>(xpath);
        }

        public TXmlTag AddTagBefore<TXmlTag>(TXmlTag tag, IXmlTag anchor) where TXmlTag : IXmlTag
        {
            return _tag.AddTagBefore(tag, anchor);
        }

        public TXmlTag AddTagAfter<TXmlTag>(TXmlTag tag, IXmlTag anchor) where TXmlTag : IXmlTag
        {
            return _tag.AddTagAfter(tag, anchor);
        }

        public void RemoveTag(IXmlTag tag)
        {
            tag.RemoveTag(tag);
        }

        public TreeNodeCollection<IXmlTag> InnerTags => _tag.InnerTags;
        public TXmlAttribute AddAttributeBefore<TXmlAttribute>(TXmlAttribute attribute, IXmlAttribute anchor) where TXmlAttribute : IXmlAttribute
        {
            return _tag.AddAttributeBefore(attribute, anchor);
        }

        public TXmlAttribute AddAttributeAfter<TXmlAttribute>(TXmlAttribute attribute, IXmlAttribute anchor) where TXmlAttribute : IXmlAttribute
        {
            return _tag.AddAttributeAfter(attribute, anchor);
        }

        public void RemoveAttribute(IXmlAttribute attribute)
        {
            _tag.RemoveAttribute(attribute);
        }

        public IXmlTagHeader Header => _tag.Header;
        public IXmlTagFooter Footer => _tag.Footer;
        public bool IsEmptyTag => _tag.IsEmptyTag;
        public ITreeRange InnerXml => _tag.InnerXml;
        public TreeNodeCollection<IXmlToken> InnerTextTokens => _tag.InnerTextTokens;
        public string InnerText => _tag.InnerText;
        public string InnerValue => _tag.InnerValue;
        public IXmlTagHeader HeaderNode => _tag.HeaderNode;
        public IXmlTagFooter FooterNode => _tag.FooterNode;
        public void SetName(string name)
        {
            // idAttribute?.AttributeName = name;
        }

        public TreeTextRange GetNameRange()
        {
            return _tag?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;
        }

        public bool IsSynthetic()
        {
            // ?
            return false;
        }

        public IDeclaredElement DeclaredElement => _tagElement;
        public string DeclaredName => _tagElement.ShortName;
    }
}