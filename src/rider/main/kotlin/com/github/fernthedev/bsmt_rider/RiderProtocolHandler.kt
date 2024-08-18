package com.github.fernthedev.bsmt_rider

import com.github.fernthedev.bsmt_rider.settings.AppSettingsState
import com.intellij.openapi.application.ApplicationManager
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.model.BSMT_RiderModel
import com.jetbrains.rider.model.ConfigSettings
import com.jetbrains.rider.protocol.ProtocolManager

// TODO: This doesn't work
private class RiderProtocolHandler {
    companion object {
        val instance: RiderProtocolHandler
            get() = ApplicationManager.getApplication().getService(RiderProtocolHandler::class.java)
    }


//    val bsmtRidermodel: BSMT_RiderModel


    init {
        val protocol = ProtocolManager.createProtocol(ApplicationManager.getApplication(), Lifetime.Eternal)
//        bsmtRidermodel = BSMT_RiderModel(ApplicationManager.getApplication().lifetime, protocol)

        ApplicationManager.getApplication().invokeLater {

//            val protocol = IdeBackend.allBackends.first().protocol


//            bsmtRidermodel.getUserSettings.set { _ ->
//                val instance = AppSettingsState.instance
//                ConfigSettings(
//                    isDefaultBeatSaberLocation = instance.useDefaultFolder,
//                    defaultBeatSaberLocation = instance.defaultFolder,
//                    configuredBeatSaberLocations = instance.beatSaberFolders
//                )
//            }
        }

        //            RiderProtocolHandler.bsmtRidermodel.foundBeatSaberLocations.start(Lifetime.Eternal, null).result.adviseOnce(
//                Lifetime.Eternal
//            ) {
//                if (beatSaberFolders.isEmpty()) {
//                    beatSaberFolders = it.unwrap()
//                }
//            }
    }
}