package com.github.fernthedev.bsmt_rider.dialogue

import com.github.fernthedev.bsmt_rider.settings.AppSettingsState
import com.intellij.icons.AllIcons
import com.intellij.openapi.fileChooser.FileChooser
import com.intellij.openapi.fileChooser.FileChooserDescriptorFactory
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.ComboBox
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.ui.components.CheckBox
import com.intellij.ui.components.fields.ExtendableTextComponent
import com.intellij.ui.components.fields.ExtendableTextField
import com.intellij.util.ui.FormBuilder
import java.awt.BorderLayout
import javax.swing.JCheckBox
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.JTextField
import javax.swing.plaf.basic.BasicComboBoxEditor


class BeatSaberChooseDialogue(val project: Project?) : DialogWrapper(project) {
    lateinit var beatSaberInput: ComboBox<String>
    lateinit var addToConfigCheckbox: JCheckBox
    lateinit var setAsDefault: JCheckBox

    init {
        init()
        title = "Choose Beat Saber Directory"
    }

    override fun createCenterPanel(): JComponent {
        val configFolders = AppSettingsState.instance.beatSaberFolders
        beatSaberInput = ComboBox(configFolders)

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

        setAsDefault = CheckBox("Set this beat saber folder as default")
        addToConfigCheckbox = CheckBox("Store this beat saber folder in config")

        // logic

        val checkIfConfigInputIsValid = fun(): Boolean {
            val inputValue = beatSaberInput.selectedItem as String?
            return !inputValue.isNullOrEmpty()
        }

        val checkIfConfigInputIsNew = fun(): Boolean {
            val inputValue = beatSaberInput.selectedItem as String?
            return checkIfConfigInputIsValid() && !configFolders.any { it == inputValue }
        }

        val updateCheckboxes = {
            val isNew = checkIfConfigInputIsNew()
            addToConfigCheckbox.isEnabled = isNew

            // Check if the path already exists
            // or if new path, if it will be added to config
            val isNewOrExisting = (isNew && addToConfigCheckbox.isSelected) || !isNew
            setAsDefault.isEnabled = isNewOrExisting && checkIfConfigInputIsValid()
        }

        beatSaberInput.addActionListener {
            updateCheckboxes()
        }

        addToConfigCheckbox.addChangeListener {
            updateCheckboxes()
        }

        updateCheckboxes()

        return FormBuilder.createFormBuilder()
            .addLabeledComponent("Beat Saber directory", beatSaberInput, 1, true)
            .addComponentFillVertically(JPanel(BorderLayout()), 0)
            .addComponent(addToConfigCheckbox)
            .addComponent(setAsDefault)
            .panel
    }
}