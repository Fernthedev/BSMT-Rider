package com.github.fernthedev.bsmt_rider.actions

import com.github.fernthedev.bsmt_rider.BeatSaberProjectManager
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.progress.runBackgroundableTask
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.jetbrains.rider.projectView.workspace.findProjects


class UserCSProjAction : BeatSaberProjectAction() {

    // Button to regenerate user.csproj
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.getData(CommonDataKeys.PROJECT)

        if (project != null && e.presentation.isEnabledAndVisible && findProjects != null) {
            runBackgroundableTask("Create user.csproj", project) {
                BeatSaberProjectManager.locateFoldersAndGenerate(WorkspaceModel.getInstance(project).findProjects(), project, true)
            }
        }
    }
}