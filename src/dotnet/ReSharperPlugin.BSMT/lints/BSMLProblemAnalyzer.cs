using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml.Tree;
using ReSharperPlugin.BSMT_Rider.bsml;
using ReSharperPlugin.BSMT_Rider.utils;

namespace ReSharperPlugin.BSMT_Rider.lints
{
    
    // Types mentioned in this attribute are used for performance optimizations
    [ElementProblemAnalyzer(
        typeof (IFieldDeclaration), typeof(IPropertyDeclaration),
        HighlightingTypes = new [] {typeof (BsmlListWrongTypeHighlighting)})]
    public class BsmlProblemAnalyzer : ElementProblemAnalyzer<ICSharpDeclaration>
    {
        private readonly BsmlFileManager _bsmlFileManager;
        private List<IDeclaredType>? _allowedListTypes;

        public BsmlProblemAnalyzer(BsmlFileManager bsmlFileManager, IPsiModule psiModule)
        {
            _bsmlFileManager = bsmlFileManager;
            
        }

        protected override void Run(ICSharpDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            _allowedListTypes ??= BsmlListWrongTypeHighlighting.AllowedListClrTypes
                .Select(t =>
                    TypeFactory.CreateTypeByCLRName(t, NullableAnnotation.Unknown, element.GetSourceFile()!.PsiModule))
                .ToList();
            
            
            // TODO: Move to BSMLList
            var attributes = element.GetChildrenInSubtrees<IAttribute>();

            var uiValueAttribute = attributes
                .FirstOrDefault(attribute => attribute?.Name.Reference.Resolve().DeclaredElement is IClass attributeClass && 
                                             Equals(attributeClass.GetClrName(), BSMLConstants.BsmlUIValueAttribute));
            
            var uiValueId = uiValueAttribute?.ConstructorArgumentExpressionsEnumerable.FirstOrDefault(expression =>
                expression is ILiteralExpression && expression.ConstantValue.Value is string);

            if (uiValueId?.ConstantValue.Value is not string uiValueIdStr)
                return;
            
            var bsmlClassData = _bsmlFileManager.GetAssociatedBsmlFile(element.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>()!, 1000);

            if (bsmlClassData?.AssociatedBsmlFile is null)
            {
                return;
            }
                
            var identifiedTags = _bsmlFileManager.ParseBsml(bsmlClassData.AssociatedBsmlFile)!.GetAttributeToNameMap();
            

            var (tag, _) = identifiedTags.FirstOrDefault(pair => BSMLConstants.StripGarbage(pair.Value) == uiValueIdStr);
            if (tag is null || BSMLConstants.BsmlCustomListTag !=
                tag.GetParentOfTypeRecursiveNotStupid<IXmlTag>()!.Header.Name.XmlName) return;


            
            // var addHighlight = !_allowedListTypes.Any(type => Equals(type, element.DeclaredElement.Type()));

            // I absolutely hate this solution and I hope I can replace it fast
            // So Jetbrains, `element.DeclaredElement.Type()` returns System.Collections.Generic.List`1[T -> System.Object] which is what I want, right? right?!
            // Comparing `element.DeclaredElement.Type()` and `_allowedListTypes`'s contents doesn't work? Why? WHO KNOWS, JETBRAINS NONSENSE
            // Heaven forbid, comparing ClrName is also not possible? Why?
            // `element.DeclaredElement.Type().GetScalarType().GetClrName()` ALWAYS returns "System.Collections.Generic.List`1" which omits the generic type,
            // which I so desperately want.
            // Why. Why. Why.
            var addHighlight = !element.DeclaredElement.Type()!.ToString()
                .Equals("System.Collections.Generic.List`1[T -> System.Object]");
            
            if (addHighlight)
            {
                consumer.AddHighlighting(new BsmlListWrongTypeHighlighting(element, _bsmlFileManager, (ILiteralExpression) uiValueId));
            }
        }
    }
}