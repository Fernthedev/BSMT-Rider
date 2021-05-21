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
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.IMutableViewableMap
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.model.RdCustomLocation
import com.jetbrains.rider.model.RdProjectDescriptor
import com.jetbrains.rider.model.RdProjectModelItem
import com.jetbrains.rider.model.projectModelView
import com.jetbrains.rider.projectView.solution
import java.io.File


data class BeatSaberFolders(
    val csprojFile: File,
    val projectFolder: File,
)

class BeatSaberGenerator(project: Project) : ProtocolSubscribedProjectComponent(project) {
    init {
        project.solution.isLoaded.whenTrue(projectComponentLifetime) {
            val solution = project.solution
            locateFoldersAndGenerate(solution.projectModelView.items)
        }
    }

    companion object {
        // with Jackson 2.10 and later
        private val mapper = XmlMapper.builder(XmlFactory(WstxInputFactory(), WstxOutputFactory())) // possible configuration changes
            .defaultPrettyPrinter(DefaultXmlPrettyPrinter())
            .build()

        fun locateFolders(items: IMutableViewableMap<Int, RdProjectModelItem>?): List<BeatSaberFolders> {
            if (items == null)
                return emptyList()

            val folders = mutableListOf<BeatSaberFolders>()
            items.forEach { (_, projectData) ->
                println("Project: ${projectData.descriptor.name}")


                val location = projectData.descriptor.location

                if (location is RdCustomLocation && location.customLocation.endsWith(".csproj")
                    && projectData.descriptor is RdProjectDescriptor
                ) {
                    val projectRdData: RdProjectDescriptor = projectData.descriptor as RdProjectDescriptor
                    val csprojFile = File(location.customLocation)

                    val projFolder = File(projectRdData.baseDirectory ?: csprojFile.parent)

                    folders.add(BeatSaberFolders(csprojFile, projFolder))
                }
            }

            return folders
        }

        fun locateFoldersAndGenerate(items: IMutableViewableMap<Int, RdProjectModelItem>) {
            locateFolders(items).forEach {
                generate(it.projectFolder, it.csprojFile)
            }
        }

        fun generate(folder: File, csprojFile: File) {
            // Get the folder of the solution, then get the folder of the actual project
            val userFile = File(folder, "${csprojFile.name}.user")

            if (folder.exists() && isBeatSaberProject(csprojFile)) {
                if (userFile.exists()) {
                    updateFileContent(userFile)
                } else {
                    val content = generateFileContent(getBeatSaberFolder())

                    ApplicationManager.getApplication().invokeLaterOnWriteThread {
                        ApplicationManager.getApplication().runWriteAction {
                            userFile.createNewFile()
                            VfsUtil.saveText(VfsUtil.findFileByIoFile(userFile, true)!!, content)
                        }
                    }
                }
            }
        }

        // TODO: Make this more performant
        fun isBeatSaberProject(file: File?): Boolean {
            if (file == null) return false

            if (!file.exists()) return false

            var contents = ""
            ApplicationManager.getApplication().runReadAction {
                contents = VfsUtil.loadText(VfsUtil.findFileByIoFile(file, true)!!)
            }
            
            return contents.contains("IPA.Loader") ||
                    contents.contains("BeatSaberModdingTools.Tasks") ||
                    contents.contains("$(BeatSaberDir)")
        }

        // TODO: Clean this up using POJO if possible.
        // This merges the previous XML data with the new one
        private fun updateFileContent(userCsprojFile: File) {
            val file = VfsUtil.findFileByIoFile(userCsprojFile, true)!!

            var contents = ""
            ApplicationManager.getApplication().runReadAction {
                contents = VfsUtil.loadText(file)
            }



            val beatSaberFolder = getBeatSaberFolder()

            // Skip if user.csproj already contains reference
            if (contents.trimIndent().contains(
                    """
                    <BeatSaberDir>$beatSaberFolder</BeatSaberDir>
                    """.trimIndent(),
                    ignoreCase = true)) {
                return
            }


            val startIndex = contents.indexOf("<Project>")
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

            val node = mapper.createArrayNode();

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

        // TODO: This is temporary
        // TODO: Make a setting
        private fun getBeatSaberFolder(): String {
            return AppSettingsState.instance.defaultFolder ?: "BeatSaberDir! Tacos are pog"
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
}