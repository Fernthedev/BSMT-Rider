package com.github.fernthedev.bsmt_rider.settings

import com.github.fernthedev.bsmt_rider.BeatSaberProjectManager
import com.intellij.openapi.project.Project

fun Project?.getBeatSaberSelectedDir(): String? {
    if (this != null) {
        val selected = BeatSaberProjectManager.getSelectedBeatSaberFolder(this)

        if (selected != null)
            return selected
    }

    return AppSettingsState.instance.getBeatSaberDir(this)
}