@file:Suppress("EXPERIMENTAL_API_USAGE","EXPERIMENTAL_UNSIGNED_LITERALS","PackageDirectoryMismatch","UnusedImport","unused","LocalVariableName","CanBeVal","PropertyName","EnumEntryName","ClassName","ObjectPropertyName","UnnecessaryVariable","SpellCheckingInspection")
package com.jetbrains.rider.model

import com.jetbrains.rd.framework.*
import com.jetbrains.rd.framework.base.*
import com.jetbrains.rd.framework.impl.*

import com.jetbrains.rd.util.lifetime.*
import com.jetbrains.rd.util.reactive.*
import com.jetbrains.rd.util.string.*
import com.jetbrains.rd.util.*
import kotlin.reflect.KClass



/**
 * #### Generated from [BSMT-RiderModel.kt:12]
 */
class BSMT_RiderModel private constructor(
    private val _getUserSettings: RdCall<Unit?, ConfigSettings>,
    private val _foundBeatSaberLocations: RdCall<Unit?, Array<String>>
) : RdExtBase() {
    //companion
    
    companion object : ISerializersOwner {
        
        override fun registerSerializersCore(serializers: ISerializers)  {
            serializers.register(ConfigSettings)
            BSMT_RiderModel.register(serializers)
        }
        
        
        fun create(lifetime: Lifetime, protocol: IProtocol): BSMT_RiderModel  {
            BSMT_RiderModel.register(protocol.serializers)
            
            return BSMT_RiderModel().apply {
                identify(protocol.identity, RdId.Null.mix("BSMT_RiderModel"))
//                bind(lifetime, protocol, "BSMT_RiderModel")
            }
        }
        
        private val __VoidNullableSerializer = FrameworkMarshallers.Void.nullable()
        private val __StringArraySerializer = FrameworkMarshallers.String.array()
        
        const val serializationHash = -5354681891147381364L
        
    }
    override val serializersOwner: ISerializersOwner get() = BSMT_RiderModel
    override val serializationHash: Long get() = BSMT_RiderModel.serializationHash
    
    //fields
    val getUserSettings: IRdEndpoint<Unit?, ConfigSettings> get() = _getUserSettings
    val foundBeatSaberLocations: IRdCall<Unit?, Array<String>> get() = _foundBeatSaberLocations
    //methods
    //initializer
    init {
        bindableChildren.add("getUserSettings" to _getUserSettings)
        bindableChildren.add("foundBeatSaberLocations" to _foundBeatSaberLocations)
    }
    
    //secondary constructor
    private constructor(
    ) : this(
        RdCall<Unit?, ConfigSettings>(__VoidNullableSerializer, ConfigSettings),
        RdCall<Unit?, Array<String>>(__VoidNullableSerializer, __StringArraySerializer)
    )
    
    //equals trait
    //hash code trait
    //pretty print
    override fun print(printer: PrettyPrinter)  {
        printer.println("BSMT_RiderModel (")
        printer.indent {
            print("getUserSettings = "); _getUserSettings.print(printer); println()
            print("foundBeatSaberLocations = "); _foundBeatSaberLocations.print(printer); println()
        }
        printer.print(")")
    }
    //deepClone
    override fun deepClone(): BSMT_RiderModel   {
        return BSMT_RiderModel(
            _getUserSettings.deepClonePolymorphic(),
            _foundBeatSaberLocations.deepClonePolymorphic()
        )
    }
    //contexts
}


/**
 * #### Generated from [BSMT-RiderModel.kt:14]
 */
data class ConfigSettings (
    val isDefaultBeatSaberLocation: Boolean,
    val defaultBeatSaberLocation: String?,
    val configuredBeatSaberLocations: Array<String>
) : IPrintable {
    //companion
    
    companion object : IMarshaller<ConfigSettings> {
        override val _type: KClass<ConfigSettings> = ConfigSettings::class
        
        @Suppress("UNCHECKED_CAST")
        override fun read(ctx: SerializationCtx, buffer: AbstractBuffer): ConfigSettings  {
            val isDefaultBeatSaberLocation = buffer.readBool()
            val defaultBeatSaberLocation = buffer.readNullable { buffer.readString() }
            val configuredBeatSaberLocations = buffer.readArray {buffer.readString()}
            return ConfigSettings(isDefaultBeatSaberLocation, defaultBeatSaberLocation, configuredBeatSaberLocations)
        }
        
        override fun write(ctx: SerializationCtx, buffer: AbstractBuffer, value: ConfigSettings)  {
            buffer.writeBool(value.isDefaultBeatSaberLocation)
            buffer.writeNullable(value.defaultBeatSaberLocation) { buffer.writeString(it) }
            buffer.writeArray(value.configuredBeatSaberLocations) { buffer.writeString(it) }
        }
        
        
    }
    //fields
    //methods
    //initializer
    //secondary constructor
    //equals trait
    override fun equals(other: Any?): Boolean  {
        if (this === other) return true
        if (other == null || other::class != this::class) return false
        
        other as ConfigSettings
        
        if (isDefaultBeatSaberLocation != other.isDefaultBeatSaberLocation) return false
        if (defaultBeatSaberLocation != other.defaultBeatSaberLocation) return false
        if (!(configuredBeatSaberLocations contentDeepEquals other.configuredBeatSaberLocations)) return false
        
        return true
    }
    //hash code trait
    override fun hashCode(): Int  {
        var __r = 0
        __r = __r*31 + isDefaultBeatSaberLocation.hashCode()
        __r = __r*31 + if (defaultBeatSaberLocation != null) defaultBeatSaberLocation.hashCode() else 0
        __r = __r*31 + configuredBeatSaberLocations.contentDeepHashCode()
        return __r
    }
    //pretty print
    override fun print(printer: PrettyPrinter)  {
        printer.println("ConfigSettings (")
        printer.indent {
            print("isDefaultBeatSaberLocation = "); isDefaultBeatSaberLocation.print(printer); println()
            print("defaultBeatSaberLocation = "); defaultBeatSaberLocation.print(printer); println()
            print("configuredBeatSaberLocations = "); configuredBeatSaberLocations.print(printer); println()
        }
        printer.print(")")
    }
    //deepClone
    //contexts
}
