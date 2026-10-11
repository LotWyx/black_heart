-- Душа (16x16): база, пульсация, удар, трещины, осколки
local L = dofile("/home/lotwy/black_heart/Art/Source/bh_lib.lua")

local HEART = {
  ".RRR....RRR.",
  "RRRRR..RRRRR",
  "RRRRRRRRRRRR",
  "RRRRRRRRRRRR",
  "RRRRRRRRRRRR",
  ".RRRRRRRRRR.",
  "..RRRRRRRR..",
  "...RRRRRR...",
  "....RRRR....",
  ".....RR.....",
}
local OX, OY = 2, 3   -- тело 12x10 -> с контуром 14x12 по центру 16x16

-- «Удар» сердца: на 1 px шире и выше
local BEAT = {
  ".RRRR...RRRR..",
  "RRRRRR.RRRRRR.",
  "RRRRRRRRRRRRRR",
  "RRRRRRRRRRRRRR",
  "RRRRRRRRRRRRRR",
  ".RRRRRRRRRRRR.",
  "..RRRRRRRRRR..",
  "...RRRRRRRR...",
  "....RRRRRR....",
  ".....RRRR.....",
  "......RR......",
}

local function hex(h) return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) } end
L.COLORS.hl = hex("#ff8a96")   -- светлая доля
L.COLORS.hd = hex("#5e0a1c")   -- глубокая тень
local BAYER = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 }
local function dither(x, y, d) return (BAYER[(y % 4) * 4 + (x % 4) + 1] + 0.5) / 16 < d end

-- Тон по диагонали (свет сверху-слева) с дизерингом на переходах; light — сдвиг к светлому (пульс)
local function heart(light, fill, shape)
  local body = L.rows(shape or HEART)
  local w, h = #(shape or HEART)[1], #(shape or HEART)
  L.each(body, function(x, y)
    if fill then L.set(body, x, y, fill); return end
    local t = (x / (w - 1) + y / (h - 1)) / 2 - (light or 0) * 0.08
    local c
    if t < 0.22 then c = dither(x, y, t / 0.22) and "R" or "hl"
    elseif t < 0.5 then c = "R"
    elseif t < 0.72 then c = dither(x, y, (t - 0.5) / 0.22) and "r" or "R"
    else c = dither(x, y, (t - 0.72) / 0.2) and "hd" or "r" end
    L.set(body, x, y, c)
  end)
  if not fill then
    -- блик: пятно 2x2 и искорка на левой доле
    L.set(body, 2, 1, "W"); L.set(body, 3, 1, "W"); L.set(body, 2, 2, "W"); L.set(body, 4, 2, "hl")
  end
  local g = L.grid(16, 16)
  L.blit(g, body, shape and OX - 1 or OX, shape and OY - 1 or OY)
  return L.outline(g, 1, "K")
end

local base = heart(0)
L.save("Player/Soul", { base })
L.save("Player/Soul_pulse", {
  base,
  heart(1, nil, BEAT),   -- удар: сердце на миг больше и светлее
  heart(0.5),
}, { 0.4 })
L.save("Player/Soul_hit", { heart(0, "W"), heart(0, "P") }, { 0.1 })

-- Трещины от правой верхней доли к центру
local broken = heart(0)
local function crack(pts) for _, p in ipairs(pts) do L.set(broken, p[1] + OX, p[2] + OY, "K") end end
crack({ {9,0}, {9,1}, {8,2}, {8,3}, {7,4}, {6,5}, {6,6}, {5,7} })
crack({ {8,2}, {9,3}, {10,4}, {10,5} })
crack({ {7,4}, {6,3}, {5,3} })
L.save("Player/SoulBroken", { broken })

-- Осколки 5x5
local shards = {
  L.rows({ "..K..", ".KWK.", ".KRK.", "KRrrK", "KKKKK" }),
  L.rows({ ".KKK.", "KWRRK", "KRRrK", "KRrK.", "KKK.." }),
  L.rows({ "KKKKK", "KWRrK", ".KRK.", ".KrK.", "..K.." }),
  L.rows({ "...KK", "..KWK", ".KRK.", "KrK..", "KK..." }),
  L.rows({ "KK...", "KWK..", ".KRKK", "..KrK", "...KK" }),
  L.rows({ ".....", ".KKK.", "KWRrK", ".KKK.", "....." }),
}
L.save("Player/SoulShard", { shards[1] })
L.save("Player/SoulShard_set", shards)
