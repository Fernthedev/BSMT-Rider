package com.github.fernthedev.bsmt_rider.settings

import com.intellij.openapi.options.Configurable
import com.intellij.openapi.options.ConfigurationException
import org.jetbrains.annotations.Nls
import javax.swing.JComponent


/**
 * Provides controller functionality for application settings.
 */
class AppSettingsConfigurable : Configurable {
    private var mySettingsComponent: AppSettingsComponent? = null

    // A default constructor with no arguments is required because this implementation
    // is registered as an applicationConfigurable EP
    override fun getDisplayName(): @Nls(capitalization = Nls.Capitalization.Title) String {
        return "BSMT Rider: Settings"
    }

    override fun getPreferredFocusedComponent(): JComponent {
        return mySettingsComponent!!.preferredFocusedComponent
    }

    override fun createComponent(): JComponent {
        mySettingsComponent = AppSettingsComponent()
        return mySettingsComponent!!.panel
    }

    override fun isModified(): Boolean {
        val settings: AppSettingsState = AppSettingsState.instance


        var modified: Boolean = mySettingsComponent!!.beatSaberFolders.any { !settings.beatSaberFolders.contains(it) }
        modified = modified or (mySettingsComponent!!.useDefaultFolder != settings!!.useDefaultFolder)
        modified = modified or (mySettingsComponent!!.defaultBeatSaberFolder != settings.defaultFolder)
        return modified
    }

    override fun apply() {
        val settings: AppSettingsState = AppSettingsState.instance
        // TODO: Should this even be null?
        if (mySettingsComponent != null) {
            settings.beatSaberFolders = mySettingsComponent!!.beatSaberFolders
            settings.useDefaultFolder = mySettingsComponent!!.useDefaultFolder

            if (settings.useDefaultFolder && mySettingsComponent!!.defaultBeatSaberFolder == null)
                throw ConfigurationException("You must specify the default beat saber folder")

            settings.defaultFolder = mySettingsComponent!!.defaultBeatSaberFolder
        }
    }

    override fun reset() {
        val settings: AppSettingsState = AppSettingsState.instance
        /// TODO: Find beat saber folders from C# backend

        mySettingsComponent!!.beatSaberFolders = settings.beatSaberFolders
        mySettingsComponent!!.useDefaultFolder = settings.useDefaultFolder
        mySettingsComponent!!.defaultBeatSaberFolder = settings.defaultFolder
    }

    override fun disposeUIResources() {
        mySettingsComponent = null
    }
}