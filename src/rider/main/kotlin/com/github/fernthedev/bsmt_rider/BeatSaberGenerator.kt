package com.github.fernthedev.bsmt_rider

import com.ctc.wstx.stax.WstxInputFactory
import com.ctc.wstx.stax.WstxOutputFactory
import com.fasterxml.jackson.databind.JsonNode
import com.fasterxml.jackson.databind.node.ObjectNode
import com.fasterxml.jackson.dataformat.xml.XmlFactory
import com.fasterxml.jackson.dataformat.xml.XmlMapper
import com.fasterxml.jackson.dataformat.xml.util.DefaultXmlPrettyPrinter
import com.github.fernthedev.bsmt_rider.settings.AppSettingsState
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rider.model.*
import com.jetbrains.rider.projectView.ProjectModelViewUpdater
import com.jetbrains.rider.projectView.ProjectModelViewUpdaterAsync
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.getId
import com.jetbrains.rider.util.idea.runUnderProgress
import java.io.File


data class BeatSaberFolders(
    val csprojFile: File,
    val projectFolder: File,
    val project: ProjectModelEntity
)

object BeatSaberGenerator {
    // with Jackson 2.10 and later
    private val mapper =
        XmlMapper.builder(XmlFactory(WstxInputFactory(), WstxOutputFactory())) // possible configuration changes
            .defaultPrettyPrinter(DefaultXmlPrettyPrinter())
            .build()

    fun locateFolders(items: List<ProjectModelEntity>?): List<BeatSaberFolders> {
        if (items == null)
            return emptyList()

        val folders = mutableListOf<BeatSaberFolders>()
        items.forEach { projectData ->
            println("Project: ${projectData.descriptor.name}")


            val location = projectData.descriptor.location

            if (location is RdCustomLocation && location.customLocation.endsWith(".csproj")
                && projectData.descriptor is RdProjectDescriptor
            ) {
                val projectRdData: RdProjectDescriptor = projectData.descriptor as RdProjectDescriptor
                val csprojFile = File(location.customLocation)

                val projFolder = File(projectRdData.baseDirectory ?: csprojFile.parent)

                folders.add(BeatSaberFolders(csprojFile, projFolder, projectData))
            }
        }

        return folders
    }


    fun locateFoldersAndGenerate(items: List<ProjectModelEntity>?, project: Project?) {
        val folders = locateFolders(items)
        val filesToRefresh: MutableList<File> = mutableListOf()

        folders.forEach {
            generate(it.projectFolder, it.csprojFile)
            filesToRefresh.add(it.projectFolder)
            filesToRefresh.add(it.csprojFile)
        }

        if (filesToRefresh.isNotEmpty() && project != null) {

            // TODO: Make this only run if a `csproj.user` file has been created or modified, not all the time
            // We do this to force ordering
            // In other words, force it to refresh AFTER writing is done
            // I'm not proud of this ugly code nesting at all nor hard coding this
            ApplicationManager.getApplication().invokeLaterOnWriteThread {
                ApplicationManager.getApplication().runWriteAction {
                    ApplicationManager.getApplication().invokeLater {

                        ProjectModelViewUpdater.fireUpdate(project) {
                            it.updateAll()
                        }
                        ProjectModelViewUpdaterAsync.getInstance(project).requestUpdatePresentation()

                        /// We have to hard code UnloadProjectAction.execute it seems unfortunately
                        /// This is ugly, maybe find a way to get a component that references the
                        // csproj directly so we can use DataContexts?
                        val projects = folders.mapNotNull { it.project.getId(project) }

                        application.saveAll()
                        val command =
                            UnloadCommand(projects.toList())

                        project.solution.projectModelTasks.unloadProjects.runUnderProgress(
                            command,
                            project,
                            "Unload ${projects.size} projects...",
                            isCancelable = false,
                            throwFault = false
                        )

                        // We do the same here sadly
                        val command2 =
                            ReloadCommand(projects.toList())

                        project.solution.projectModelTasks.reloadProjects.runUnderProgress(
                            command2,
                            project,
                            "Reload ${projects.size} projects...",
                            isCancelable = false,
                            throwFault = false,
                        )
                    }
                    VfsUtil.markDirtyAndRefresh(true, false, false, *filesToRefresh.toTypedArray())
                }
            }
        }
    }

    private fun generate(folder: File, csprojFile: File) {
        // Get the folder of the solution, then get the folder of the actual project
        val userFile = File(folder, "${csprojFile.name}.user")

        if (folder.exists() && isBeatSaberProject(csprojFile)) {
            if (userFile.exists()) {
                updateFileContent(userFile)
            } else {
                val userString = getBeatSaberFolder()

                if (userString != null) {
                    val content = generateFileContent(userString)

                    ApplicationManager.getApplication().invokeLaterOnWriteThread {
                        ApplicationManager.getApplication().runWriteAction {
                            userFile.createNewFile()
                            VfsUtil.saveText(VfsUtil.findFileByIoFile(userFile, true)!!, content)
                        }
                    }
                }
            }
        }
    }

