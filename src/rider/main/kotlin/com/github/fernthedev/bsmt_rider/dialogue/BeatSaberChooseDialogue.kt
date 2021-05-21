package com.github.fernthedev.bsmt_rider.dialogue

import com.github.fernthedev.bsmt_rider.settings.AppSettingsState
import com.intellij.openapi.fileChooser.FileChooserDescriptorFactory
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.ComboBox
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.ui.ComboboxWithBrowseButton
import com.intellij.util.ui.FormBuilder
import java.awt.BorderLayout
import javax.swing.JComponent
import javax.swing.JPanel


class BeatSaberChooseDialogue(val project: Project?) : DialogWrapper(project) {
    lateinit var beatSaberInput: ComboBox<String>

    init {
        init()
        title = "Choose Beat Saber Directory"
    }

    override fun createCenterPanel(): JComponent {
        beatSaberInput = ComboBox(AppSettingsState.instance.beatSaberFolders)

        val comboBoxParent = ComboboxWithBrowseButton(beatSaberInput)
        comboBoxParent.componentPopupMenu?.isVisible = AppSettingsState.instance.beatSaberFolders.isNotEmpty()
        comboBoxParent.addBrowseFolderListener(project, FileChooserDescriptorFactory.createSingleFolderDescriptor())

        beatSaberInput.isEditable = true

        return FormBuilder.createFormBuilder()
            .addLabeledComponent("Beat Saber directory", comboBoxParent, 1, true)
            .addComponentFillVertically(JPanel(BorderLayout()), 0)
            .panel
    }
}