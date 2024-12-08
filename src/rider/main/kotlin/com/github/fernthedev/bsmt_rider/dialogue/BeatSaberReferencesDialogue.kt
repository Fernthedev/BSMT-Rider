package com.github.fernthedev.bsmt_rider.dialogue

import com.github.fernthedev.bsmt_rider.xml.ReferenceXML
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.ui.CheckBoxList
import com.intellij.ui.SearchTextField
import com.intellij.ui.components.JBScrollPane
import com.intellij.util.PathUtil
import com.intellij.util.ui.FormBuilder
import com.intellij.util.ui.JBUI
import java.awt.Dimension
import java.io.File
import java.util.*
import javax.swing.JCheckBox
import javax.swing.JComponent


/// $(Beat_Saber_Path)\Beat Saber_Data\Managed
@OptIn(ExperimentalStdlibApi::class)
class BeatSaberReferencesDialogue(
    project: Project?,
    beatSaberPath: Array<String>,
    existingReferences: List<ReferenceXML>
) : DialogWrapper(project) {
    private val _foundBeatSaberReferences: List<BeatSaberReferencePair>

    val references = ArrayList<File>()

    private val _searchBox = SearchTextField(false)
    private val _beatSaberReferences: CheckBoxList<BeatSaberReferencePair>
    private val _beatSaberReferencesScrollPane: JBScrollPane

    init {
        val beatSaberFolders = beatSaberPath.map { File(it) }.filter { it.exists() && it.isDirectory }

        if (beatSaberFolders.isEmpty())
            throw IllegalArgumentException("Beat saber folders are empty or not found!")

        val existingReferencesMatch = existingReferences.map { ref ->
            ref.stringHintPath?.let { PathUtil.getFileName(it) }
        }



        _foundBeatSaberReferences = beatSaberFolders.map { folder ->
            folder.listFiles { it ->
                it.extension.lowercase(Locale.getDefault()) == "dll" &&
                        existingReferencesMatch.none { ref ->
                            ref == it.name
                        }
            }
            // Merge list
        }.reduce { acc, arrayOfFiles ->
            acc + arrayOfFiles
        }.map {
            BeatSaberReferencePair(false, it)
        }


        title = "Add Beat Saber Reference"

        _beatSaberReferences = CheckBoxList<BeatSaberReferencePair>()
        _beatSaberReferences.setItems(_foundBeatSaberReferences) { pair ->
            pair.file.nameWithoutExtension
        }
        _beatSaberReferences.setCheckBoxListListener { index, value ->
            _beatSaberReferences.getItemAt(index)!!.included = value
        }

        _beatSaberReferencesScrollPane = JBScrollPane(_beatSaberReferences)

        _searchBox.textEditor.addCaretListener {
            val searchText = _searchBox.text.lowercase()
            val filteredReferences = _foundBeatSaberReferences.filter { pair ->
                pair.file.nameWithoutExtension.lowercase().contains(searchText)
            }

            _beatSaberReferences.setItems(filteredReferences) { pair ->
                pair.file.nameWithoutExtension
            }

            refreshCheckboxes()
        }

        setOKButtonText("Add")
        refreshCheckboxes()
        init()
    }

    private fun refreshCheckboxes() {
        val model = _beatSaberReferences.model
        for (i in 0 until model.size) {
            val checkbox = model.getElementAt(i) as JCheckBox
            val pair = _beatSaberReferences.getItemAt(i)!!
            checkbox.isSelected = pair.included
            checkbox.toolTipText = pair.file.absolutePath
        }
    }

    override fun createCenterPanel(): JComponent {
        // This is not a self assignment, they are different methods!
//        _beatSaberReferencesScrollPane.preferredSize = _beatSaberReferences.preferredSize


//        _beatSaberReferences.preferredScrollableViewportSize = _beatSaberReferences.preferredScrollableViewportSize

        val panel = FormBuilder.createFormBuilder()
            .addComponent(_searchBox)
            .addComponentFillVertically(_beatSaberReferencesScrollPane, 8)
//            .addComponentFillVertically(JPanel(GridLayout()), 0)
            .panel


        panel.minimumSize = Dimension(JBUI.scale(400), JBUI.scale(400))
        panel.preferredSize = Dimension(JBUI.scale(400), JBUI.scale(400))

        return panel
    }

    override fun getPreferredFocusedComponent(): JComponent {
        return _searchBox
    }

    override fun doOKAction() {
        _foundBeatSaberReferences.forEach { pair ->
            if (pair.included) {
                references.add(pair.file)
            }
        }

        super.doOKAction()
    }
}

data class BeatSaberReferencePair(
    var included: Boolean = false,
    var file: File,
)