    // TODO: Make this more performant
    // TODO: Make this a coroutine?
    fun isBeatSaberProject(file: File?): Boolean {
        if (file == null) return false

        if (!file.exists()) return false

        var contents = ""

        if (ApplicationManager.getApplication().isDispatchThread) {
            ApplicationManager.getApplication().runReadAction {
                contents = VfsUtil.loadText(VfsUtil.findFileByIoFile(file, true)!!)
            }
        } else {
            while (!ProgressManager.getInstance().runInReadActionWithWriteActionPriority({
                    ProgressManager.checkCanceled()
                    contents = VfsUtil.loadText(VfsUtil.findFileByIoFile(file, true)!!)
                }, null)) {
                // Avoid using resources
                Thread.yield()
            }
        }


        return contents.contains("IPA.Loader") ||
                contents.contains("BeatSaberModdingTools.Tasks") ||
                contents.contains("$(BeatSaberDir)")
    }

    // TODO: Clean this up using POJO if possible.
    // This merges the previous XML data with the new one
    private fun updateFileContent(userCsprojFile: File) {
        val beatSaberFolder = getBeatSaberFolder() ?: return

        val file = VfsUtil.findFileByIoFile(userCsprojFile, true)!!

        var contents = ""
        while (!ProgressManager.getInstance().runInReadActionWithWriteActionPriority({
                ProgressManager.checkCanceled()
                contents = VfsUtil.loadText(file)
            }, null)) {
            // Avoid using resources
            Thread.yield()
        }

        // Skip if user.csproj already contains reference
        // Replace \ to / allows for paths to be resolved universally
        if (contents.trimIndent().replace("\\","/").contains(
                """
                    <BeatSaberDir>${beatSaberFolder.replace("\\","/")}</BeatSaberDir>
                    """.trimIndent(),
                ignoreCase = true
            )
        ) {
            return
        }


        val startIndex = contents.indexOf("<Project")
        val endString = "</Project>"
        val endIndex = contents.indexOf(endString) + endString.length

        var parsedContent = contents

        // Get from the start of project to end of file
        if (startIndex != 0 && startIndex > 0) {
            parsedContent = contents.substring(startIndex, endIndex)
        }

        // Trim end to end of </Project>
        if (endIndex != contents.lastIndex)
            parsedContent = parsedContent.substring(0, parsedContent.indexOf(endString) + endString.length)

        val xmlPreData = mapper.readTree(parsedContent)

        val xmlData: ObjectNode = if (xmlPreData is ObjectNode) {
            xmlPreData
        } else {
            mapper.createObjectNode()
        }

        // Modify
        val propertyGroupNodePre: JsonNode? = xmlData["PropertyGroup"]
        val propertyGroupNode: ObjectNode
        if (propertyGroupNodePre == null || propertyGroupNodePre.isNull || propertyGroupNodePre !is ObjectNode) {
            propertyGroupNode = mapper.createObjectNode()
            xmlData.set<ObjectNode>("PropertyGroup", propertyGroupNode)
        } else {
            propertyGroupNode = propertyGroupNodePre
        }

        val node = mapper.createArrayNode()

        node.add(beatSaberFolder)

        propertyGroupNode.set<JsonNode>("BeatSaberDir", node)

        val writer = mapper.writer().withDefaultPrettyPrinter().withRootName("Project")

        // Merge xml
        val body = writer.writeValueAsString(xmlData)

        // This will merge the contents of the XML and new XML
        val finalString = StringBuilder()

        when {
            // Merge XML
            startIndex > 0 -> {

                val end = if (endIndex != contents.lastIndex && endIndex + 1 < contents.lastIndex) {
                    contents.substring(endIndex + 1, contents.lastIndex)
                } else {
                    ""
                }

                finalString
                    .append(contents.substring(0, startIndex - 1)).append("\n") // Head
                    .append(body) // Body
                    .append(end) // End
            }
            // Project was never defined
            startIndex < 0 -> {
                finalString
                    .append(contents).append("\n")
                    .append(body)
            }
            // Project was first
            else -> {
                val end = if (endIndex != contents.lastIndex && endIndex + 1 < contents.lastIndex) {
                    contents.substring(endIndex + 1, contents.lastIndex)
                } else {
                    ""
                }

                finalString.append(body)
                    .append(end)
            }
        }

        ApplicationManager.getApplication().invokeLaterOnWriteThread {
            ApplicationManager.getApplication().runWriteAction {
                VfsUtil.saveText(file, finalString.toString())
            }
        }

    }

    private fun getBeatSaberFolder(project: Project? = null): String? {
        return AppSettingsState.instance.getBeatSaberDir(project)
    }

    private fun generateFileContent(beatSaberFolder: String): String {
        return """
                <?xml version="1.0" encoding="utf-8"?>
                <Project>
                  <PropertyGroup>
                		<!-- Change this path if necessary. Make sure it ends with a backslash. -->
                		<BeatSaberDir>$beatSaberFolder</BeatSaberDir>
                  </PropertyGroup>
                </Project>
            """.trimIndent()
    }
}
