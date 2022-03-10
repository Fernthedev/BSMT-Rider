package com.github.fernthedev.bsmt_rider

import com.github.fernthedev.bsmt_rider.settings.AppSettingsState
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.progress.ProgressManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.psi.PsiManager
import com.intellij.psi.XmlElementFactory
import com.intellij.psi.xml.XmlFile
import com.intellij.psi.xml.XmlTag
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
            project!!
            folders.forEach {
                if (generateUserFile(it.projectFolder, it.csprojFile, beatSaberFolder, project)) {
                    filesToRefresh.add(it.projectFolder)
                    filesToRefresh.add(it.csprojFile)
                }
            }

            ProjectUtils.refreshProjectWithFiles(folders, filesToRefresh, project)
        }
    }

    private fun generateUserFile(folder: File, csprojFile: File, beatSaberFolder: String, project: Project): Boolean {
        // Get the folder of the solution, then get the folder of the actual project
        val userFile = File(folder, "${csprojFile.name}.user")

        if (folder.exists() && isBeatSaberProject(csprojFile)) {
            if (userFile.exists()) {
                return updateUserFile(userFile, beatSaberFolder, project)
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

    // This merges the previous XML data with the new one
    private fun updateUserFile(userCsprojFile: File, beatSaberFolder: String, project: Project): Boolean {
        val file = VfsUtil.findFileByIoFile(userCsprojFile, true)!!

        lateinit var csprojFileXML: XmlFile

        if (ApplicationManager.getApplication().isReadAccessAllowed) {
            csprojFileXML = PsiManager.getInstance(project).findFile(file) as XmlFile

        } else {
            require(!ApplicationManager.getApplication().isDispatchThread)

            while (!ProgressManager.getInstance().runInReadActionWithWriteActionPriority({
                    ProgressManager.checkCanceled()

                    // TODO:
                    csprojFileXML = PsiManager.getInstance(project).findFile(file) as XmlFile
                }, null)) {
                Thread.yield()
            }
        }

        // Skip if user.csproj already contains reference
        // Replace \ to / allows for paths to be resolved universally
        if (csprojFileXML.textMatches("""
                    <BeatSaberDir>${beatSaberFolder.replace("\\","/")}</BeatSaberDir>
                    """.trimIndent())
        ) {
            return false
        }

        if (csprojFileXML.document == null) throw IllegalAccessException("CSPROJ FILE DOCUMENT IS NULL TELL FERN ABOUT THIS")

        ApplicationManager.getApplication().invokeLaterOnWriteThread {
            ApplicationManager.getApplication().runWriteAction {
                val xmlFactory = XmlElementFactory.getInstance(project)
                val xmlDocument = csprojFileXML.document!!
                var rootTag = csprojFileXML.rootTag

                if (rootTag == null) {
                    rootTag = xmlDocument.add(xmlFactory.createTagFromText("Project")) as XmlTag
                }

                val propertyGroups = rootTag.findSubTags("PropertyGroup");

                if (propertyGroups.isEmpty()) {
                    val propertyGroup = rootTag.createChildTag("PropertyGroup", "", "", false)

                    val beatSaberDirTag = propertyGroup.createChildTag("BeatSaberDir", "", beatSaberFolder, false)


                } else {
                    var beatSaberDirTag = propertyGroups.find {
                        it.name == "BeatSaberDir"
                    }
                    val newValue = propertyGroups.first().createChildTag("BeatSaberDir", "", beatSaberFolder, false);

                    if (beatSaberDirTag == null) {
                        beatSaberDirTag = newValue
                    } else {
                        beatSaberDirTag.replace(newValue)
                    }
                }
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
