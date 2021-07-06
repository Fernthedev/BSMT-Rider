package com.github.fernthedev.bsmt_rider.settings

import com.github.fernthedev.bsmt_rider.BeatSaberGenerator
import com.intellij.openapi.project.Project

fun Project?.getBeatSaberSelectedDir(): String? {
    if (this != null) {
        val selected = BeatSaberGenerator.getSelectedBeatSaberFolder(this)

        if (selected != null)
            return selected
    }

    return AppSettingsState.instance.getBeatSaberDir(this)
}