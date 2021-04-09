using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

[assembly: Apartment(ApartmentState.STA)]

namespace ReSharperPlugin.BSMT_Rider.Tests
{

    [ZoneDefinition]
    public class BSMT_RiderTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>, IRequire<IBSMT_RiderZone> { }

    [ZoneMarker]
    public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<BSMT_RiderTestEnvironmentZone> { }
    
    [SetUpFixture]
    public class BSMT_RiderTestsAssembly : ExtensionTestEnvironmentAssembly<BSMT_RiderTestEnvironmentZone> { }
}