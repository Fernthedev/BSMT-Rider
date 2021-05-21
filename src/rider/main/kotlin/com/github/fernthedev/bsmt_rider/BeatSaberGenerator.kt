package com.github.fernthedev.bsmt_rider

import com.ctc.wstx.stax.WstxInputFactory
import com.ctc.wstx.stax.WstxOutputFactory
import com.fasterxml.jackson.databind.JsonNode
import com.fasterxml.jackson.databind.node.ObjectNode
import com.fasterxml.jackson.dataformat.xml.XmlFactory
import com.fasterxml.jackson.dataformat.xml.XmlMapper
import com.fasterxml.jackson.dataformat.xml.util.DefaultXmlPrettyPrinter
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rd.platform.ui.bedsl.extensions.valueOrEmpty
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rider.model.runnableProjectsModel
import com.jetbrains.rider.projectView.hasSolution
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionPath
import java.io.File


class BeatSaberGenerator(project: Project) : ProtocolSubscribedProjectComponent(project) {
    init {
        generate(project)
    }

    companion object {
        // with Jackson 2.10 and later
        private val mapper = XmlMapper.builder(XmlFactory(WstxInputFactory(), WstxOutputFactory())) // possible configuration changes
            .defaultPrettyPrinter(DefaultXmlPrettyPrinter())
            .build()

        fun generate(project: Project) {
            if (project.projectFile?.extension == "csproj.user") {
                updateFileContent(getUserCsprojFile(project))
                return
            }

            if (project.hasSolution) {

                val solution = project.solution
                val projectsInSolution = solution.runnableProjectsModel.projects;

                projectsInSolution.valueOrEmpty().forEach { t ->
                    println("Project: ${t.name}");
                }

                // Get the folder of the solution, then get the folder of the actual project
                val folder = getProjectFolder(project)
                val userFile = getUserCsprojFile(project)

                if (folder.exists()) {
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
        }

        // This code is problematic since it assumes that project name, folder and csproj name are all equal. We need a fix
        private fun getProjectFolder(project: Project): File {
            return File(File(project.solutionPath).parentFile, project.name)
        }

        private fun getCsprojFile(project: Project): File {
            return File(getProjectFolder(project), "${project.name}.csproj")
        }

        private fun getUserCsprojFile(project: Project): File {
            return File(getProjectFolder(project), "${project.name}.csproj.user")
        }

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
            if (propertyGroupNodePre == null || propertyGroupNodePre.isNull || !(propertyGroupNodePre is ObjectNode)) {
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
                        .append(contents.substring(0, startIndex - 1)) // Head
                        .append(body) // Body
                        .append(end) // End
                }
                // Project was never defined
                startIndex < 0 -> {
                    finalString
                        .append(contents)
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

        private fun getBeatSaberFolder(): String {
            return "F:\\SteamLibrary\\steamapps\\common\\Beat Saber"
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