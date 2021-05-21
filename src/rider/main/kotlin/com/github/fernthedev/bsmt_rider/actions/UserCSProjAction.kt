package com.github.fernthedev.bsmt_rider.actions

import com.github.fernthedev.bsmt_rider.BeatSaberGenerator
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
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
            BeatSaberGenerator.locateFoldersAndGenerate(findProjects)
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
        e.presentation.isEnabled = e.presentation.isVisible &&
                findProjects != null && BeatSaberGenerator.locateFolders(findProjects)
                    .any { BeatSaberGenerator.isBeatSaberProject(it.csprojFile) }
    }
}