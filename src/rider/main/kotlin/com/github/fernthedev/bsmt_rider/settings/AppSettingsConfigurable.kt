package com.github.fernthedev.bsmt_rider.settings

import com.intellij.openapi.options.Configurable
import com.intellij.openapi.options.ConfigurationException
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import org.jetbrains.annotations.Nls
import javax.swing.JComponent


/**
 * Provides controller functionality for application settings.
 */
class AppSettingsConfigurable : Configurable {
    private var lifetime: Lifetime? = LifetimeDefinition()
    private var mySettingsComponent: AppSettingsComponent? = null

    // A default constructor with no arguments is required because this implementation
    // is registered as an applicationConfigurable EP
    @Nls(capitalization = Nls.Capitalization.Title)
    override fun getDisplayName(): String {
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


        val uiFolders = mySettingsComponent!!.beatSaberFolders;

        var modified: Boolean = uiFolders.any { !settings.beatSaberFolders.contains(it) }
        modified = modified or settings.beatSaberFolders.any { !uiFolders.contains(it) }
        modified = modified or (mySettingsComponent!!.useDefaultFolder != settings.useDefaultFolder)
        modified = modified or (mySettingsComponent!!.defaultBeatSaberFolder != settings.defaultFolder)
        return modified
    }

    override fun apply() {
        val settings: AppSettingsState = AppSettingsState.instance
        // TODO: Should this even be null?
        if (mySettingsComponent != null) {
            val component = mySettingsComponent!!

            settings.beatSaberFolders = component.beatSaberFolders
            settings.useDefaultFolder = component.useDefaultFolder

            if (settings.useDefaultFolder && component.defaultBeatSaberFolder == null)
                throw ConfigurationException("You must specify the default beat saber folder")

            settings.defaultFolder = component.defaultBeatSaberFolder
        }
    }

    override fun reset() {
        val settings: AppSettingsState = AppSettingsState.instance
        /// TODO: Find beat saber folders from C# backend

        val component = mySettingsComponent!!

        component.beatSaberFolders = settings.beatSaberFolders
        component.useDefaultFolder = settings.useDefaultFolder
        component.defaultBeatSaberFolder = settings.defaultFolder


        // TODO: Find a way to make request to Resharper even though it only runs on project load
//        if (false && component.beatSaberFolders.isEmpty() && settings.beatSaberFolders.isEmpty()) {
//
//            RiderProtocolHandler.instance.bsmtRidermodel.foundBeatSaberLocations.start(lifetime!!, null)
//                .result.advise(lifetime!!) {
//                    if (settings.beatSaberFolders.isEmpty()) {
//                        settings.beatSaberFolders = it.unwrap()
//                    }
//                    component.beatSaberFolders = settings.beatSaberFolders
//                }
//
//
//        }
    }

    override fun disposeUIResources() {
        mySettingsComponent = null
        if (lifetime is LifetimeDefinition) {
            (lifetime as LifetimeDefinition).terminate()
        }
        lifetime = null
    }
}