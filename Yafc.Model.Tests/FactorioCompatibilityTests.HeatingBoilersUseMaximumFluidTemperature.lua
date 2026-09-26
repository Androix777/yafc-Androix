data = {raw = {
    planet = {nauvis = {type = "planet", name = "nauvis"}},
    fluid = {salt = {
        type = "fluid", name = "salt", default_temperature = 1000,
        max_temperature = 5000, heat_capacity = "1kJ",
    }},
    boiler = {
        ["salt-heater"] = {
            type = "boiler", name = "salt-heater", mode = "heat-fluid-inside",
            fluid_box = {filter = "salt"}, energy_consumption = "600kW",
            energy_source = {type = "void"},
        },
        ["no-heating"] = {
            type = "boiler", name = "no-heating", mode = "output-to-separate-pipe",
            target_temperature = 1000, fluid_box = {filter = "salt"},
            output_fluid_box = {filter = "salt"}, energy_consumption = "600kW",
            energy_source = {type = "void"},
        },
    },
}}
defines.prototypes = {
    entity = {boiler = 0}, item = {}, fluid = {fluid = 0},
    ["space-location"] = {planet = 0},
}
