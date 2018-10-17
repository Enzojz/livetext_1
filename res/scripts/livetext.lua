local pipe = require "livetext/pipe"
local func = require "livetext/func"
local coor = require "livetext/coor"

local bit32 = bit32
local band = bit32.band
local lshift = bit32.lshift
local bor = bit32.bor

local function utf2unicode(str)
    local function continue(val, c, ...)
        if (c and band(c, 0xC0) == 0x80) then
            return continue(bor(lshift(val, 6), band(c, 0x3F)), ...)
        else
            return val, {c, ...}
        end
    end
    local function convert(rs, c, ...)
        if (c == nil) then return rs
        elseif (c < 0x80) then
            return convert(rs / c, ...)
        else
            local lGr = c < 0xE0 and 2
                or c < 0xF0 and 3
                or c < 0xF8 and 4
                or error("invalid UTF-8 character sequence")
            local val, rest = continue(band(c, 2 ^ (8 - lGr) - 1), ...)
            return convert(rs / val, table.unpack(rest))
        end
    end
    return convert(pipe.new, str:byte(1, -1))
end

local gen = function(font, style)
    local facename = (style and font .. "_" .. style or font):lower()
    
    local abc, kern = table.unpack(require("livetext/" .. facename))
    return function(scale, z, twoSide)
        local scale = scale or 1
        local z = z or 0
        return function(text)
            local result = utf2unicode(text)
            * pipe.fold(pipe.new, 
            function(r, c)
                local lastPos = #r > 0 and r[#r].to or 0
                local abc = abc[c]
                local kern = kern[c]
                local pos = lastPos + abc.a + (#r > 0 and kern and kern[r[#r].c] or 0)
                local nextPos = pos + abc.b + abc.c
                return r / {c = c, from = pos, to = nextPos}
            end)

            return 
                function(transf) return func.map(result, function(r)
                    return {
                        id = "livetext/" .. facename .. "/" .. tostring(r.c) .. ".mdl",
                        transf = coor.transX(r.from * 0.01) * coor.transZ(z * 0.1) * coor.scale(coor.xyz(scale, scale, scale)) * (transf or coor.I())
                    }
                end)
            end, result[#result].to * scale * 0.01
        end
    end
end

return gen
