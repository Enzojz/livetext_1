local pipe = require "livetext/pipe"
local func = require "livetext/func"
local coor = require "livetext/coor"
local bit32 = bit32
local unpack = table.unpack
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
            return convert(rs / val, unpack(rest))
        end
    end
    return convert(pipe.new, str:byte(1, -1))
end

local tryLoad = function(font, style)
    local facename = (style and font .. "_" .. style or font):lower()
    local r, i = xpcall(
            require,
            function(e) print ("not able to find font " .. facename .. ", fallback used") end,
            "livetext/" .. facename
        )
    return r, i
end

local gen = function(font, style, color)
    local facename, color, abc, kern = (function()
        local facename = (style and font .. "_" .. style or font):lower()
        local r, i = tryLoad(font, style)
        if (r and i) then
            return facename, color, unpack(i)
        else
            local r, i = tryLoad(font)
            if (r and i) then
                return facename:lower(), color, unpack(i)
            else
                return  "lato", "CFFFFFF", unpack(require("livetext/lato"))
            end
        end
    end)()
    local path = "livetext/" .. facename .. "/" .. color .. "/"
    return function(scale, z, twoSide)
        local scale = scale or 1
        local z = z or 0
        return function(text)
            local result = utf2unicode(text)
                * pipe.fold(pipe.new, 
                function(r, c)
                    local c = abc[c] and c or 32
                    local abc = abc[c] or {a = 0, b = scale, c = 0} --fallback
                    local kern = kern[c]
                    local lastPos = #r > 0 and r[#r].to or 0
                    local pos = lastPos + abc.a + (#r > 0 and kern and kern[r[#r].c] or 0)
                    local nextPos = pos + abc.b + abc.c
                    return r / {c = c, from = pos, to = nextPos}
                end)
            if (#result > 0) then
                local width = result[#result].to * scale
                return 
                    function(fTrans) return func.map(result, function(r)
                        return {
                            id = path .. tostring(r.c) .. ".mdl",
                            transf = coor.transX(r.from) * coor.scale(coor.xyz(scale, scale, scale)) * coor.transZ(z * scale) * (fTrans(width) or coor.I())
                        }
                    end)
                end, width
            else
                return false, false
            end
        end
    end
end

return gen
