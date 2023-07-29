package com.github.fernthedev.bsmt_rider.helpers

import com.fasterxml.jackson.module.kotlin.readValue
import com.github.fernthedev.bsmt_rider.dialogue.BeatSaberReferencesDialogue
import com.github.fernthedev.bsmt_rider.settings.getBeatSaberSelectedDir
import com.github.fernthedev.bsmt_rider.xml.ReferenceXML
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.application.invokeAndWaitIfNeeded
import com.intellij.openapi.application.invokeLater
import com.intellij.openapi.application.runReadAction
import com.intellij.openapi.command.WriteCommandAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.psi.PsiManager
import com.intellij.psi.XmlElementFactory
import com.intellij.psi.xml.XmlFile
import com.intellij.psi.xml.XmlTag
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
        itemGroupParam: XmlTag,
        itemGroupRoot: XmlTag,
        refsToAdd: List<ReferenceXML>,
        project: Project,
        projectData: ProjectModelEntity
    ) {
        invokeLater {
            WriteCommandAction.runWriteCommandAction(project) {
                // add to root if not already
                val itemGroup = when {
                    itemGroupRoot.subTags.contains(itemGroupParam) -> itemGroupParam
                    else -> itemGroupRoot.addSubTag(itemGroupParam, false)
                }

                require(itemGroup.isWritable) { "Cannot write to tag!" }
                require(itemGroup.containingFile.isWritable) { "Cannot write to file!" }


                // I hate this
                refsToAdd.forEach { ref ->
                    val tag =
                        itemGroup.createChildTag("Reference", null, ref.toXMLNoRoot().replace("\r\n", "\n"), false);

                    val includeName = Path(ref.stringHintPath).nameWithoutExtension

                    val include = includeName.replace("\r\n", "\n")
                    tag.setAttribute("Include", include)

                    var added = false
                    for (subTag in itemGroup.subTags) {
                        val nextInclude = subTag.getAttributeValue("Include")
                        if (nextInclude != null && nextInclude > include) {
                            itemGroup.addBefore(tag, subTag)
                            added = true
                            break
                        }
                    }
                    if (!added) {
                        itemGroup.addSubTag(tag, false)
                    }
                }
            }

            ProjectUtils.refreshProjectManually(project, listOf(projectData), listOf(csprojFile.toIOFile()))
        }
    }

    fun askToAddReferences(project: Project) {
        require(!ApplicationManager.getApplication().isDispatchThread)

        val projectData: ProjectModelEntity =
            WorkspaceModel.getInstance(project).findProjectsByName(project.name).first()
        val projectRdData: RdProjectDescriptor = projectData.descriptor as RdProjectDescriptor
        val projectLocation = projectRdData.location as RdCustomLocation

        val csprojFile: VirtualFile = VfsUtil.findFileByIoFile(File(projectLocation.customLocation), true)!!

        lateinit var csprojFileXML: XmlFile
        lateinit var itemGroup: XmlTag
        lateinit var refs: List<ReferenceXML>

        // Read file
        runReadAction {
            // I hate PSI
            csprojFileXML = PsiManager.getInstance(project).findFile(csprojFile) as XmlFile
            val groups = csprojFileXML.document?.rootTag?.findSubTags("ItemGroup")

            itemGroup = when (val foundItemGroup =
                groups?.firstOrNull { itemGroup -> itemGroup.subTags.any { it.name == "Reference" } }) {
                null -> {
                    XmlElementFactory.getInstance(project).createTagFromText("<ItemGroup></ItemGroup>")
                }

                else -> {
                    foundItemGroup
                }
            }

            refs = getReferences(itemGroup)
        }

        // Find beat saber dir
        val path =
            project.getBeatSaberSelectedDir()
                ?: throw IllegalAccessException("No user csproj file found ${project.projectFilePath}:${project.name}") // TODO: Make this get the beat saber dir from csproj.user?

        val managedPath = BeatSaberUtils.getAssembliesOfBeatSaber(path)
        val libsPath = BeatSaberUtils.getLibsOfBeatSaber(path)
        val pluginsPath = BeatSaberUtils.getPluginsOfBeatSaber(path)

        // Open dialog and block until closed
        var refsFromDialogue = ArrayList<File>()

        invokeAndWaitIfNeeded {
            val dialogue = BeatSaberReferencesDialogue(project, arrayOf(managedPath, libsPath, pluginsPath), refs)

            if (dialogue.showAndGet()) {
                refsFromDialogue = dialogue.references
            }
        }


        if (refsFromDialogue.isEmpty()) return

        val pathWithOSSeparator = path.replace('/', File.separatorChar)
        val xmlRefsFromDialogue = refsFromDialogue.map {
            val hintPath = it.absolutePath.replace(pathWithOSSeparator, "\$(BeatSaberDir)")
            val private = false // TODO: should this be configurable?

            ReferenceXML(hintPath, private)
        }

        writeReferences(
            csprojFile,
            itemGroup,
            csprojFileXML.document?.rootTag!!,
            xmlRefsFromDialogue,
            project,
            projectData
        )
    }
}

