package com.github.fernthedev.bsmt_rider.actions

import com.github.fernthedev.bsmt_rider.helpers.BeatSaberReferenceManager
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.progress.runBackgroundableTask


class BeatSaberReferenceGeneratorAction : BeatSaberProjectAction() {


    override fun actionPerformed(e: AnActionEvent) {
        val project = e.getData(CommonDataKeys.PROJECT)

        if (project != null && e.presentation.isEnabledAndVisible && findProjects != null) {
            runBackgroundableTask("Creating harmony patch", project) {
                BeatSaberReferenceManager.askToAddReferences(project)
            }
        }
    }


}