data = {raw = {
    planet = {nauvis = {type = "planet", name = "nauvis"}},
    technology = {},
}}
local units = {
    ["infinite-7"] = {count_formula = "2^(L-6)*1000"},
    ["omnipressed-infinite-7"] = {count_formula = "(2^(l-6)*1000)*0.01"},
    ["no-suffix"] = {count_formula = "L*l"},
    ["fixed-9"] = {count = 42},
}
for name, unit in pairs(units) do
    unit.time = 1
    unit.ingredients = {}
    data.raw.technology[name] = {type = "technology", name = name, unit = unit}
end
defines.prototypes = {entity = {}, item = {}, ["space-location"] = {planet = 0}}
