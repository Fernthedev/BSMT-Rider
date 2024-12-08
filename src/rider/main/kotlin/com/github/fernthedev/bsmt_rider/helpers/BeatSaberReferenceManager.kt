package com.github.fernthedev.bsmt_rider.helpers

import com.fasterxml.jackson.module.kotlin.readValue
import com.github.fernthedev.bsmt_rider.dialogue.BeatSaberReferencesDialogue
import com.github.fernthedev.bsmt_rider.settings.getBeatSaberSelectedDir
import com.github.fernthedev.bsmt_rider.xml.ReferenceXML
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.application.EDT
import com.intellij.openapi.application.readAction
import com.intellij.openapi.application.readActionBlocking
import com.intellij.openapi.command.WriteCommandAction
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
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
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.File
import kotlin.io.path.Path
import kotlin.io.path.nameWithoutExtension

@Service(Service.Level.PROJECT)
class BeatSaberReferenceManager(
    val project: Project, val scope: CoroutineScope
) {

    private suspend fun getReferences(itemGroup: XmlTag): List<ReferenceXML> {
        val refs = ArrayList<ReferenceXML>()

        val empty = readActionBlocking { itemGroup.isEmpty }
        if (empty) return emptyList()

        readActionBlocking {
            itemGroup.subTags.forEach {
                if (it.subTags.isNotEmpty()) {
                    val text = it.text

                    val ref = ProjectUtils.xmlParser.readValue<ReferenceXML>(text)

                    refs.add(ref)
                }
            }
        }

        return refs
    }


    private suspend fun writeReferences(
        csprojFile: VirtualFile,
        itemGroupParam: XmlTag,
        itemGroupRoot: XmlTag,
        refsToAdd: List<ReferenceXML>,
        project: Project,
        projectData: ProjectModelEntity
    ) {
        // add to root if not already
        val itemGroup = when {
            itemGroupRoot.subTags.contains(itemGroupParam) -> itemGroupParam
            else -> itemGroupRoot.addSubTag(itemGroupParam, false)
        }

        readAction {
            require(itemGroup.isWritable) { "Cannot write to tag!" }
            require(itemGroup.containingFile.isWritable) { "Cannot write to file!" }
        }

        // I hate this
        // This allows for undo
        WriteCommandAction.runWriteCommandAction(project, "Add References", null, {
            refsToAdd.forEach { ref ->
                val tag = itemGroup.createChildTag("Reference", null, ref.toXMLNoRoot().replace("\r\n", "\n"), false);

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
        })

        project.service<ProjectUtils>().refreshProjectManually(listOf(projectData), listOf(csprojFile.toIOFile()))

    }

    suspend fun askToAddReferences(projectName: String) {
        require(!ApplicationManager.getApplication().isDispatchThread)

        val projectData: ProjectModelEntity =
            WorkspaceModel.getInstance(project).findProjectsByName(projectName).first()
        val projectRdData: RdProjectDescriptor = projectData.descriptor as RdProjectDescriptor
        val projectLocation = projectRdData.location as RdCustomLocation

        val csprojFile: VirtualFile = readActionBlocking {
            VfsUtil.findFileByIoFile(File(projectLocation.customLocation), true)!!
        }

        val csprojFileXML = readActionBlocking {
            PsiManager.getInstance(project).findFile(csprojFile) as XmlFile
        }

        val itemGroup = readActionBlocking {
            // I hate PSI
            val groups = csprojFileXML.document?.rootTag?.findSubTags("ItemGroup")

            when (val foundItemGroup =
                groups?.firstOrNull { itemGroup -> itemGroup.subTags.any { it.name == "Reference" } }) {
                null -> {
                    XmlElementFactory.getInstance(project).createTagFromText("<ItemGroup></ItemGroup>")
                }

                else -> {
                    foundItemGroup
                }
            }
        }

        val refs = getReferences(itemGroup)

        // Find beat saber dir
        val path = project.getBeatSaberSelectedDir()
            ?: throw IllegalAccessException("No user csproj file found ${project.projectFilePath}:${project.name}") // TODO: Make this get the beat saber dir from csproj.user?

        val managedPath = BeatSaberUtils.getAssembliesOfBeatSaber(path)
        val libsPath = BeatSaberUtils.getLibsOfBeatSaber(path)
        val pluginsPath = BeatSaberUtils.getPluginsOfBeatSaber(path)

        // Open dialog and block until closed
        val refsFromDialogue = withContext(Dispatchers.EDT) {
            val dialogue = BeatSaberReferencesDialogue(project, arrayOf(managedPath, libsPath, pluginsPath), refs)

            if (dialogue.showAndGet()) {
                dialogue.references
            } else {
                null
            }
        }

        if (refsFromDialogue.isNullOrEmpty()) return

        val pathWithOSSeparator = path.replace('/', File.separatorChar)
        val xmlRefsFromDialogue = refsFromDialogue.map {
            val hintPath = it.absolutePath.replace(pathWithOSSeparator, "\$(BeatSaberDir)")
            val private = false // TODO: should this be configurable?

            ReferenceXML(hintPath, private)
        }

        writeReferences(
            csprojFile, itemGroup, csprojFileXML.document?.rootTag!!, xmlRefsFromDialogue, project, projectData
        )
    }
}

