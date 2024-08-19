package com.github.fernthedev.bsmt_rider.actions

import com.github.fernthedev.bsmt_rider.BeatSaberProjectManager
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.components.service
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.platform.ide.progress.withBackgroundProgress
import com.jetbrains.rider.projectView.workspace.findProjects
import kotlinx.coroutines.launch


class UserCSProjAction : BeatSaberProjectAction() {

    // Button to regenerate user.csproj
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.getData(CommonDataKeys.PROJECT)

        if (project == null || !e.presentation.isEnabledAndVisible || findProjects == null) return

        val beatSaberProjectManager = project.service<BeatSaberProjectManager>()

        beatSaberProjectManager.scope.launch {
            withBackgroundProgress(project,"Create user.csproj" ) {
                beatSaberProjectManager.locateFoldersAndGenerate(WorkspaceModel.getInstance(project).findProjects(), true)
            }

        }
    }

    override fun getActionUpdateThread(): ActionUpdateThread {
        return ActionUpdateThread.BGT
    }
}