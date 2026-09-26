-- Omnilib opens these recipes in control.lua, which YAFC does not execute.
-- Keep the original unlock and require the corresponding compression technology as well.
local tiers = {compact = 1, nanite = 2, quantum = 3, singularity = 4}
local levels = settings.startup["omnicompression_building_levels"].value
local variants = {}
local unlock_setting = settings.startup["omnicompression_unlock_all"]
local unlocked = {}
if unlock_setting and unlock_setting.value then
    for name, technology in pairs(data.raw.technology) do
        if name:match("^compression%-") and technology.enabled ~= false then
            unlocked[name] = true
            for _, effect in pairs(technology.effects or {}) do
                if effect.type == "unlock-recipe" and data.raw.recipe[effect.recipe] then
                    data.raw.recipe[effect.recipe].enabled = true
                end
            end
        end
    end
end

for name, recipe in pairs(data.raw.recipe) do
    local original = name:match("^(.*)%-compression$")
    local gate = "compression-recipes"
    if not original then
        local tier
        original, tier = name:match("^(.*)%-compressed%-([^%-]+)$")
        if not tiers[tier] or tiers[tier] > levels then
            original = nil
        else
            gate = "compression-" .. tier .. "-buildings"
        end
    end
    local base = original and data.raw.recipe[original]
    local categories = recipe.categories or {recipe.category or "crafting"}
    local compressed_category = false
    for _, category in pairs(categories) do
        if category:match("%-compressed$") then compressed_category = true end
    end
    if base and compressed_category and data.raw.technology[gate] then
        recipe.enabled = base.enabled ~= false
        recipe.yafc_additional_technology_unlock = unlocked[gate] and {} or {gate}
        if not unlocked[gate] and data.raw.technology["omnipressed-" .. gate] then
            table.insert(recipe.yafc_additional_technology_unlock, "omnipressed-" .. gate)
        end
        variants[original] = variants[original] or {}
        table.insert(variants[original], name)
    end
end

for _, technology in pairs(data.raw.technology) do
    local effects = technology.effects or {}
    for i = 1, #effects do
        local effect = effects[i]
        if effect.type == "unlock-recipe" then
            for _, name in pairs(variants[effect.recipe] or {}) do
                table.insert(effects, {type = "unlock-recipe", recipe = name})
            end
        end
    end
end

return ...
