package com.github.fernthedev.bsmt_rider.settings

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.fileChooser.FileChooser
import com.intellij.openapi.fileChooser.FileChooserDescriptorFactory
import com.intellij.ui.AnActionButton
import com.intellij.ui.CollectionListModel
import com.intellij.ui.ToolbarDecorator
import com.intellij.ui.components.JBCheckBox
import com.intellij.ui.components.JBLabel
import com.intellij.ui.components.JBList
import com.intellij.ui.components.JBTextField
import com.intellij.util.ui.FormBuilder
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.ListModel

fun <E> ListModel<E>.toList() : List<E> {
    if (this.size == 0)
        emptyList<E>()

    val list = mutableListOf<E>()

    try {
        for (i in 0 until this.size) {
            list[i] = this.getElementAt(i)
        }
    } catch (_: IndexOutOfBoundsException) {
        // Why is this thrown if size is being checked?
    }

    return list
}

/**
 * Supports creating and managing a [JPanel] for the Settings Dialog.
 */
class AppSettingsComponent {
    val panel: JPanel
    private val _beatSaberFolders = JBList<String>(CollectionListModel())
    private val _beatSaberFoldersToolbar = ToolbarDecorator.createDecorator(_beatSaberFolders)

    private val _useDefaultFolder = JBCheckBox("Use a default beat saber directory?")
    private var _defaultFolder = JBTextField()

    val preferredFocusedComponent: JComponent
        get() = _beatSaberFolders

    var beatSaberFolders: Array<String>
        get() = (_beatSaberFolders.model as CollectionListModel<String>).items.toTypedArray()
        set(newText) {
            _beatSaberFolders.model = CollectionListModel (*newText)
        }

    var useDefaultFolder: Boolean
        get() = _useDefaultFolder.isSelected
        set(newStatus) {
            _useDefaultFolder.isSelected = newStatus
            _defaultFolder.isEnabled = newStatus
        }

    var defaultBeatSaberFolder: String?
        get() = if (_defaultFolder.text.isNullOrEmpty()) {
            null
        } else _defaultFolder.text
        set(newText) {
            _defaultFolder.text = newText
        }

    init {
        _beatSaberFolders.model = CollectionListModel()

        _beatSaberFoldersToolbar.setAddAction {
            FileChooser.chooseFile(FileChooserDescriptorFactory.createSingleFolderDescriptor(), null, null) {
                // TODO: Validate it's a beat saber folder using protocol

                // Theoretically this should never crash
                (_beatSaberFolders.model as CollectionListModel<String>).add(it.path)

            }

        }
        _beatSaberFoldersToolbar.setRemoveAction {
            (_beatSaberFolders.model as CollectionListModel<String>).remove(_beatSaberFolders.selectedIndex)
        }

        val selectDefault = object: AnActionButton() {
            override fun actionPerformed(p0: AnActionEvent) {
                _defaultFolder.text = _beatSaberFolders.selectedValue
            }
        }



        selectDefault.templatePresentation.text = "Set as default beat saber directory"
        selectDefault.isEnabled = useDefaultFolder
        selectDefault.templatePresentation.icon = AllIcons.Actions.Checked_selected

        _useDefaultFolder.addActionListener {
            selectDefault.isEnabled = useDefaultFolder
        }

        _useDefaultFolder.addChangeListener {
            selectDefault.isEnabled = useDefaultFolder
        }

        _beatSaberFoldersToolbar.addExtraAction(selectDefault)
        _beatSaberFoldersToolbar.disableUpDownActions()


        _defaultFolder.isEditable = false



        panel = FormBuilder.createFormBuilder()
            .addLabeledComponent(JBLabel("Beat Saber game directories: "), _beatSaberFoldersToolbar.createPanel(), 1, true)
            .addComponent(_useDefaultFolder, 1)
            .addLabeledComponent(JBLabel("Default Beat Saber Directory:"), _defaultFolder, 1, false)
            .addComponentFillVertically(JPanel(), 0)
            .panel
    }
}
