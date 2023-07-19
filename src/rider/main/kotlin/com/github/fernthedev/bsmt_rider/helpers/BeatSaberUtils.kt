package com.github.fernthedev.bsmt_rider.helpers

import com.github.fernthedev.bsmt_rider.BeatSaberFolders
import com.jetbrains.rider.model.RdCustomLocation
import com.jetbrains.rider.model.RdProjectDescriptor
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import java.io.File
import kotlin.io.path.Path

object BeatSaberUtils {


    fun getAssembliesOfBeatSaber(beatSaberPath: String): String = Path(beatSaberPath, "Beat Saber_Data", "Managed").toString()


    fun getLibsOfBeatSaber(beatSaberPath: String): String = Path(beatSaberPath, "Libs").toString()

    fun getPluginsOfBeatSaber(beatSaberPath: String): String = Path(beatSaberPath, "Plugins").toString()

    fun locateBeatSaberProjects(items: List<ProjectModelEntity>?): List<BeatSaberFolders> {
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
}