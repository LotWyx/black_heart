-- Луноликий (босс обучения): образ из присланного спрайта — джентльмен в цилиндре и фраке
-- с красным лицом-луной, широкой ухмылкой и белым пламенем в руке, — но в стиле новых врагов
-- (bh_silhouette.lua): тёмный силуэт с красной каймой, яркие только лицо, перчатки, манишка и пламя.
-- Кадры: base, idle, windup, hurt, laugh, angry, vanish. Вывод — Assets/Resources/Art/Enemies/,
-- превью на фоне зала — Art/Preview/Enemies/.
local V = dofile("/home/lotwy/black_heart/Art/Source/bh_silhouette.lua")
local S, L, W, H = V.S, V.L, V.W, V.H
local mask, part, paint, paintLine, render = V.mask, V.part, V.paint, V.paintLine, V.render
local OUT = "/home/lotwy/black_heart/Art/Preview/"

local function hex(h)
  return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) }
end
for k, v in pairs({
  mn1 = "#3a040c", mn2 = "#78101a", mn3 = "#b81c24", mn4 = "#e23426", mn5 = "#ff7a5a",   -- лицо-луна
  fw1 = "#8a8aa0", fw2 = "#cfd0e0", fw3 = "#f4f4ff",                                     -- белое пламя
}) do L.COLORS[k] = hex(v) end
V.MAT.moon = { kind = "bright", ramp = { "mn1", "mn2", "mn3", "mn4", "mn5" }, rim = { "mn5" } }

