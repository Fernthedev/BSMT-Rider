package com.github.fernthedev.bsmt_rider.helpers

import com.ctc.wstx.stax.WstxInputFactory
import com.ctc.wstx.stax.WstxOutputFactory
import com.fasterxml.jackson.dataformat.xml.XmlFactory
import com.fasterxml.jackson.dataformat.xml.XmlMapper
import com.fasterxml.jackson.dataformat.xml.util.DefaultXmlPrettyPrinter
import com.github.fernthedev.bsmt_rider.BeatSaberFolders
import com.intellij.openapi.application.invokeAndWaitIfNeeded
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.util.application
import com.jetbrains.rider.model.ReloadCommand
import com.jetbrains.rider.model.UnloadCommand
import com.jetbrains.rider.model.projectModelTasks
import com.jetbrains.rider.projectView.ProjectModelViewUpdaterAsync
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.util.idea.runUnderProgress
import java.io.File

object ProjectUtils {
    // with Jackson 2.10 and later
    val xmlParser: XmlMapper =
        XmlMapper.builder(XmlFactory(WstxInputFactory(), WstxOutputFactory())) // possible configuration changes
            .defaultPrettyPrinter(DefaultXmlPrettyPrinter())
            .build()


    fun refreshProjectWithFiles(folders: List<BeatSaberFolders>, filesToRefresh: List<File>, project: Project?) {
        if (filesToRefresh.isNotEmpty() && project != null) {

            // TODO: Make this only run if a `csproj.user` file has been created or modified, not all the time
            // We do this to force ordering
            // In other words, force it to refresh AFTER writing is done
            // I'm not proud of this ugly code nesting at all nor hard coding this
            runWriteActionSafely {
                val projects = folders.map { it.project }

                refreshProjectManually(project, projects, filesToRefresh)
            }
        }
    }

    /// We have to hard code UnloadProjectAction.execute it seems unfortunately
    /// This is ugly, maybe find a way to get a component that references the
    // csproj directly so we can use DataContexts?
    fun refreshProjectManually(project: Project, projects: List<ProjectModelEntity>, filesToRefresh: List<File>) {

        if (projects.isEmpty())
            return

//        ProjectModelViewUpdater.fireUpdate(project) {
//            it.updateAll()
//        }
        ProjectModelViewUpdaterAsync.getInstance(project).requestUpdatePresentation()

        val projectIds = projects.mapNotNull { it.getId(project) }


        // run on dispatch thread
        invokeAndWaitIfNeeded {
            runWriteActionSafely {
                application.saveAll()
            }
        }

        val command =
            UnloadCommand(projectIds.toList())

        project.solution.projectModelTasks.unloadProjects.runUnderProgress(
            command,
            project,
            "Unload ${projects.size} projects...",
            isCancelable = false,
            throwFault = false
        )


//        ReloadProjectAction.execute() is the original code

        // We do the same here sadly
        val command2 =
            ReloadCommand(projectIds.toList(), withDependencies = true, onlyUnloaded = false);

        project.solution.projectModelTasks.reloadProjects.runUnderProgress(
            command2,
            project,
            "Reload ${projects.size} projects...",
            isCancelable = false,
            throwFault = false,
        )


        VfsUtil.markDirtyAndRefresh(true, false, false, *filesToRefresh.toTypedArray())
    }
}