package com.github.fernthedev.bsmt_rider.actions

import com.github.fernthedev.bsmt_rider.BeatSaberProjectManager
import com.github.fernthedev.bsmt_rider.helpers.BeatSaberUtils
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.application.ReadAction
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rider.projectView.hasSolution
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.findProjects

abstract class BeatSaberProjectAction : AnAction() {

    companion object {
        var findProjects: List<ProjectModelEntity>? = null
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
        ReadAction.nonBlocking<Unit> {
            enabled = enabled &&
                    !findProjects.isNullOrEmpty() && BeatSaberUtils.locateBeatSaberProjects(
                findProjects
            ).any { BeatSaberProjectManager.isBeatSaberProject(it.csprojFile) }
        }.finishOnUiThread(ModalityState.NON_MODAL) {
            e.presentation.isEnabled = enabled
        }
    }

}