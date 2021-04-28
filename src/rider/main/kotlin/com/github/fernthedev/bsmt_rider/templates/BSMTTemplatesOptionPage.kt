package com.github.fernthedev.bsmt_rider.templates


import com.intellij.openapi.options.Configurable
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class BSMTTemplatesOptionPage : SimpleOptionsPage("BSMT", "BSMT_RiderTemplatesSettings"), Configurable.NoScroll {
    override fun getId(): String {
        return "RiderUnityFileTemplatesSettings"
    }
}


