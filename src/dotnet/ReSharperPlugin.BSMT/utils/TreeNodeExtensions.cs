using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.PsiGen.Util;

namespace ReSharperPlugin.BSMT_Rider.utils
{
    public static class TreeNodeExtensions
    {
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