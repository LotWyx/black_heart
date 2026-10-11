-- Фон боя 320x180, 3 кадра: кровавая луна над морем (по референсу).
-- Чёрное небо в тяжёлых облаках, подсвеченных луной; луна истекает кровью в море;
-- тёмный мыс с маячком; красные волны с отражением; тёмные скалы впереди (под надписями HUD).
local L = dofile("/home/lotwy/black_heart/Art/Source/bh_lib.lua")

local function hex(h)
  return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) }
end

local P = {
  -- небо
  ["1"] = "#050507", ["2"] = "#0A090C", ["3"] = "#110D12",
  -- облака: тень → подсвеченный луной край
  q = "#16111A", t = "#241A20", u = "#3A2A2A", x = "#5A4038", z = "#7A5644",
  -- луна: тёмная сторона → яркий край
  A = "#4A0A10", F = "#7E1218", H = "#B0201E", J = "#D8402C", L = "#F07A50", Q = "#FFD2B4",
  -- мыс
  X = "#08060A", Z = "#2A0C10",
  -- море: впадины → гребни и блики
  ["4"] = "#1A0306", ["5"] = "#34060A", ["6"] = "#5A0A10", ["7"] = "#86141A", ["8"] = "#B42420", ["9"] = "#E04A34",
  -- скалы
  a = "#0E0C10", b = "#1C1820", c = "#2E2830", d = "#463E46", e = "#6A1A1C",
}
for key, value in pairs(P) do L.COLORS[key] = hex(value) end

local W, H = 320, 180
local HORIZON = 104
local MX, MY, MR = 100, 44, 27

-- ---------- шум для облаков и волн ----------
local function hash(ix, iy, seed)
  local n = ix * 374761393 + iy * 668265263 + seed * 1442695041
  n = (n ~ (n >> 13)) * 1274126177
  n = n ~ (n >> 16)
  return (n & 0xffff) / 65535
end

local function smooth(t) return t * t * (3 - 2 * t) end

local function noise(x, y, seed)
  local ix, iy = math.floor(x), math.floor(y)
  local fx, fy = smooth(x - ix), smooth(y - iy)
  local a, b = hash(ix, iy, seed), hash(ix + 1, iy, seed)
  local c, d = hash(ix, iy + 1, seed), hash(ix + 1, iy + 1, seed)
  return (a + (b - a) * fx) + ((c + (d - c) * fx) - (a + (b - a) * fx)) * fy
end

local function fbm(x, y, seed)
  local sum, amp, norm = 0, 1, 0
  for o = 0, 3 do
    sum = sum + noise(x * 2 ^ o, y * 2 ^ o, seed + o) * amp
    norm = norm + amp
    amp = amp * 0.5
  end
  return sum / norm
end

local function dither(x, y) return (x + y) % 2 == 0 end

-- ---------- статичная часть: небо, облака, луна, мыс ----------
local sky = L.grid(W, H)

for y = 0, HORIZON - 1 do
  for x = 0, W - 1 do
    local c = y < 30 and "1" or (y < 70 and "2" or "3")
    if (y == 29 or y == 69) and dither(x, y) then c = (y == 29) and "2" or "3" end
    L.set(sky, x, y, c)
  end
end

-- облака: плотные массы; край светлеет там, где плотность падает в сторону луны
local function density(x, y)
  local band = math.exp(-((y - 48) / 34) ^ 2)
  return fbm(x / 52, y / 18, 11) * (0.5 + 0.7 * band)
end
for y = 0, HORIZON - 3 do
  for x = 0, W - 1 do
    local v = density(x, y)
    if v > 0.42 then
      local dx, dy = MX - x, MY - y
      local dist = math.max(1, math.sqrt(dx * dx + dy * dy))
      local light = (v - density(x + dx / dist * 4, y + dy / dist * 4)) * 14
      local near = math.max(0, 1 - dist / 170)
      local lvl = light * (0.35 + near)
      local c = lvl > 0.75 and near > 0.35 and "z" or lvl > 0.45 and "x" or lvl > 0.2 and "u" or lvl > 0.02 and "t" or "q"
      if v < 0.445 and not dither(x, y) then c = nil end   -- рыхлый край облака
      if c then L.set(sky, x, y, c) end
    end
  end
end

-- красноватое свечение вокруг луны
for y = MY - MR - 9, MY + MR + 9 do
  for x = MX - MR - 9, MX + MR + 9 do
    local d = math.sqrt((x + 0.5 - MX) ^ 2 + (y + 0.5 - MY) ^ 2)
    if d > MR and d < MR + 4 and dither(x, y) then L.set(sky, x, y, "A")
    elseif d >= MR + 4 and d < MR + 8 and x % 2 == 0 and y % 2 == 0 then L.set(sky, x, y, "A") end
  end
end

