using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.LiveTemplates.CSharp.Macros;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl.DocComments;

namespace ReSharperPlugin.BSMT_Rider.macros
{
    // [MacroDefinition("HarmonyPatchClass")]
    // public class HarmonyPatchMacroDefinition : SimpleMacroDefinition
    // {
    //     public override ParameterInfo[] Parameters { get; } = {
    //         new(ParameterType.Type), new(ParameterType.String),
    //         new(ParameterType.String)
    //     };
    // }
    //
    // [MacroImplementation(Definition = typeof(HarmonyPatchMacroDefinition))]
    // public class HarmonyPatchMacroImplementation : SimpleMacroImplementation
    // {
    //     private IMacroParameterValueNew? classArgument;
    //
    //     public HarmonyPatchMacroImplementation(MacroParameterValueCollection arguments)
    //     {
    //         classArgument = arguments.OptionalFirstOrDefault();
    //     }
    //
    //     public override HotspotItems GetLookupItems(IHotspotContext context)
    //     {
    //         IMacroUtil macroUtil = MacroUtil.GetVisibleVariables(context).Variables;
    //         return base.GetLookupItems(context);
    //     }
    //
    //     public override string EvaluateQuickResult(IHotspotContext context)
    //     {
    //         MacroUtil.AsType(classArgument.GetValue(), context).
    //     _macroUtil.SuggestVariableTypes(context, CSharpLanguage.Instance);
    //         return myArgument == null ? null : myArgument.GetValue().ToUpperInvariant();
    //     }
    //     
    // }
    
}