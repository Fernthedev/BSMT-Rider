package model.rider

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.bool
import com.jetbrains.rd.generator.nova.PredefinedType.string
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rider.model.nova.ide.SolutionModel

// https://github.com/JetBrains/resharper-unity/blob/9058c328202ac1a28e40a6bf29dd064c872934ee/rider/protocol/src/main/kotlin/model/backendUnity/BackendUnityModel.kt#L8-L14

@Suppress("unused")
// TODO: Find a way to make this not an extension and instead a singleton
object BSMT_RiderModel : Ext(SolutionModel.Solution) {

    val ConfigSettings = structdef {
        field("isDefaultBeatSaberLocation", bool)
        field("defaultBeatSaberLocation", string.nullable)

        field("ConfiguredBeatSaberLocations", array(string))
    }

    init {
        setting(CSharp50Generator.Namespace, "ReSharperPlugin.BSMT_Rider.Rider.Model")
//        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.bsmt_rider.model")

        callback("GetUserSettings", PredefinedType.void.nullable, ConfigSettings)
        call("FoundBeatSaberLocations", PredefinedType.void.nullable , array(string))

//        signal("myStructure", MyStructure)
    }
}