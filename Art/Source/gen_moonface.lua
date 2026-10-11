-- Луноликий (босс обучения): из присланного спрайта 64x64 (Moonface_native.png)
-- строятся кадры на холсте 80x80: idle, замах, урон, смех, злость, исчезновение.
local L = dofile("/home/lotwy/black_heart/Art/Source/bh_lib.lua")
local pc = app.pixelColor

local function hex(h)
  return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) }
end

-- Цвета исходного спрайта -> метки (не пересекаются с метками библиотеки)
local SOURCE = {
  ["08080C"] = "k", ["222228"] = "d", ["FAFAFA"] = "f", ["C0C0C6"] = "i", ["808088"] = "m",
  ["484850"] = "n", ["DEDEE2"] = "e", ["C41C24"] = "a", ["820C18"] = "b", ["E82424"] = "l",
  ["EE4634"] = "h", ["46040C"] = "c", ["FF966E"] = "j",
}
for hexcode, label in pairs(SOURCE) do L.COLORS[label] = hex("#" .. hexcode) end

local S = 80
local OX, OY = 5, 10                    -- содержимое 56x66 (с достроенной шляпой) по центру холста 80x80
-- Координаты исходника 64x64 переводим в координаты холста
local EYES = { {23 + OX, 12 + OY}, {32 + OX, 12 + OY} }                 -- левый верхний угол глаз 5x5
local FLAME = { x0 = 46 + OX, y0 = 5 + OY, x1 = 63 + OX, y1 = 16 + OY }  -- языки пламени и искры

-- ---------- загрузка ----------
local src = Image{ fromFile = "/home/lotwy/black_heart/Art/Source/Moonface_native.png" }
local base = L.grid(S, S)
for y = 0, src.height - 1 do
  for x = 0, src.width - 1 do
    local c = src:getPixel(x, y)
    if pc.rgbaA(c) > 0 then
      local key = string.format("%02X%02X%02X", pc.rgbaR(c), pc.rgbaG(c), pc.rgbaB(c))
      L.set(base, x + OX, y + OY, assert(SOURCE[key], "unknown color " .. key))
    end
  end
end

-- В исходнике шляпа обрезана краем картинки: достраиваем тулью на 2 пикселя
-- (теми же тонами, что верхняя строка: тень, блик, тёмная основа, светлый край) и закрываем контуром
local CROWN = "kddnwddddddddmgk"   -- строка 0 исходника, x 21..36
for i = 1, 2 do
  for dx = 0, #CROWN - 1 do L.set(base, 21 + dx + OX, OY - i, CROWN:sub(dx + 1, dx + 1)) end
end
for x = 22, 35 do L.set(base, x + OX, OY - 3, "k") end   -- верх тульи, углы скруглены

-- ---------- операции ----------
local function inEye(x, y)
  for _, e in ipairs(EYES) do
    if x >= e[1] and x <= e[1] + 4 and y >= e[2] and y <= e[2] + 4 then return true end
  end
  return false
end

-- Внешний контур: тёмные пиксели, граничащие с пустотой
local function outerOutline(g)
  local set = {}
  L.each(g, function(x, y, c)
    if c == "k" then
      for _, o in ipairs({ {1,0}, {-1,0}, {0,1}, {0,-1} }) do
        if not L.get(g, x + o[1], y + o[2]) then set[y * S + x] = true; break end
      end
    end
  end)
  return set
end
local OUTER = outerOutline(base)

local function withOuter(g, label)
  local n = L.copy(g)
  for key in pairs(OUTER) do
    local x, y = key % S, key // S
    if L.get(n, x, y) then L.set(n, x, y, label) end
  end
  return n
end

-- Перекраска только внутри глаз
local function eyes(g, map)
  local n = L.copy(g)
  L.each(g, function(x, y, c) if inEye(x, y) and map[c] then L.set(n, x, y, map[c]) end end)
  return n
end

-- Мерцание пламени: языки и искры сдвигаются на 1 px вправо
local function flicker(g)
  local n = L.copy(g)
  local moved = {}
  for y = FLAME.y0, FLAME.y1 do
    for x = FLAME.x0, FLAME.x1 do
      local c = L.get(g, x, y)
      if c then moved[#moved + 1] = { x + 1, y, c }; L.set(n, x, y, nil) end
    end
  end
  for _, m in ipairs(moved) do L.set(n, m[1], m[2], m[3]) end
  return n
end

-- Прищур от смеха: глаза-дуги «^»
local SQUINT = { "aaaaa", "aakaa", "akfka", "kfafk", "aaaaa" }
local function squint(g)
  local n = L.copy(g)
  for _, e in ipairs(EYES) do
    for dy = 0, 4 do
      for dx = 0, 4 do L.set(n, e[1] + dx, e[2] + dy, SQUINT[dy + 1]:sub(dx + 1, dx + 1)) end
    end
  end
  return n
end

-- Белый силуэт: всё белое, кроме внешнего контура и зрачков
local function whiteSilhouette(g)
  local n = L.grid(S, S)
  L.each(g, function(x, y, c)
    local keep = OUTER[y * S + x] or (inEye(x, y) and c == "k")
    L.set(n, x, y, keep and "K" or "W")
  end)
  return n
end

-- ---------- кадры ----------
local minx, miny, maxx, maxy = L.bounds(base)
local cx, cy = (minx + maxx) // 2, (miny + maxy) // 2

local idle = { base, L.shift(flicker(base), 0, -1) }

local windup = {
  withOuter(base, "O"),
  L.outline(L.fillAll(L.outline(base, 2, "O"), "O"), 1, "K"),
  eyes(base, { l = "O", k = "W" }),          -- радужка вспыхивает оранжевым, зрачок белым
}

local hurt = { whiteSilhouette(base), eyes(base, { f = "D" }) }

local laughBase = squint(base)
local laugh = { laughBase, L.shift(flicker(laughBase), 0, -2), laughBase }

local angryBase = eyes(withOuter(base, "D"), { l = "D" })
local angry = { L.shift(angryBase, -1, 0), L.shift(flicker(angryBase), 1, 0) }

-- Исчезновение: белый силуэт -> вертикальная полоса -> короткая полоса -> точка 2x2
local function bar(halfHeight)
  local g = L.grid(S, S)
  L.rect(g, cx - 1, cy - halfHeight, cx + 2, cy + halfHeight, "W")
  return L.outline(g, 1, "K")
end
local dot = L.grid(S, S)
L.rect(dot, cx, cy, cx + 1, cy + 1, "W")
local vanish = { whiteSilhouette(base), bar((maxy - miny) // 2), bar((maxy - miny) // 5), dot }

L.save("Enemies/Moonface", { base })
L.save("Enemies/Moonface_idle", idle, { 0.4 })
L.save("Enemies/Moonface_windup", windup, { 0.1 })
L.save("Enemies/Moonface_hurt", hurt, { 0.05 })
L.save("Enemies/Moonface_laugh", laugh, { 0.1 })
L.save("Enemies/Moonface_angry", angry, { 0.08 })
L.save("Enemies/Moonface_vanish", vanish, { 0.07 })
