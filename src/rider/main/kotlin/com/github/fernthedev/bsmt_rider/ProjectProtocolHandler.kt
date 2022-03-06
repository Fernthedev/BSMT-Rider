package com.github.fernthedev.bsmt_rider

import com.github.fernthedev.bsmt_rider.settings.AppSettingsState
import com.intellij.openapi.progress.runBackgroundableTask
import com.intellij.openapi.project.Project
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.findProjects


class ProjectProtocolHandler(project: Project) : ProtocolSubscribedProjectComponent(project) {

    init {
        // Called when a project is open
        project.solution.isLoaded.whenTrue(projectComponentLifetime) {
            if (AppSettingsState.instance.refreshOnProjectOpen)
                runBackgroundableTask("Create user.csproj", project) {
                    BeatSaberGenerator.locateFoldersAndGenerate(WorkspaceModel.getInstance(project).findProjects(), project)
                }


//            model.foundBeatSaberLocations.runUnderProgress(null, project, "t", isCancelable = false, throwFault = false)
        }
    }
}