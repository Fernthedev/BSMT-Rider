package com.github.fernthedev.bsmt_rider.settings

import com.github.fernthedev.bsmt_rider.dialogue.BeatSaberChooseDialogue
import com.intellij.openapi.application.EDT
import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.ServiceManager
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.project.Project
import com.intellij.util.xmlb.XmlSerializerUtil
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext


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
    var refreshOnProjectOpen: Boolean = true

    suspend fun getBeatSaberInstallationDir(project: Project?) : String? {
        if (useDefaultFolder && defaultFolder != null)
            return defaultFolder!!


        return withContext(Dispatchers.EDT) {
            val dialogue = BeatSaberChooseDialogue(project)

            if (!dialogue.showAndGet()) return@withContext null

            val beatSaberPath = dialogue.beatSaberInput.selectedItem as String

            val addToConfig = dialogue.addToConfigCheckbox.isEnabled && dialogue.addToConfigCheckbox.isSelected
            val makeDefault = dialogue.setAsDefault.isEnabled && dialogue.setAsDefault.isSelected

            addToList(beatSaberPath, addToConfig, makeDefault)

            if (beatSaberPath.isNotEmpty()) {
                return@withContext beatSaberPath
            }

            return@withContext null
        }
    }

    private fun addToList(path: String, addToConfig: Boolean, default: Boolean) {
        if (path.isEmpty())
            return

        if (addToConfig && !beatSaberFolders.any {path == it}) {
            val newList = beatSaberFolders.toMutableList()
            newList.add(path)
            beatSaberFolders = newList.toTypedArray()
        }

        if (default) {
            defaultFolder = path
            // Use default folder must be set to true
            useDefaultFolder = true
        }
    }

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