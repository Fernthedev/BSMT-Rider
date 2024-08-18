package com.github.fernthedev.bsmt_rider

import com.github.fernthedev.bsmt_rider.settings.AppSettingsState
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.platform.ide.progress.withBackgroundProgress
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.findProjects
import kotlinx.coroutines.launch


class ProjectProtocolHandler(project: Project) : ProtocolSubscribedProjectComponent(project) {

    init {
        // Called when a project is open
        project.solution.isLoaded.whenTrue(projectComponentLifetime) {

            project.service<BeatSaberProjectManager>().scope.launch {
                withBackgroundProgress(project, "Create user.csproj", cancellable = true) {
                    project.service<BeatSaberProjectManager>().locateFoldersAndGenerate(
                        WorkspaceModel.getInstance(project).findProjects(),
                        AppSettingsState.instance.refreshOnProjectOpen
                    )
                }
            }


//            model.foundBeatSaberLocations.runUnderProgress(null, project, "t", isCancelable = false, throwFault = false)
        }
    }
}