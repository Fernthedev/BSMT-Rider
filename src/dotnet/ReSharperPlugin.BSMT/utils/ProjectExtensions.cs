using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Paths;
using JetBrains.Util;

namespace ReSharperPlugin.BSMT_Rider.utils
{
    public static class ProjectExtensions
    {
        [CanBeNull]
        public static IEnumerable<IPsiSourceFile> GetPsiSourceFilesInProject(
            [CanBeNull] this IProject project,
            VirtualFileSystemPath resourceFilePath)
        {
            if (project == null)
                return null;

            var items = project.FindProjectItemsByLocation(resourceFilePath);

            var projectItems = items.ToList();

            var itemsCast = projectItems.Select(item =>
                item is IProjectFile projectItemByLocation
                    ? projectItemByLocation.ToSourceFile()
                    : project.GetSolution().GetComponent<IVirtualPathsService>()
                        .GetPsiSourceFileByVirtualPath(project, resourceFilePath));

            return itemsCast;
        }
    }
}