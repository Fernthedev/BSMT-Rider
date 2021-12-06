using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.PsiGen.Util;

namespace ReSharperPlugin.BSMT_Rider.utils
{
    public static class TreeNodeExtensions
    {
        public static IEnumerable<ITreeNode> GetChildrenInSubtrees(
            this ITreeNode node)
        {
            if (node.FirstChild == null) yield break;
            
            ITreeNode? child;
            for (child = node.FirstChild; child != null; child = child.NextSibling)
            {
                yield return child;
                foreach (ITreeNode childrenInSubtree in child.GetChildrenInSubtrees())
                    yield return childrenInSubtree;
            }
        }

        public static IEnumerable<T> GetChildrenInSubtrees<T>(this ITreeNode node) where T : class, ITreeNode
        {
            if (node.FirstChild == null) yield break;
            ITreeNode? child;
            for (child = node.FirstChild; child != null; child = child.NextSibling)
            {
                if (child is T obj4)
                    yield return obj4;
                foreach (T childrenInSubtree in child.GetChildrenInSubtrees<T>())
                    yield return childrenInSubtree;
            }
        }

        public static T? GetParentOfType<T>(this ITreeNode node) where T : class, ITreeNode
        {
            for (; node != null; node = node.Parent)
            {
                if (node is T obj1)
                    return obj1;
            }
            return default;
        }
        
        public static T? GetParentOfTypeRecursiveNotStupid<T>(this ITreeNode node) where T : class
        {
            while (true)
            {
                if (node == null) break;

                if (node is T t)
                    return t;

                node = node.Parent;
            }

            return default;
        }
        
        public static IEnumerable<T> GetChildrenInSubtreesUnrecursive<T>(this ITreeNode node) where T : class, ITreeNode
        {
            if (node.FirstChild == null) yield break;
            
            ITreeNode? child;
            for (child = node.FirstChild; child != null; child = child.NextSibling)
            {
                if (child is T obj4)
                    yield return obj4;
            }
        }

        public static List<T> GetTypeInFile<T>(this ICSharpFile file, IEnumerable<ITreeNode>? nodes = null, List<T>? list = null) where T : class
        {
            list ??= new List<T>();

            nodes ??= file.Children().Where(e => e is INamespaceDeclaration or ITypeDeclaration);


            var treeNodes = nodes.ToList();
            foreach (var node in treeNodes)
            {
                GetTypeInFile(file, node.Children(), list);
            }

            list.AddAll(treeNodes.OfType<T>());

            return list;
        }
    }
}