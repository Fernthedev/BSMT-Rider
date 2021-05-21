package com.github.fernthedev.bsmt_rider.settings

import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.ServiceManager
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.util.xmlb.XmlSerializerUtil


/**
 * Supports storing the application settings in a persistent way.
 * The [State] and [Storage] annotations define the name of the data and the file name where
 * these persistent application settings are stored.
 */
@State(name = "com.github.fernthedev.bsmt_rider.settings.AppSettingsState", storages = [Storage("BSMT_Rider_Settings.xml")])
class AppSettingsState : PersistentStateComponent<AppSettingsState> {
    var beatSaberFolders: Array<String> = emptyArray()
    var useDefaultFolder = false
    var defaultFolder: String? = null

    override fun getState(): AppSettingsState {
        return this
    }

    override fun loadState(state: AppSettingsState) {
        XmlSerializerUtil.copyBean(state, this)
    }

    companion object {
        val instance: AppSettingsState
            get() = ServiceManager.getService(AppSettingsState::class.java)
    }
}