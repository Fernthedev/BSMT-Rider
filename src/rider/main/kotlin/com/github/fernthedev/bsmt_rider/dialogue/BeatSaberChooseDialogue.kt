package com.github.fernthedev.bsmt_rider.dialogue

import com.github.fernthedev.bsmt_rider.settings.AppSettingsState
import com.intellij.icons.AllIcons
import com.intellij.openapi.fileChooser.FileChooser
import com.intellij.openapi.fileChooser.FileChooserDescriptorFactory
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.ComboBox
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.ui.components.fields.ExtendableTextComponent
import com.intellij.ui.components.fields.ExtendableTextField
import com.intellij.util.ui.FormBuilder
import java.awt.BorderLayout
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.JTextField
import javax.swing.plaf.basic.BasicComboBoxEditor


class BeatSaberChooseDialogue(val project: Project?) : DialogWrapper(project) {
    lateinit var beatSaberInput: ComboBox<String>

    init {
        init()
        title = "Choose Beat Saber Directory"
    }

    override fun createCenterPanel(): JComponent {
        beatSaberInput = ComboBox(AppSettingsState.instance.beatSaberFolders)

        val browseExtension = ExtendableTextComponent.Extension.create(
            AllIcons.General.OpenDisk, AllIcons.General.OpenDiskHover,
            "Open folder"
        ) {
            FileChooser.chooseFile(FileChooserDescriptorFactory.createSingleFolderDescriptor(), project, null) {
                beatSaberInput.selectedItem = it.canonicalPath
            }
        }

        beatSaberInput.editor = object : BasicComboBoxEditor() {
            override fun createEditorComponent(): JTextField {
                val ecbEditor = ExtendableTextField()
                ecbEditor.addExtension(browseExtension)
                ecbEditor.border = null
                return ecbEditor
            }
        }


        beatSaberInput.componentPopupMenu?.isVisible = AppSettingsState.instance.beatSaberFolders.isNotEmpty()
        beatSaberInput.isEditable = true

        return FormBuilder.createFormBuilder()
            .addLabeledComponent("Beat Saber directory", beatSaberInput, 1, true)
            .addComponentFillVertically(JPanel(BorderLayout()), 0)
            .panel
    }
}