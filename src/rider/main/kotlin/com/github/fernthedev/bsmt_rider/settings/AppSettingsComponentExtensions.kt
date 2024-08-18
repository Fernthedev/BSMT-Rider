package com.github.fernthedev.bsmt_rider.settings

import com.github.fernthedev.bsmt_rider.BeatSaberProjectManager
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project

/**
 * Looks for the beat saber directory
 */
suspend fun Project?.getBeatSaberSelectedDir(): String? {
    if (this != null) {
        val selected = this.service<BeatSaberProjectManager>().selectedBeatSaberFolder

        if (selected != null) {
            return selected
        }
    }

    return AppSettingsState.instance.getBeatSaberInstallationDir(this)
}