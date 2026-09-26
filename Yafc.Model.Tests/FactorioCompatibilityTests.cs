using System.Collections.Generic;
using System.Text;
using Xunit;
using Yafc.Parser;

namespace Yafc.Model.Tests;

[Collection("LuaDependentTests")]
public class FactorioCompatibilityTests {
    [Fact]
    public void RecommendedDependency_IsOptionalButStillOrdersLoading() {
        var mod = new FactorioDataSource.ModInfo("", Encoding.UTF8.GetBytes(
            """{"name":"test","version":"1.0.0","dependencies":["+ recommended >= 1.0.0"]}"""), 0, 0);
        mod.ParseDependencies();

        Assert.True(mod.CheckDependencies([], []));
        Assert.False(mod.CanLoad(new HashSet<string> { "recommended" }));
        Assert.True(mod.CanLoad([]));
    }
}