-- луна: освещена слева, тёмная сторона справа, кратеры, яркий серп по левому краю
local craters = { {0.25, -0.2, 0.22}, {0.45, 0.3, 0.18}, {-0.05, 0.45, 0.14}, {0.1, -0.55, 0.12}, {0.6, -0.1, 0.1}, {-0.3, 0.15, 0.1} }
local lx, ly, lz = -0.8, -0.25, 0.55
local ll = math.sqrt(lx * lx + ly * ly + lz * lz)
lx, ly, lz = lx / ll, ly / ll, lz / ll
for y = MY - MR, MY + MR do
  for x = MX - MR, MX + MR do
    local dx, dy = (x + 0.5 - MX) / MR, (y + 0.5 - MY) / MR
    local r2 = dx * dx + dy * dy
    if r2 <= 1 then
      local dz = math.sqrt(1 - r2)
      local s = dx * lx + dy * ly + dz * lz
      for _, cr in ipairs(craters) do
        if (dx - cr[1]) ^ 2 + (dy - cr[2]) ^ 2 < cr[3] ^ 2 then s = s - 0.22 end
      end
      local c = s > 0.78 and "L" or s > 0.5 and "J" or s > 0.22 and "H" or s > -0.05 and "F" or "A"
      if math.sqrt(r2) > 1 - 1.3 / MR and dx < -0.5 then c = "Q" end
      L.set(sky, x, y, c)
    end
  end
end

-- мыс слева с маячком
L.poly(sky, { {0,90}, {14,86}, {30,88}, {48,92}, {70,96}, {96,100}, {126,104}, {0,104} }, "X")
L.each(sky, function(x, y, c)
  if c == "X" and y > 0 and L.get(sky, x, y - 1) ~= "X" then L.set(sky, x, y, "Z") end
end)
L.rect(sky, 60, 85, 61, 93, "X")

-- ---------- кадры: море, кровавая струя, огонёк маяка ----------
local function frame(f)
  local g = L.copy(sky)

  -- море: горизонтальные волны (растянутый шум), у зрителя крупнее и быстрее;
  -- яркие тона — только в дорожке отражения под луной
  for y = HORIZON, H - 1 do
    local p = (y - HORIZON) / (H - HORIZON)
    local sxScale, syScale = 5 + p * 26, 0.9 + p * 2.2
    for x = 0, W - 1 do
      local n1 = noise((x + f * (2 + p * 6)) / sxScale, y / syScale, 5)
      local n2 = noise((x - f * 3) / (sxScale * 0.5), y / (syScale * 0.6), 9)
      local wave = n1 * 0.7 + n2 * 0.3
      local refl = math.max(0, 1 - math.abs(x - (MX + 2)) / (4 + p * 42))
      local v = wave * 2.0 + p * 0.55 + refl * 1.9
      local c = v < 0.9 and "4" or v < 1.4 and "5" or v < 1.9 and "6" or v < 2.5 and "7" or v < 3.1 and "8" or "9"
      if y == HORIZON then c = (math.abs(x - MX - 2) < 14) and "8" or "5" end
      L.set(g, x, y, c)
    end
  end

  -- кровь стекает с луны в море; капли бегут вниз от кадра к кадру
  local sx, top = MX + 3, MY + MR - 1
  for y = top, HORIZON - 1 do
    L.set(g, sx, y, "H")
    L.set(g, sx - 1, y, y < top + 12 and "J" or "F")
  end
  local len = HORIZON - top
  for k = 0, 2 do
    local dy = (k * 11 + f * 4) % len
    L.set(g, sx, top + dy, "L")
  end
  for _, s in ipairs({ {-2,0}, {2,0}, {-1,-1}, {1,-1}, {-3,1}, {3,1} }) do   -- всплеск у горизонта
    if (s[1] + f) % 2 == 0 then L.set(g, sx + s[1], HORIZON + s[2], "J") end
  end

  -- огонёк маяка мигает
  L.set(g, 60, 84, f == 1 and "X" or "J")
  L.set(g, 61, 84, f == 1 and "X" or "H")

  -- скалы на переднем плане: тёмные, с красной кромкой сверху
  local rocks = L.grid(W, H)
  L.poly(rocks, { {58,180}, {68,165}, {86,156}, {110,153}, {128,159}, {142,170}, {150,180} }, "x")
  L.poly(rocks, { {128,180}, {142,166}, {160,159}, {186,157}, {206,162}, {224,171}, {232,180} }, "x")
  L.poly(rocks, { {214,180}, {230,171}, {250,169}, {266,175}, {272,180} }, "x")
  L.each(rocks, function(x, y)
    local above1, above2 = L.get(rocks, x, y - 1), L.get(rocks, x, y - 2)
    local c
    if not above1 then c = "e"
    elseif not above2 then c = "d"
    else
      local n = noise(x / 6, y / 4, 23)
      c = n > 0.62 and "c" or (n < 0.3 and "a" or "b")
    end
    L.set(g, x, y, c)
  end)

  return g
end

L.save("Background/BloodMoon", { frame(0), frame(1), frame(2) }, { 0.25 })
