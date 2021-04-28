package com.github.fernthedev.bsmt_rider

import com.intellij.icons.AllIcons
import com.intellij.openapi.module.ModuleType
import javax.swing.Icon

class BSMTModuleType : ModuleType<BSMTModuleBuilder>("BSMT_MODULE") {
    override fun createModuleBuilder(): BSMTModuleBuilder = BSMTModuleBuilder()

    override fun getName(): String = "Beat Saber Modding Tools"

    override fun getDescription(): String = "Make modding Beat Saber a breeze on Rider 🤞"

    override fun getNodeIcon(p0: Boolean): Icon {
//        TODO("Not yet implemented")
        return AllIcons.Modules.SourceRoot // Only Icon I could find
    }
}