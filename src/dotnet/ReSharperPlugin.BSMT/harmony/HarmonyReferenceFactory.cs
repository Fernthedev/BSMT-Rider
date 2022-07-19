using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using NuGet;

namespace ReSharperPlugin.BSMT_Rider.harmony
{
    public class HarmonyReferenceFactory : IReferenceFactory
    {
        /*
         * namespace QMerge.Hooking
            {
              [AttributeUsage(AttributeTargets.Class)]
              public class Hook : Attribute
              {
                public Type type;
                public string methodName;

                public Hook(Type type, string methodName)
                {
                  this.type = type;
                  this.methodName = methodName;
                }
         */
        public static readonly ClrTypeName QMergeHook = new("QMerge.Hooking.Hook");
        public static readonly ClrTypeName HarmonyPatch = new("HarmonyLib.HarmonyPatch");
        
        public static readonly ISet<ClrTypeName> SupportedAttributes = new HashSet<ClrTypeName>
        {
            QMergeHook,
            HarmonyPatch,
            // TODO: AFFINITY
        };

        private static readonly ClrTypeName SystemType = new(typeof(System.Type).FullName);

        // https://www.jetbrains.com/help/resharper/sdk/ReferenceProviders.html#creating-the-references

        private static IClrTypeName GetTargetPatch(IAttribute attribute, ITypeElement attributeClass)
        {
            // var type = attributeClass.Fields.First(e =>
            //     e.Type is IDeclaredType declaredType && Equals(declaredType.GetClrName(), SystemType)).Type;
            //
            //
            //
            // return ((IDeclaredType)type).GetClrName();
            if (Equals(attributeClass.GetClrName(), QMergeHook))
            {
                return (ClrTypeName) attribute.ConstructorArgumentExpressions.First(e => e.Type() is IDeclaredType declaredType && Equals(declaredType.GetClrName(), SystemType));
            }
            
            return null;
        }

        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            // Check it's a string literal, and the text of the
            // string literal is in the collection of names
            if (element is not ICSharpExpression literal || literal.ConstantValue.StringValue is not { } value)
                return ReferenceCollection.Empty;
            
            var attribute = AttributeNavigator.GetByConstructorArgumentExpression(literal);

            if (attribute?.Name.Reference.Resolve().DeclaredElement is not IClass attributeClass)
                return ReferenceCollection.Empty;
            
            if (!SupportedAttributes.Any(a => Equals(a, attributeClass.GetClrName()))) 
                return ReferenceCollection.Empty;
            
            var container = ClassDeclarationNavigator.GetByAttribute(attribute) as ICSharpDeclaration ??
                            MethodDeclarationNavigator.GetByAttribute(attribute) as ICSharpDeclaration;

            if (container is null)
                return ReferenceCollection.Empty;

            var target = GetTargetPatch(attribute, attributeClass);

            // TODO: Find based on hook type

            return new ReferenceCollection();
            // return new ReferenceCollection(new HarmonyPatchReference(target, literal.));

        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            // Check it's a string literal, and the text of the
            // string literal is in the collection of names
            if (element is not ICSharpExpression literal || literal.ConstantValue.StringValue is not { } value)
                return false;
            
            var attribute = AttributeNavigator.GetByConstructorArgumentExpression(literal);

            if (attribute?.Name.Reference.Resolve().DeclaredElement is not IClass attributeClass) return false;

            return SupportedAttributes.Any(a => Equals(a, attributeClass.GetClrName())) && names.Contains(value);
        }
    }
}