package com.github.fernthedev.bsmt_rider.helpers

import com.github.fernthedev.bsmt_rider.BeatSaberFolders
import com.jetbrains.rider.model.RdCustomLocation
import com.jetbrains.rider.model.RdProjectDescriptor
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import java.io.File
import kotlin.io.path.Path

object BeatSaberUtils {


    fun getAssembliesOfBeatSaber(beatSaberPath: String): String =
        Path(beatSaberPath, "Beat Saber_Data", "Managed").toString()

    fun getModLoaderOfBeatSaber(beatSaberPath: String): String = Path(beatSaberPath, "IPA").toString()

    fun getLibsOfBeatSaber(beatSaberPath: String): String = Path(beatSaberPath, "Libs").toString()

    fun getPluginsOfBeatSaber(beatSaberPath: String): String = Path(beatSaberPath, "Plugins").toString()

    fun locateBeatSaberProjects(items: List<ProjectModelEntity>?): List<BeatSaberFolders> {
        if (items == null)
            return emptyList()

        val folders = items.mapNotNull { projectData ->
            println("Project: ${projectData.descriptor.name}")
            val location = projectData.descriptor.location

            if (location !is RdCustomLocation) {
                return@mapNotNull null
            }
            if (!location.customLocation.endsWith(".csproj")) {
                return@mapNotNull null
            }
            if (projectData.descriptor !is RdProjectDescriptor) {
                return@mapNotNull null
            }

            val projectRdData: RdProjectDescriptor = projectData.descriptor as RdProjectDescriptor
            val csprojFile = File(location.customLocation)

            val projFolder = File(projectRdData.baseDirectory ?: csprojFile.parent)

            BeatSaberFolders(csprojFile, projFolder, projectData)
        }

        return folders
    }
}