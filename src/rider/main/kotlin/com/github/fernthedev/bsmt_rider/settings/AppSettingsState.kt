package com.github.fernthedev.bsmt_rider.settings

import com.github.fernthedev.bsmt_rider.dialogue.BeatSaberChooseDialogue
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.ServiceManager
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.project.Project
import com.intellij.util.xmlb.XmlSerializerUtil
import java.util.concurrent.atomic.AtomicBoolean


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

    fun getBeatSaberDir(project: Project?) : String? {
        if (useDefaultFolder && defaultFolder != null)
            return defaultFolder!!


        val task = fun(): String? {
            val dialogue = BeatSaberChooseDialogue(project)

            if (dialogue.showAndGet()) {

                val str = dialogue.beatSaberInput.selectedItem as String

                if (str.isNotEmpty())
                    return str
            }

            return null
        }

        // If on UI thread, make dialogue
        if (ApplicationManager.getApplication().isDispatchThread)
            return task()
        // if not on ui thread, make dialogue in UI thread and wait
        else {
            var result: String? = null
            val called = AtomicBoolean(false)
            ApplicationManager.getApplication().invokeLater {
                result = task()
                called.set(true)
            }

            // Just yield since this will take a while
            // We want to avoid hoarding resources
            while (!called.get())
                Thread.yield()

            return result
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