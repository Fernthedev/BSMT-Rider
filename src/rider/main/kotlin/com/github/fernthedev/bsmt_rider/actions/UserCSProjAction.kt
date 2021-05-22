package com.github.fernthedev.bsmt_rider.actions

import com.github.fernthedev.bsmt_rider.BeatSaberGenerator
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.application.ReadAction
import com.intellij.openapi.progress.runBackgroundableTask
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rider.projectView.hasSolution
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.findProjects


class UserCSProjAction : AnAction() {
    companion object {
        var findProjects: List<ProjectModelEntity>? = null
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.getData(CommonDataKeys.PROJECT)

        if (project != null && e.presentation.isEnabledAndVisible && findProjects != null) {
            runBackgroundableTask("Create user.csproj", project) {
                BeatSaberGenerator.locateFoldersAndGenerate(WorkspaceModel.getInstance(project).findProjects())
            }
        }
    }

    override fun update(e: AnActionEvent) {
        val project = e.getData(CommonDataKeys.PROJECT)


        findProjects = if (project != null) {
            WorkspaceModel.getInstance(project).findProjects()
        } else {
            null
        }

        e.presentation.isEnabledAndVisible = project?.hasSolution == true

        var enabled = e.presentation.isVisible

        // Avoid locking the UI thread
        ReadAction.nonBlocking {
            enabled = enabled &&
                    findProjects != null && BeatSaberGenerator.locateFolders(findProjects)
                .any { BeatSaberGenerator.isBeatSaberProject(it.csprojFile) }
        }.finishOnUiThread(ModalityState.NON_MODAL) {
            e.presentation.isEnabled = enabled
        }

    }
}