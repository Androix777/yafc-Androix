using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Yafc.Parser;

namespace Yafc.Model.Tests;

[Collection("LuaDependentTests")]
public class FactorioCompatibilityTests {
    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(true, 42)]
    public void TurdItemFix_OnlySuppliesMissingStackSize(bool itemExists, int? stackSize) {
        using var context = new LuaContext(new(2, 1, 20));
        var items = context.NewTable();
        var item = context.NewTable();
        if (stackSize.HasValue) {
            item["stack_size"] = stackSize.Value;
        }
        if (itemExists) {
            items["hidden-beacon-turd"] = item;
        }
        var raw = context.NewTable();
        raw["item"] = items;
        var data = context.NewTable();
        data["raw"] = raw;
        context.SetGlobal("data", data);

        context.Exec(File.ReadAllBytes("Data/Mod-fixes/pypostprocessing.prototypes.yafc.lua"), "*", "turd-fix");

        if (itemExists) {
            Assert.Equal((double)(stackSize ?? 1), item["stack_size"]);
        }
        else {
            Assert.Null(items["hidden-beacon-turd"]);
        }
    }

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
