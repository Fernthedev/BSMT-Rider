package com.github.fernthedev.bsmt_rider.xml

import com.fasterxml.jackson.annotation.JsonAutoDetect
import com.fasterxml.jackson.annotation.JsonIgnoreProperties
import com.fasterxml.jackson.annotation.JsonProperty
import com.intellij.util.xml.DomElement

@JsonAutoDetect(fieldVisibility = JsonAutoDetect.Visibility.ANY)
@JsonIgnoreProperties(ignoreUnknown = true)
data class ReferenceXML(
    @JsonProperty("HintPath")
    @get:JsonProperty("HintPath")
    val stringHintPath: String,

    @JsonProperty("Private")
    @get:JsonProperty("Private")
    val private: Boolean
    )  {

    // I really hate that I have to do this
    fun toXMLNoRoot() = """
        <HintPath>${stringHintPath}</HintPath>
        <Private>${private}</Private>
    """.trimIndent()
}

interface ItemGroupXML : DomElement {
    val references: List<ReferenceXML>
}