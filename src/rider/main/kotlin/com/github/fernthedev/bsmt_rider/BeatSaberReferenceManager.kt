package com.github.fernthedev.bsmt_rider

import com.fasterxml.jackson.module.kotlin.readValue
import com.github.fernthedev.bsmt_rider.dialogue.BeatSaberReferencesDialogue
import com.github.fernthedev.bsmt_rider.settings.getBeatSaberSelectedDir
import com.github.fernthedev.bsmt_rider.xml.ReferenceXML
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.application.invokeLater
import com.intellij.openapi.command.WriteCommandAction
import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.psi.PsiManager
import com.intellij.psi.xml.XmlFile
import com.intellij.psi.xml.XmlTag
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.jetbrains.rdclient.util.idea.toIOFile
import com.jetbrains.rider.model.RdCustomLocation
import com.jetbrains.rider.model.RdProjectDescriptor
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.findProjectsByName
import java.io.File
import kotlin.io.path.ExperimentalPathApi
import kotlin.io.path.Path
import kotlin.io.path.nameWithoutExtension

object BeatSaberReferenceManager {

    private fun getReferences(itemGroup: XmlTag): List<ReferenceXML> {
        val refs = ArrayList<ReferenceXML>()

        if (itemGroup.isEmpty)
            return emptyList()

        itemGroup.subTags.forEach {
            if (it.subTags.isNotEmpty()) {
                val ref = ProjectUtils.xmlParser.readValue<ReferenceXML>(it.text)


                refs.add(ref)
            }
        }

        return refs
    }

    @OptIn(ExperimentalPathApi::class)
    private fun writeReferences(
        csprojFile: VirtualFile,
        itemGroup: XmlTag,
        refsToAdd: List<ReferenceXML>,
        project: Project,
        projectData: ProjectModelEntity
    ) {

        try {
            WriteCommandAction.runWriteCommandAction(project) {
                require(itemGroup.isWritable) { "Cannot write to tag!" }
                require(itemGroup.containingFile.isWritable) { "Cannot write to file!" }


                refsToAdd.forEach { ref ->
                    val tag = itemGroup.createChildTag("Reference", "", ref.toXMLNoRoot().replace("\r\n", "\n"), false);

                    val includeName = Path(ref.stringHintPath).nameWithoutExtension

                    tag.setAttribute("Include", includeName.replace("\r\n", "\n"))
                    itemGroup.addSubTag(tag, false)
                }
            }

            invokeLater {
                ProjectUtils.refreshProjectManually(project, listOf(projectData), listOf(csprojFile.toIOFile()))
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    fun askToAddReferences(project: Project) {
        val projectData: ProjectModelEntity = WorkspaceModel.getInstance(project).findProjectsByName(project.name).first()
        val projectRdData: RdProjectDescriptor = projectData.descriptor as RdProjectDescriptor
        val projectLocation = projectRdData.location as RdCustomLocation

        val csprojFile: VirtualFile = VfsUtil.findFileByIoFile(File(projectLocation.customLocation), true)!!

        lateinit var csprojFileXML: XmlFile
        lateinit var itemGroup: XmlTag
        lateinit var refs: List<ReferenceXML>
        while (!ProgressManager.getInstance().runInReadActionWithWriteActionPriority({
                ProgressManager.checkCanceled()
                csprojFileXML = PsiManager.getInstance(project).findFile(csprojFile) as XmlFile
                itemGroup = csprojFileXML.document?.rootTag?.findFirstSubTag("ItemGroup")!!
                refs = getReferences(itemGroup)
            }, null)) {
            Thread.yield()
        }

        val path =
            project.getBeatSaberSelectedDir() ?: throw IllegalAccessException("No user csproj file found ${project.projectFilePath}:${project.name}") // TODO: Make this get the beat saber dir from csproj.user?

        val managedPath = BeatSaberUtils.getAssembliesOfBeatSaber(path)
        val libsPath = BeatSaberUtils.getLibsOfBeatSaber(path)
        val pluginsPath = BeatSaberUtils.getPluginsOfBeatSaber(path)

        var refsFromDialogue = ArrayList<File>()

        ApplicationManager.getApplication().invokeAndWait {
            val dialogue = BeatSaberReferencesDialogue(project, arrayOf(managedPath, libsPath, pluginsPath), refs)

            if (dialogue.showAndGet()) {
                refsFromDialogue = dialogue.references
            }
        }

        if (refsFromDialogue.isNotEmpty()) {
            val pathWithOSSeparator = path.replace('/', File.separatorChar)
            val xmlRefsFromDialogue = refsFromDialogue.map {
                val hintPath = it.absolutePath.replace(pathWithOSSeparator, "\$(BeatSaberDir)")
                val private = false // TODO: should this be configurable?

                ReferenceXML(hintPath, private)
            }

            writeReferences(csprojFile, itemGroup, xmlRefsFromDialogue, project, projectData)
        }
    }
}

