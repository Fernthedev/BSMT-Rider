package com.github.fernthedev.bsmt_rider.actions

import com.github.fernthedev.bsmt_rider.helpers.BeatSaberReferenceManager
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.components.service
import com.intellij.platform.ide.progress.withBackgroundProgress
import kotlinx.coroutines.launch


class BeatSaberReferenceGeneratorAction : BeatSaberProjectAction() {


    override fun actionPerformed(e: AnActionEvent) {
        val project = e.getData(CommonDataKeys.PROJECT)

        if (project == null || !e.presentation.isEnabledAndVisible || findProjects == null) return

        val referenceManager = project.service<BeatSaberReferenceManager>()

        referenceManager.scope.launch {
            withBackgroundProgress(project,  "Adding to Beat Saber References", cancellable = true) {
                referenceManager.askToAddReferences()
            }
        }
    }


}