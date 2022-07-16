package com.github.fernthedev.bsmt_rider.actions

import com.github.fernthedev.bsmt_rider.dialogue.HarmonyPatchDialog
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.application.ApplicationManager

class CreateHarmonyPatch : BeatSaberProjectAction() {


    override fun actionPerformed(e: AnActionEvent) {
        val project = e.getData(CommonDataKeys.PROJECT)

        if (project != null && e.presentation.isEnabledAndVisible && findProjects != null) {

            ApplicationManager.getApplication().invokeAndWait {
                val dialogue = HarmonyPatchDialog(e.getData(CommonDataKeys.PSI_FILE)!!, e.getData(CommonDataKeys.EDITOR)!!)

                if (dialogue.showAndGet()) {

                }
            }
        }
    }


}