package com.github.fernthedev.bsmt_rider.dialogue

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.psi.PsiDocumentManager
import com.intellij.psi.PsiFile
import com.intellij.ui.EditorTextField
import com.intellij.ui.LanguageTextField
import com.intellij.ui.dsl.builder.panel
import com.jetbrains.rider.ideaInterop.fileTypes.csharp.CSharpFileType
import com.jetbrains.rider.ideaInterop.fileTypes.csharp.CSharpLanguage
import com.jetbrains.rider.ideaInterop.fileTypes.csharp.CSharpParserDefinition
import com.jetbrains.rider.ideaInterop.fileTypes.csharp.psi.impl.CSharpDummyNodeImpl
import com.jetbrains.rider.ideaInterop.fileTypes.csharp.psi.impl.CSharpElementTypes
import javax.swing.JComponent

class HarmonyPatchDialog(file: PsiFile, editor: Editor) : DialogWrapper(file.project) {

    private val harmonyClass: EditorTextField
    private val harmonyResultPreview: LanguageTextField

    init {
        val manager = file.manager
        val project = file.project



        val element = file.findElementAt(editor.caretModel.offset)

        val viewProvider = manager.findViewProvider(file.virtualFile)!!
        val targetClassFile = CSharpParserDefinition().createFile(viewProvider)
        targetClassFile.add(CSharpDummyNodeImpl(CSharpElementTypes.REFERENCE_NAME))

        val document = PsiDocumentManager.getInstance(project).getDocument(targetClassFile)

        harmonyClass = EditorTextField(document, project, CSharpFileType)
        harmonyResultPreview = LanguageTextField(CSharpLanguage, project, "", true)

        init()
    }

    enum class Visibility {
        PUBLIC,
        INTERNAL,
        PROTECTED,
        PRIVATE
    }

    override fun createCenterPanel(): JComponent {
        return panel {
            row {
                dropDownLink(Visibility.PRIVATE, Visibility.values().toList())
                harmonyClass
                harmonyResultPreview
            }
        }
    }
}