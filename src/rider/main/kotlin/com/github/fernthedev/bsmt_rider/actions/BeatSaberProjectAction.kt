package com.github.fernthedev.bsmt_rider.actions

import com.github.fernthedev.bsmt_rider.BeatSaberProjectManager
import com.github.fernthedev.bsmt_rider.helpers.BeatSaberUtils
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.components.service
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.jetbrains.rider.projectView.hasSolution
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.findProjects
import kotlinx.coroutines.launch

abstract class BeatSaberProjectAction : AnAction() {

    companion object {
        var findProjects: List<ProjectModelEntity>? = null
    }

    override fun update(e: AnActionEvent) {
        val project = e.getData(CommonDataKeys.PROJECT)

        if (project == null) {
            e.presentation.isEnabledAndVisible = false
            return
        }

        e.presentation.isVisible = project.hasSolution == true
        e.presentation.isEnabled = project.solution.solutionLifecycle.fullStartupFinished.valueOrNull?.fullStartupTime != null

        findProjects = WorkspaceModel.getInstance(project).findProjects()
        val beatSaberProjectManager = project.service<BeatSaberProjectManager>()

        beatSaberProjectManager.scope.launch {
            var enabled =
                e.presentation.isVisible &&
                        !findProjects.isNullOrEmpty()

            if (enabled) {
                enabled = BeatSaberUtils.locateBeatSaberProjects(findProjects).any {
                    beatSaberProjectManager.isBeatSaberProject(it.csprojFile)
                }
            }

            e.presentation.isEnabled = enabled
        }
    }

    override fun getActionUpdateThread(): ActionUpdateThread {
        return ActionUpdateThread.BGT
    }

}