using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Yafc.Parser;

namespace Yafc.Model.Tests;

[Collection("LuaDependentTests")]
public class FactorioCompatibilityTests {
    [Fact]
    public void ResearchCountFormula_UsesInitialLevelInsteadOfPlaceholder() {
        LuaDependentTestHelper.GetProjectForLua();
        var technologies = Database.technologies.all.ToDictionary(t => t.name);
        Assert.Equal(2000, technologies["infinite-7"].count);
        Assert.Equal(20, technologies["omnipressed-infinite-7"].count);
        Assert.Equal(1, technologies["no-suffix"].count);
        Assert.Equal(42, technologies["fixed-9"].count);
    }

    [Fact]
    public void HeatingBoilersUseMaximumFluidTemperature() {
        LuaDependentTestHelper.GetProjectForLua(factorioVersion: new(2, 1, 20));
        var recipe = Database.recipes.all.Single(r => r.name.StartsWith("boiler.salt-heater."));
        Assert.Equal(5000, Assert.IsType<Fluid>(recipe.products.Single().goods).temperature);
        Assert.Equal(0.15f, recipe.ingredients.Single().amount, 5);
        Assert.Equal(0.15f, recipe.products.Single().amount, 5);
        Assert.DoesNotContain(Database.recipes.all, r => r.name.StartsWith("boiler.no-heating."));
    }

    [Fact]
    public void CompressedRecipe_RequiresOriginalUnlockAndCompressionTechnology() {
        var original = new Technology { id = (FactorioId)1 };
        var gate = new Technology { id = (FactorioId)2 };
        var gateVariant = new Technology { id = (FactorioId)3 };
        var recipe = new Recipe {
            ingredients = [], products = [],
            crafters = [new EntityCrafter { id = (FactorioId)0 }],
            technologyUnlock = [original], additionalTechnologyUnlock = [gate, gateVariant],
        };
        var dependencies = recipe.GetDependencies();
        Assert.False(dependencies.IsAccessible(id => (int)id is 0 or 1));
        Assert.False(dependencies.IsAccessible(id => (int)id is 0 or 2));
        Assert.True(dependencies.IsAccessible(id => (int)id is 0 or 1 or 2));
        Assert.True(dependencies.IsAccessible(id => (int)id is 0 or 1 or 3));
        recipe.enabled = true;
        Assert.False(recipe.GetDependencies().IsAccessible(id => (int)id == 0));
        Assert.True(recipe.GetDependencies().IsAccessible(id => (int)id is 0 or 2));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OmniFix_CopiesUnlocksWithoutEnablingRecipesOrUnavailableTiers(bool unlockAll) {
        using var context = new LuaContext(new(2, 1, 20));
        context.Exec(Encoding.UTF8.GetBytes("""
            settings = {startup = {omnicompression_building_levels = {value = 1}}}
            data = {raw = {
                recipe = {
                    iron = {enabled = false}, copper = {},
                    ["iron-compression"] = {enabled = false, categories = {"smelting-compressed"}},
                    ["copper-compression"] = {enabled = false, categories = {"smelting-compressed"}},
                    ["iron-compressed-compact"] = {enabled = false, categories = {"crafting-compressed"}},
                    ["iron-compressed-nanite"] = {enabled = false, categories = {"crafting-compressed"}},
                    ["foreign-compression"] = {enabled = false, categories = {"crafting"}},
                    foreign = {}, machine = {enabled = false}, disabled = {enabled = false},
                },
                technology = {
                    original = {effects = {{type = "unlock-recipe", recipe = "iron"}}},
                    ["compression-recipes"] = {effects = {{type = "unlock-recipe", recipe = "machine"}}},
                    ["compression-disabled"] = {enabled = false, effects = {{type = "unlock-recipe", recipe = "disabled"}}},
                    ["omnipressed-compression-recipes"] = {},
                    ["compression-compact-buildings"] = {}, ["compression-nanite-buildings"] = {},
                },
            }}
            """), "*", "omni-data");
        context.Exec(Encoding.UTF8.GetBytes("settings.startup.omnicompression_unlock_all = {value = " + (unlockAll ? "true" : "false") + "}"), "*", "omni-setting");
        context.Exec(File.ReadAllBytes("Data/Mod-fixes/omnimatter_compression.data-final-fixes.lua"), "*", "omni-fix");
        context.Exec(Encoding.UTF8.GetBytes("""
            local recipes, techs = data.raw.recipe, data.raw.technology
            assert(not recipes["iron-compression"].enabled)
            assert(recipes["copper-compression"].enabled)
            if settings.startup.omnicompression_unlock_all.value then
                assert(#recipes["iron-compression"].yafc_additional_technology_unlock == 0)
                assert(#recipes["iron-compressed-compact"].yafc_additional_technology_unlock == 0)
                assert(recipes.machine.enabled)
            else
                assert(recipes["iron-compression"].yafc_additional_technology_unlock[1] == "compression-recipes")
                assert(recipes["iron-compression"].yafc_additional_technology_unlock[2] == "omnipressed-compression-recipes")
                assert(recipes["iron-compressed-compact"].yafc_additional_technology_unlock[1] == "compression-compact-buildings")
                assert(not recipes.machine.enabled)
            end
            assert(not recipes.disabled.enabled)
            assert(not recipes["iron-compressed-nanite"].yafc_additional_technology_unlock)
            assert(not recipes["foreign-compression"].yafc_additional_technology_unlock)
            local unlocked = {}
            for _, effect in pairs(techs.original.effects) do unlocked[effect.recipe] = true end
            assert(unlocked["iron-compression"] and unlocked["iron-compressed-compact"])
            assert(not unlocked["iron-compressed-nanite"])
            """), "*", "omni-check");
    }

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
