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

-- shadowDepth — толщина тени справа-снизу; hl = {x, y, w, h} блика в координатах тела
local function heart(shadowDepth, hl, fill)
  local body = L.rows(HEART)
  if fill then
    body = L.fillAll(body, fill)
  else
    body = L.shade(body, "R", "r", 1, 0, shadowDepth)
    body = L.shade(body, "R", "r", 1, 1, shadowDepth)
    if hl then L.rect(body, hl[1], hl[2], hl[1] + hl[3] - 1, hl[2] + hl[4] - 1, "W") end
  end
  local g = L.grid(16, 16)
  L.blit(g, body, OX, OY)
  return L.outline(g, 1, "K")
end

local base = heart(2, { 2, 1, 2, 2 })
L.save("Player/Soul", { base })
L.save("Player/Soul_pulse", {
  base,
  heart(3, { 3, 2, 1, 1 }),   -- вдох: блик меньше и ниже-правее, тени больше
  heart(1, { 1, 0, 3, 2 }),   -- выдох: блик больше и выше-левее, тени меньше
}, { 0.4 })
L.save("Player/Soul_hit", { heart(0, nil, "W"), heart(0, nil, "P") }, { 0.1 })

-- Трещины от правой верхней доли к центру
local broken = heart(2, { 2, 1, 2, 2 })
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