local function moonface(st)
  local parts = {}
  local by = st.bob or 0
  local hat = st.hatLift or 0
  local fl = st.flick or 0
  local laugh = (st.mouth or 0) > 0

  -- ноги и ботинки с белыми гетрами
  local legs = mask()
  L.poly(legs, { {34,55 + by}, {39,55 + by}, {38,71}, {34,71} }, "x")
  L.poly(legs, { {41,55 + by}, {46,55 + by}, {47,71}, {43,71} }, "x")
  part(parts, "legs", "cloth", 1, legs)
  local shoes = mask()
  L.ellipse(shoes, 35, 73, 4.5, 2, "x"); L.ellipse(shoes, 46, 73, 4.5, 2, "x")
  part(parts, "shoes", "cloth", 2, shoes)
  local spats = mask()
  L.rect(spats, 33, 69, 38, 71, "x"); L.rect(spats, 43, 69, 48, 71, "x")
  part(parts, "spats", "white", 3, spats, { noRim = true })

  -- фрак: корпус и раздвоенные фалды
  local coat = mask()
  L.poly(coat, { {31,33 + by}, {49,33 + by}, {51,44 + by}, {50,55 + by}, {30,55 + by}, {29,44 + by} }, "x")
  L.poly(coat, { {30,52 + by}, {37,52 + by}, {33,64 + by}, {28,62 + by} }, "x")
  L.poly(coat, { {43,52 + by}, {50,52 + by}, {52,62 + by}, {47,64 + by} }, "x")
  part(parts, "coat", "cloth", 4, coat)
  local shirt = mask()
  L.poly(shirt, { {36,33 + by}, {44,33 + by}, {40,45 + by} }, "x")
  part(parts, "shirt", "white", 5, shirt, { noRim = true })

  -- левая рука на трости, правая поднята с пламенем
  local cane = mask()
  L.line(cane, 24, 46 + by, 20, 74, "x", 2)
  L.line(cane, 25, 46 + by, 21, 74, "x", 1)
  part(parts, "cane", "horn", 2, cane)
  local knob = mask()
  L.ellipse(knob, 24.5, 45 + by, 2.4, 2.2, "x")
  part(parts, "knob", "white", 7, knob)
  local sleeves = mask()
  S.taper(sleeves, { 31, 36 + by }, { 26, 41 + by }, { 25, 47 + by }, 3.6, 3)
  S.taper(sleeves, { 49, 36 + by }, { 57, 40 + by }, { 59, 31 + by }, 3.6, 3)
  part(parts, "sleeve", "cloth", 6, sleeves)
  local gloves = mask()
  L.ellipse(gloves, 25, 48 + by, 3, 2.6, "x")
  L.ellipse(gloves, 59.5, 29 + by, 3, 2.8, "x")
  part(parts, "glove", "white", 8, gloves)

  -- голова-луна и цилиндр (в смехе цилиндр подпрыгивает)
  local head = mask()
  L.ellipse(head, 40, 25 + by, 9, 8.5, "x")
  part(parts, "face", "moon", 9, head)
  local hatM = mask()
  L.poly(hatM, { {33,3 + by - hat}, {47,2 + by - hat}, {47,16 + by - hat}, {33,16 + by - hat} }, "x")
  L.rect(hatM, 29, 16 + by - hat, 51, 18 + by - hat, "x")
  part(parts, "hat", "cloth", 10, hatM)

  local out, owner = render(parts)

  -- лента цилиндра, лацканы, бабочка, роза в петлице, пуговицы
  for x = 33, 47 do paint(out, owner, { hat = true }, x, 13 + by - hat, "rim2"); paint(out, owner, { hat = true }, x, 14 + by - hat, "mn2") end
  paintLine(out, owner, { coat = true }, { {36,33 + by}, {33,44 + by} }, "dk0")
  paintLine(out, owner, { coat = true }, { {44,33 + by}, {47,44 + by} }, "dk0")
  for _, p in ipairs({ {37,34}, {38,35}, {39,35}, {40,35}, {41,35}, {42,35}, {43,34}, {37,36}, {43,36} }) do
    paint(out, owner, { shirt = true }, p[1], p[2] + by, "K")
  end
  paint(out, owner, { shirt = true }, 40, 35 + by, "gem2")
  for _, p in ipairs({ {33,37,"gem2"}, {34,37,"gem3"}, {33,38,"gem1"}, {34,38,"gem2"}, {33,39,"rim1"} }) do
    paint(out, owner, { coat = true }, p[1], p[2] + by, p[3])
  end
  for _, yy in ipairs({ 47, 51 }) do paint(out, owner, { coat = true }, 40, yy + by, "bn3") end
  paintLine(out, owner, { coat = true }, { {40,45 + by}, {40,55 + by} }, "dk0")

  -- лицо: кратеры, квадратные глаза, ухмылка
  for _, p in ipairs({ {35,19}, {36,19}, {45,30}, {46,29}, {33,27}, {47,22} }) do paint(out, owner, { face = true }, p[1], p[2] + by, "mn2") end
  local eyes = {}
  for _, ex in ipairs({ 34, 42 }) do
    local ey = 20 + by
    if laugh then   -- прищур «^»
      for i, o in ipairs({ {0,3}, {1,2}, {2,1}, {3,2}, {4,3} }) do
        L.set(out, ex + o[1], ey + o[2], "sc5"); L.set(out, ex + o[1], ey + o[2] + 1, "K")
      end
    else
      for dy = 0, 4 do for dx = 0, 4 do
        local edge = dx == 0 or dy == 0 or dx == 4 or dy == 4
        L.set(out, ex + dx, ey + dy, edge and "sc4" or "ir2")
      end end
      L.set(out, ex + 2, ey + 2, "K"); L.set(out, ex, ey, "sc5")
      eyes[#eyes + 1] = { ex + 1, ey + 1 }; eyes[#eyes + 1] = { ex + 2, ey + 1 }; eyes[#eyes + 1] = { ex + 3, ey + 1 }
      eyes[#eyes + 1] = { ex + 1, ey + 2 }; eyes[#eyes + 1] = { ex + 3, ey + 2 }
      eyes[#eyes + 1] = { ex + 1, ey + 3 }; eyes[#eyes + 1] = { ex + 2, ey + 3 }; eyes[#eyes + 1] = { ex + 3, ey + 3 }
    end
  end
  local mh = laugh and 4 or 2
  for y = 27 + by, 27 + by + mh do
    for x = 33, 47 do
      local inside = (x - 40) ^ 2 / 49 + (y - 27 - by) ^ 2 / ((mh + 1) ^ 2) <= 1 or y == 27 + by
      if inside and x > 32 and x < 48 then L.set(out, x, y, "K") end
    end
  end
  for x = 34, 46, 2 do L.set(out, x, 27 + by, "sc4"); L.set(out, x + 1, 28 + by, "sc3") end   -- зубы-пилы
  if laugh then for x = 37, 43 do L.set(out, x, 29 + by + mh - 1, "mn3") end end              -- язык

  -- белое пламя над перчаткой: язычки и искры меняются по кадрам
  local fx, fy = 59, 27 + by + (laugh and -1 or 0)
  -- каплевидный язык пламени: ширина растёт книзу, верх колышется по кадрам
  local fh = laugh and 17 or 14
  for y = fy - fh, fy do
    local t = (y - (fy - fh)) / fh                     -- 0 — кончик, 1 — основание
    local w = 4.8 * math.sin(math.pi * (t ^ 0.65) * 0.92)
    local sway = math.sin(t * 5 + fl * 1.7) * 1.6 * (1 - t)
    for x = math.floor(fx - w + sway), math.ceil(fx + w + sway) do
      local dx = math.abs(x + 0.5 - fx - sway) / math.max(w, 0.5)
      if dx <= 1 then
        local col = (dx < 0.45 and t > 0.3) and "fw3" or (dx < 0.8 and t > 0.12) and "fw2" or "fw1"
        L.set(out, x, y, col)
      end
    end
  end
  -- оторвавшиеся язычки
  for i, tg in ipairs({ { -4, -10 }, { 4, -8 }, { 1, -fh - 3 } }) do
    if (fl + i) % 3 ~= 0 then
      L.set(out, fx + tg[1], fy + tg[2] - (fl % 2), "fw2"); L.set(out, fx + tg[1], fy + tg[2] + 1 - (fl % 2), "fw1")
    end
  end
  for i, sp in ipairs({ {65,10}, {53,13}, {66,18} }) do   -- красные искры
    if (fl + i) % 2 == 0 then L.set(out, sp[1], sp[2] + by, "gl1") end
  end
  return V.finish(out, eyes)
end

-- ---------- кадры ----------
local IDLE = { { flick = 0 }, { flick = 1, bob = -1 }, { flick = 2, bob = -1 }, { flick = 3 } }
local LAUGH = { { mouth = 1, hatLift = 2, flick = 1 }, { mouth = 1, hatLift = 4, bob = -2, flick = 2 }, { mouth = 1, hatLift = 2, flick = 3 } }

local base, meta = moonface({})
local idle, laugh = {}, {}
for i, st in ipairs(IDLE) do idle[i] = (moonface(st)) end
for i, st in ipairs(LAUGH) do laugh[i] = (moonface(st)) end

-- внешний контур (чёрные пиксели у пустоты) — для вспышек и силуэтов
local N4 = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }
local function outer(g)
  local set = {}
  L.each(g, function(x, y, c)
    if c == "K" then
      for _, o in ipairs(N4) do if not L.get(g, x + o[1], y + o[2]) then set[y * W + x] = true; break end end
    end
  end)
  return set
end
local function withOuter(g, label)
  local n, set = L.copy(g), outer(g)
  for key in pairs(set) do L.set(n, key % W, key // W, label) end
  return n
end
local function recolor(g, pts, label)
  local n = L.copy(g)
  for _, p in ipairs(pts) do L.set(n, p[1], p[2], label) end
  return n
end
local function whiteSilhouette(g, keep)
  local n, set, k = L.grid(W, H), outer(g), {}
  for _, p in ipairs(keep or {}) do k[p[2] * W + p[1]] = true end
  L.each(g, function(x, y) L.set(n, x, y, (set[y * W + x] or k[y * W + x]) and "K" or "W") end)
  return n
end

local windup = {
  withOuter(base, "O"),
  L.outline(L.fillAll(L.outline(base, 2, "O"), "O"), 1, "K"),
  recolor(base, meta.eyes, "O"),
}
local hurt = { whiteSilhouette(base, meta.eyes), recolor(base, meta.eyes, "W") }
local angryBase = recolor(withOuter(base, "D"), meta.eyes, "D")
local angry = { L.shift(angryBase, -1, 0), L.shift(recolor(withOuter(idle[3], "D"), meta.eyes, "D"), 1, 0) }

local minx, miny, maxx, maxy = L.bounds(base)
local cx, cy = (minx + maxx) // 2, (miny + maxy) // 2
local function bar(half)
  local g = L.grid(W, H)
  L.rect(g, cx - 1, cy - half, cx + 2, cy + half, "W")
  return L.outline(g, 1, "K")
end
local dot = L.grid(W, H)
L.rect(dot, cx, cy, cx + 1, cy + 1, "W")
local vanish = { whiteSilhouette(base), bar((maxy - miny) // 2), bar((maxy - miny) // 5), dot }

L.save("Enemies/Moonface", { base })
L.save("Enemies/Moonface_idle", idle, { 0.22 })
L.save("Enemies/Moonface_windup", windup, { 0.1 })
L.save("Enemies/Moonface_hurt", hurt, { 0.05 })
L.save("Enemies/Moonface_laugh", laugh, { 0.1 })
L.save("Enemies/Moonface_angry", angry, { 0.08 })
L.save("Enemies/Moonface_vanish", vanish, { 0.07 })

V.preview({ idle, laugh, windup, { hurt[1], hurt[2], angry[1], angry[2] }, vanish }, OUT .. "Enemies/preview_moonface.png")
print("ok")
