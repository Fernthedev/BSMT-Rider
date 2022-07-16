package com.github.fernthedev.bsmt_rider.dialogue

import com.github.fernthedev.bsmt_rider.xml.ReferenceXML
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.ui.SearchTextField
import com.intellij.ui.components.JBCheckBox
import com.intellij.ui.components.JBScrollPane
import com.intellij.ui.table.JBTable
import com.intellij.util.PathUtil
import com.intellij.util.ui.FormBuilder
import com.intellij.util.ui.UI
import java.awt.Dimension
import java.io.File
import java.util.*
import javax.swing.JComponent
import javax.swing.table.AbstractTableModel


/**
TODO: [com.jetbrains.rider.projectView.actions.references.dialog.AddReferenceDialog] class
/ $(Beat_Saber_Path)\Beat Saber_Data\Managed
*/
@OptIn(ExperimentalStdlibApi::class)
class BeatSaberReferencesDialogue(project: Project?, beatSaberPath: Array<String>, existingReferences: List<ReferenceXML>) : DialogWrapper(project) {
    private val _foundBeatSaberReferences: List<BeatSaberReferencePair>

    val references = ArrayList<File>()

    private val _searchBox = SearchTextField(false)
    private val _parentDirectoryCheckbox = JBCheckBox("Show parent directory", true)
    private val _beatSaberReferences: JBTable
    private val _beatSaberReferencesScrollPane: JBScrollPane

    init {
        val beatSaberFolders = beatSaberPath.map { File(it) }.filter { it.exists() && it.isDirectory }

        if (beatSaberFolders.isEmpty())
            throw IllegalArgumentException("Beat saber folders are empty or not found!")

        val existingReferencesMatch = existingReferences.map {
                ref -> PathUtil.getFileName(ref.stringHintPath)
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
            // Remove duplicates
        }.distinct().map {
            BeatSaberReferencePair(false, it)
        }


        title = "Beat Saber Reference Manager"

        _beatSaberReferences = JBTable(BeatSaberReferenceTable(_foundBeatSaberReferences, _parentDirectoryCheckbox.isSelected))
        _beatSaberReferences.setShowColumns(true)

        _beatSaberReferencesScrollPane = JBScrollPane(_beatSaberReferences)

        _searchBox.textEditor.addCaretListener {
            val beatSaberReferenceTable = _beatSaberReferences.model as BeatSaberReferenceTable
            beatSaberReferenceTable.rows = _foundBeatSaberReferences.filter { it.file.path.lowercase().contains(_searchBox.text.lowercase()) }
            beatSaberReferenceTable.fireTableDataChanged()
        }

        _parentDirectoryCheckbox.addChangeListener {
            val beatSaberReferenceTable = _beatSaberReferences.model as BeatSaberReferenceTable
            beatSaberReferenceTable.showParentFolder = _parentDirectoryCheckbox.isSelected
            beatSaberReferenceTable.fireTableDataChanged()
        }

        init()
    }


    override fun createCenterPanel(): JComponent {
        // This is not a self assignment, they are different methods!
//        _beatSaberReferencesScrollPane.preferredSize = _beatSaberReferences.preferredSize
        _beatSaberReferences.autoResizeMode = JBTable.AUTO_RESIZE_SUBSEQUENT_COLUMNS

        _beatSaberReferences.columnModel.getColumn(0).maxWidth = UI.scale(100)
        _beatSaberReferences.fillsViewportHeight = true


//        _beatSaberReferences.preferredScrollableViewportSize = _beatSaberReferences.preferredScrollableViewportSize

        val panel = FormBuilder.createFormBuilder()
            .addComponent(_searchBox)
            .addComponent(_parentDirectoryCheckbox)
            .addLabeledComponentFillVertically("BeatSaber references", _beatSaberReferencesScrollPane)
//            .addComponentFillVertically(JPanel(GridLayout()), 0)
            .panel


        panel.preferredSize = Dimension(UI.scale(340), UI.scale(350))

        return panel
    }

