-- Py's YAFC-only TURD item needs a stack size before other mods process it.
local item = data.raw.item["hidden-beacon-turd"]
if item and item.stack_size == nil then
    item.stack_size = 1
end

return ...
