package com.github.fernthedev.bsmt_rider.actions

import com.github.fernthedev.bsmt_rider.BeatSaberGenerator
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.jetbrains.rider.model.projectModelView
import com.jetbrains.rider.projectView.hasSolution
import com.jetbrains.rider.projectView.solution


class UserCSProjAction : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.getData(CommonDataKeys.PROJECT)

        if (project != null && e.presentation.isEnabledAndVisible) {
            val solution = project.solution
            BeatSaberGenerator.locateFoldersAndGenerate(solution.projectModelView.items)
        }
    }

    override fun update(e: AnActionEvent) {
        val project = e.getData(CommonDataKeys.PROJECT)

        e.presentation.isEnabledAndVisible = project?.hasSolution == true
        e.presentation.isEnabled = e.presentation.isVisible &&
                BeatSaberGenerator.locateFolders(project?.solution?.projectModelView?.items)
                    .any { BeatSaberGenerator.isBeatSaberProject(it.csprojFile) }
    }
}