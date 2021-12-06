using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.BSMT_Rider.bsml;

namespace ReSharperPlugin.BSMT_Rider.lints
{
    [RegisterConfigurableSeverity(
        BsmlListWrongTypeHighlighting.SeverityId,
        CompoundItemName: null,
        Group: HighlightingGroupIds.CodeSmell,
        Title: BsmlListWrongTypeHighlighting.Message,
        Description: BsmlListWrongTypeHighlighting.Description,
        DefaultSeverity: Severity.WARNING)]
    [ConfigurableSeverityHighlighting(
        SeverityId,
        CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.ERROR,
        OverloadResolvePriority = 0,
        ToolTipFormatString = Message)]
    public class BsmlListWrongTypeHighlighting : IHighlighting
    {
        public const string SeverityId = "BSMT-Rider/BSML"; // Appears in suppression comments
        public const string Message = "BSML refers to this field as UIValue for a custom-list. Consider using one of these list types instead: {List<object>}";
        public const string Description = "Wrong list type for custom-list tag.";
        
        // https://github.com/monkeymanboy/BeatSaberMarkupLanguage/blob/cb533a1955d39bba50049e18c546dc28601698ea/BeatSaberMarkupLanguage/TypeHandlers/CustomCellListTableDataHandler.cs#L76
        public static readonly List<Type> AllowedListTypes =
            new()
            {
                typeof(List<object>)
            };

        public static readonly List<ClrTypeName> AllowedListClrTypes =
            AllowedListTypes.Select(s => new ClrTypeName(s.FullName!)).ToList();
            // new List<string>
            // {
            //     $"{typeof(List<object>).FullName}",
            // }.Select(s => new ClrTypeName(s));
        


        private readonly BsmlFileManager _bsmlFileManager;
        private readonly ILiteralExpression _literalExpression;
        
        public BsmlListWrongTypeHighlighting(ICSharpDeclaration declaration, BsmlFileManager bsmlFileManager, ILiteralExpression literalExpression)
        {
            Declaration = declaration;
            _bsmlFileManager = bsmlFileManager;
            _literalExpression = literalExpression;
        }

        public ICSharpDeclaration Declaration { get; }

        public bool IsValid()
        {
            return Declaration.IsValid();
            // if (!Declaration.IsValid())
            //     return true;
            //
            // if (_literalExpression.ConstantValue.Value is not string uiValueIdStr)
            //     return true; // TODO: Should do? 
            //
            // //
            // //
            // // var attributes = Declaration.GetChildrenInSubtreesUnrecursive<IAttribute>();
            // //
            // // var uiValueAttribute = attributes
            // //     .FirstOrDefault(attribute => attribute?.Name.Reference.Resolve().DeclaredElement is IClass attributeClass && 
            // //                                  Equals(attributeClass.GetClrName(), BSMLConstants.BsmlUIValueAttribute));
            // //
            // //
            // // var uiValueId = uiValueAttribute?.ConstructorArgumentExpressionsEnumerable.FirstOrDefault(expression =>
            // //     expression is ILiteralExpression && expression.ConstantValue.Value is string);
            // //
            // // if (uiValueId?.ConstantValue.Value is not string uiValueIdStr)
            // //     return true; // TODO: Should do? 
            //
            // var bsmlClassData = _bsmlFileManager.GetAssociatedBsmlFile(Declaration.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>()!);
            //
            // if (bsmlClassData?.AssociatedBsmlFile is null)
            // {
            //     return true;
            // }
            //     
            // var identifiedTags = _bsmlFileManager.ParseBsml(bsmlClassData.AssociatedBsmlFile)!.GetAttributeToNameMap();
            //
            //
            // var (tag, _) = identifiedTags.FirstOrDefault(pair => BSMLConstants.StripGarbage(pair.Value) == uiValueIdStr);
            // if (tag is not null && BSMLConstants.BsmlCustomListTag == tag.GetParentOfTypeRecursiveNotStupid<IXmlTag>()!.Header.Name.XmlName)
            // {
            //     return Declaration.DeclaredElement is ITypeOwner declaredElementClazz &&
            //            declaredElementClazz.Type.GetScalarType()?.Resolve().DeclaredElement is ITypeElement clazz &&
            //            AllowedListClrTypes.Any(type => Equals(type, clazz.GetClrName()));
            // }
            //
            // return true;
        }

        public DocumentRange CalculateRange()
        {
            return Declaration.NameIdentifier?.GetHighlightingRange() ?? DocumentRange.InvalidRange;
        }

        public string ToolTip => Message;
        
        public string ErrorStripeToolTip => $"BSML refers to '{Declaration.DeclaredName}' as UIValue for a custom-list. Consider using one of these list types instead: {{List<object>}}";
    }
}