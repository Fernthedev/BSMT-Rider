package com.github.fernthedev.bsmt_rider

import com.fasterxml.jackson.databind.JsonNode
import com.fasterxml.jackson.databind.node.ObjectNode
import com.github.fernthedev.bsmt_rider.helpers.BeatSaberUtils
import com.github.fernthedev.bsmt_rider.helpers.ProjectUtils
import com.github.fernthedev.bsmt_rider.settings.AppSettingsState
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import java.io.File


data class BeatSaberFolders(
    val csprojFile: File,
    val projectFolder: File,
    val project: ProjectModelEntity
)

object BeatSaberProjectManager {
    private val selectedBeatSaberFolder = HashMap<Project, String?>()

    fun getSelectedBeatSaberFolder(project: Project): String? {
        return selectedBeatSaberFolder.getOrDefault(project, null)
    }




    fun locateFoldersAndGenerate(items: List<ProjectModelEntity>?, project: Project?, generate: Boolean) {
        val folders = BeatSaberUtils.locateBeatSaberProjects(items)
        val filesToRefresh: MutableList<File> = mutableListOf()

        val beatSaberFolder = getBeatSaberFolder() ?: return

        if (project != null) {
            selectedBeatSaberFolder[project] = beatSaberFolder
        }

        if (generate) {
            folders.forEach {
                if (generateUserFile(it.projectFolder, it.csprojFile, beatSaberFolder)) {
                    filesToRefresh.add(it.projectFolder)
                    filesToRefresh.add(it.csprojFile)
                }
            }

            ProjectUtils.refreshProjectWithFiles(folders, filesToRefresh, project)
        }
    }

    private fun generateUserFile(folder: File, csprojFile: File, beatSaberFolder: String): Boolean {
        // Get the folder of the solution, then get the folder of the actual project
        val userFile = File(folder, "${csprojFile.name}.user")

        if (folder.exists() && isBeatSaberProject(csprojFile)) {
            if (userFile.exists()) {
                return updateUserFile(userFile, beatSaberFolder)
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
                    return true
                }
            }
        }
        return false
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
    private fun updateUserFile(userCsprojFile: File, beatSaberFolder: String): Boolean {
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
            return false
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

        val xmlPreData = ProjectUtils.xmlParser.readTree(parsedContent)

        val xmlData: ObjectNode = if (xmlPreData is ObjectNode) {
            xmlPreData
        } else {
            ProjectUtils.xmlParser.createObjectNode()
        }

        // Modify
        val propertyGroupNodePre: JsonNode? = xmlData["PropertyGroup"]
        val propertyGroupNode: ObjectNode
        if (propertyGroupNodePre == null || propertyGroupNodePre.isNull || propertyGroupNodePre !is ObjectNode) {
            propertyGroupNode = ProjectUtils.xmlParser.createObjectNode()
            xmlData.set<ObjectNode>("PropertyGroup", propertyGroupNode)
        } else {
            propertyGroupNode = propertyGroupNodePre
        }

        val node = ProjectUtils.xmlParser.createArrayNode()

        node.add(beatSaberFolder)

        propertyGroupNode.set<JsonNode>("BeatSaberDir", node)

        val writer = ProjectUtils.xmlParser.writer().withDefaultPrettyPrinter().withRootName("Project")

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

        return true
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
