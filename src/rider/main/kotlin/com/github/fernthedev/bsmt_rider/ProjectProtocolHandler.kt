package com.github.fernthedev.bsmt_rider

import com.github.fernthedev.bsmt_rider.settings.AppSettingsState
import com.intellij.openapi.progress.runBackgroundableTask
import com.intellij.openapi.project.Project
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rd.ide.model.ConfigSettings
import com.jetbrains.rd.ide.model.bSMT_RiderModel
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.findProjects


class ProjectProtocolHandler(project: Project) : ProtocolSubscribedProjectComponent(project) {
    private val model = project.solution.bSMT_RiderModel

    init {
        model.getUserSettings.set { _ ->
            val instance = AppSettingsState.instance
            ConfigSettings(
                isDefaultBeatSaberLocation = instance.useDefaultFolder,
                defaultBeatSaberLocation = instance.defaultFolder,
                configuredBeatSaberLocations = instance.beatSaberFolders
            )
        }

        project.solution.isLoaded.whenTrue(projectComponentLifetime) {
            runBackgroundableTask("Create user.csproj", project) {
                BeatSaberGenerator.locateFoldersAndGenerate(WorkspaceModel.getInstance(project).findProjects(), project)
            }
            model.foundBeatSaberLocations.start(projectComponentLifetime, null).result.adviseOnce(projectComponentLifetime) {
                if (AppSettingsState.instance.beatSaberFolders.isEmpty()) {
                    AppSettingsState.instance.beatSaberFolders = it.unwrap()
                }
            }

//            model.foundBeatSaberLocations.runUnderProgress(null, project, "t", isCancelable = false, throwFault = false)
        }
    }
}