    override fun getPreferredFocusedComponent(): JComponent? {
        return _searchBox
    }

    override fun doOKAction() {
        val model = _beatSaberReferences.model

        for  (i in 0 until model.rowCount) {

            val pair = model.getValueAt(i, -1) as BeatSaberReferencePair

            if (pair.included) {
                references.add(pair.file)
            }
        }

        super.doOKAction()
    }
}

private enum class ColumnEnum(
    val strName: String,
    val clazz: Class<*>,
    val editable: Boolean = false,
) {
    INCLUDE("Include", Boolean::class.javaObjectType, true),
    REFERENCE("Reference", String::class.java)
}

data class BeatSaberReferencePair(
    var included: Boolean = false,
    var file: File,
)


class BeatSaberReferenceTable(files: List<BeatSaberReferencePair>, var showParentFolder: Boolean) : AbstractTableModel() {
    private val columns: Array<ColumnEnum> = arrayOf(ColumnEnum.INCLUDE, ColumnEnum.REFERENCE)

    var rows: List<BeatSaberReferencePair> = ArrayList(files)
        set(value) {
            field = ArrayList(value)
        }

    /**
     * Returns a default name for the column using spreadsheet conventions:
     * A, B, C, ... Z, AA, AB, etc.  If `column` cannot be found,
     * returns an empty string.
     *
     * @param column  the column being queried
     * @return a string containing the default name of `column`
     */
    override fun getColumnName(column: Int): String {
        return columns[column].strName
    }

    /**
     * Returns `Object.class` regardless of `columnIndex`.
     *
     * @param columnIndex  the column being queried
     * @return the Object.class
     */
    override fun getColumnClass(columnIndex: Int): Class<*> {
        return columns[columnIndex].clazz
    }

    /**
     * Returns false.  This is the default implementation for all cells.
     *
     * @param  rowIndex  the row being queried
     * @param  columnIndex the column being queried
     * @return false
     */
    override fun isCellEditable(rowIndex: Int, columnIndex: Int): Boolean {
        return columns[columnIndex].editable
    }

    /**
     * Returns the number of rows in the model. A
     * `JTable` uses this method to determine how many rows it
     * should display.  This method should be quick, as it
     * is called frequently during rendering.
     *
     * @return the number of rows in the model
     * @see .getColumnCount
     */
    override fun getRowCount(): Int {
        return rows.size
    }

    /**
     * Returns the number of columns in the model. A
     * `JTable` uses this method to determine how many columns it
     * should create and display by default.
     *
     * @return the number of columns in the model
     * @see .getRowCount
     */
    override fun getColumnCount(): Int {
        return columns.size
    }

    /**
     * Returns the value for the cell at `columnIndex` and
     * `rowIndex`.
     *
     * @param   rowIndex        the row whose value is to be queried
     * @param   columnIndex     the column whose value is to be queried
     * @return  the value Object at the specified cell
     */
    override fun getValueAt(rowIndex: Int, columnIndex: Int): Any? {
        val pair = rows[rowIndex]

        return when (columnIndex) {
            // We use this ourselves
            -1 -> pair
            0 -> pair.included
            1 ->
                if(showParentFolder) "${pair.file.parentFile.name}/${pair.file.nameWithoutExtension}"
                else pair.file.nameWithoutExtension
            else -> null
        }
    }

    /**
     * This empty implementation is provided so users don't have to implement
     * this method if their data model is not editable.
     *
     * @param  aValue   value to assign to cell
     * @param  rowIndex   row of cell
     * @param  columnIndex  column of cell
     */
    override fun setValueAt(aValue: Any?, rowIndex: Int, columnIndex: Int) {

        val pair = rows[rowIndex]

        when (columnIndex) {
            0 -> pair.included = aValue as Boolean
        }
    }



}