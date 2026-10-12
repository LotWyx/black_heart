-- Пули (10x10): общая серая (перекрашивается кодом, запасная) и цветные по типам; трещины, искры, круг «бум-круга»
local L = dofile("/home/lotwy/black_heart/Art/Source/bh_lib.lua")

-- Круглая пуля: силуэт и тень постоянны, вращение видно по 4 серым меткам внутри
-- (4-кратная симметрия -> 4 кадра по 22.5° дают полный цикл)
local function bullet(angleDeg)
  local g = L.grid(10, 10)
  L.ellipse(g, 5, 5, 4, 4, "W")
  g = L.shade(g, "W", "g", 1, 1, 1)
  local rot = math.rad(angleDeg)
  for i = 0, 3 do
    local a = rot + i * math.pi / 2
    L.set(g, math.floor(5 + math.cos(a) * 2.2), math.floor(5 + math.sin(a) * 2.2), "g")
  end
  return L.outline(g, 1, "K")
end

local spin = { bullet(0), bullet(22.5), bullet(45), bullet(67.5) }
L.save("Projectiles/Bullet", { spin[1] })
L.save("Projectiles/Bullet_spin", spin, { 0.06 })

-- ---------- цветные пули по типам (10x10, 4 кадра вращения), уже в цвете — код их не перекрашивает ----------
local function hex(h) return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) } end
for k, v in pairs({
  wb1 = "#5a5a72", wb2 = "#a8a8c0", wb3 = "#e4e4f0", wb4 = "#ffffff",   -- белая жемчужина
  pb1 = "#3a145e", pb2 = "#6e2aa8", pb3 = "#a65ae0", pb4 = "#e2c2ff",   -- фиолетовая
  bb1 = "#0e2a66", bb2 = "#1e5ec0", bb3 = "#4aa8ff", bb4 = "#c8ecff",   -- синий кристалл
}) do L.COLORS[k] = hex(v) end
local BAYER = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 }
local function dither(x, y, d) return (BAYER[(y % 4) * 4 + (x % 4) + 1] + 0.5) / 16 < d end

-- Шар: тон по диагонали (свет сверху-слева) с дизерингом на переходах, блик 2 px
local function orb(r)
  local g = L.grid(10, 10)
  for y = 1, 8 do for x = 1, 8 do
    local dx, dy = x + 0.5 - 5, y + 0.5 - 5
    if dx * dx + dy * dy <= 16 then
      local t = (dx + dy) / 8 + 0.5   -- 0 — верх-лево, 1 — низ-право
      local c = r[3]
      if t > 0.62 then c = dither(x, y, (t - 0.62) / 0.25) and r[1] or r[2] end
      if t > 0.4 and t <= 0.62 then c = dither(x, y, (t - 0.4) / 0.22) and r[2] or r[3] end
      L.set(g, x, y, c)
    end
  end end
  L.set(g, 3, 3, r[4]); L.set(g, 4, 3, r[4]); L.set(g, 3, 4, r[4])
  return g
end

-- Белая: жемчужина с вращающейся искоркой-крестом
local function whiteBullet(a)
  local g = orb({ "wb1", "wb2", "wb3", "wb4" })
  local rot = math.rad(a)
  for i = 0, 3 do
    local ang = rot + i * math.pi / 2
    L.set(g, math.floor(5 + math.cos(ang) * 2.4), math.floor(5 + math.sin(ang) * 2.4), "wb2")
  end
  return L.outline(g, 1, "K")
end

-- Фиолетовая: шар со швом-трещиной посередине (подсказка: расколется на две)
local function purpleBullet(a)
  local g = orb({ "pb1", "pb2", "pb3", "pb4" })
  local rot = math.rad(a + 90)
  local cx, cy = math.cos(rot), math.sin(rot)
  for t = -3.5, 3.5, 0.5 do
    local x, y = math.floor(5 + cx * t + 0.0), math.floor(5 + cy * t + 0.0)
    if L.get(g, x, y) then L.set(g, x, y, "pb1") end
  end
  L.set(g, 3, 3, "pb4"); L.set(g, 4, 3, "pb4")
  return L.outline(g, 1, "K")
end

-- Синяя: граненый кристалл-ромб (рубить нельзя); блик бегает по граням
local BLUE = {
  "...a...",
  "..abc..",
  ".abbcc.",
  "abbbccd",
  ".bbbcd.",
  "..bcd..",
  "...d...",
}
local function blueBullet(f)
  local g = L.grid(10, 10)
  local map = { a = "bb4", b = "bb3", c = "bb2", d = "bb1" }
  for y, row in ipairs(BLUE) do for x = 1, #row do
    local ch = row:sub(x, x)
    if ch ~= "." then L.set(g, x, y, map[ch]) end   -- со сдвигом (1, 1): контур помещается в 10x10
  end end
  -- блик пробегает по левой верхней грани
  local glint = { { 3, 3 }, { 4, 2 }, { 2, 4 }, { 3, 4 } }
  local p = glint[f + 1]
  L.set(g, p[1], p[2], "bb4")
  return L.outline(g, 1, "K")
end

local typed = {
  White = { whiteBullet(0), whiteBullet(22.5), whiteBullet(45), whiteBullet(67.5) },
  Purple = { purpleBullet(0), purpleBullet(45), purpleBullet(90), purpleBullet(135) },
  Blue = { blueBullet(0), blueBullet(1), blueBullet(2), blueBullet(3) },
}
for name, frames in pairs(typed) do
  L.save("Projectiles/Bullet" .. name, { frames[1] })
  L.save("Projectiles/Bullet" .. name .. "_spin", frames, { 0.06 })
end

-- ---------- пули в стиле врага: Projectiles/<Враг>/Bullet<Тип>[_spin] ----------
-- Цвет по-прежнему задаёт тип (белая / фиолетовая / синяя) — меняется только форма под врага.
-- У фиолетовых всегда есть шов-трещина: подсказка, что пуля расколется.
local RAMPS = {
  White = { "wb1", "wb2", "wb3", "wb4" },
  Purple = { "pb1", "pb2", "pb3", "pb4" },
  Blue = { "bb1", "bb2", "bb3", "bb4" },
}

local function seam(g, r, f)
  local a = math.rad(45 + (f or 0) * 45)
  for t = -4, 4, 0.5 do
    local x, y = math.floor(4.5 + math.cos(a) * t + 0.5), math.floor(4.5 + math.sin(a) * t + 0.5)
    if L.get(g, x, y) and L.get(g, x, y) ~= "K" then L.set(g, x, y, r[1]) end
  end
end

-- Призрак: блуждающий огонёк с глазками, язычок пламени колышется
local WISP_TOP = {
  { "....4...", "...43...", "..3443..", ".334433." },
  { "...4....", "...34...", "..3443..", ".334433." },
  { ".....4..", "....34..", "..3443..", ".334433." },
  { "....4...", "..4.34..", "..3443..", ".334433." },
}
local WISP_BOTTOM = { "33444433", "33k44k33", "33333332", ".222222." }
local function wisp(r, f)
  local g = L.grid(10, 10)
  local map = { ["2"] = r[2], ["3"] = r[3], ["4"] = r[4], k = "K" }
  local rows = {}
  for _, row in ipairs(WISP_TOP[f + 1]) do rows[#rows + 1] = row end
  for _, row in ipairs(WISP_BOTTOM) do rows[#rows + 1] = row end
  for y, row in ipairs(rows) do for x = 1, #row do
    local ch = row:sub(x, x)
    if ch ~= "." then L.set(g, x, y, map[ch]) end
  end end
  if r == RAMPS.Purple then seam(g, r, 0) end
  return L.outline(g, 1, "K")
end

-- Глаз: глазное яблоко цвета типа, тёмная радужка, зрачок смотрит по сторонам
local function eyeball(r, f)
  local g = L.grid(10, 10)
  for y = 1, 8 do for x = 1, 8 do
    local dx, dy = x + 0.5 - 5, y + 0.5 - 5
    if dx * dx + dy * dy <= 15 then L.set(g, x, y, (dx + dy > 3) and r[2] or r[3]) end
  end end
  local look = ({ { 0, 0 }, { 1, 0 }, { 0, 1 }, { -1, 0 } })[f + 1]
  local ix, iy = 4 + look[1], 4 + look[2]
  for _, p in ipairs({ { 0, 0 }, { 1, 0 }, { 0, 1 }, { 1, 1 }, { -1, 0 }, { 2, 1 }, { 0, -1 }, { 1, 2 } }) do
    if L.get(g, ix + p[1], iy + p[2]) then L.set(g, ix + p[1], iy + p[2], r[1]) end
  end
  L.set(g, ix, iy, "K"); L.set(g, ix + 1, iy, "K"); L.set(g, ix, iy + 1, "K"); L.set(g, ix + 1, iy + 1, "K")
  L.set(g, 2, 3, r[4]); L.set(g, 3, 2, r[4])
  if r == RAMPS.Purple then seam(g, r, 2) end
  return L.outline(g, 1, "K")
end

-- Череп: маленький череп, клацает челюстью
local SKULL = {
  "..4444..",
  ".444443.",
  "44444433",
  "4kk44kk3",
  "4kk44kk3",
  ".44k443.",
  "..3333..",
}
local function skullBullet(r, f)
  local g = L.grid(10, 10)
  local map = { ["4"] = r[3], ["3"] = r[2], k = "K" }
  for y, row in ipairs(SKULL) do for x = 1, #row do
    local ch = row:sub(x, x)
    if ch ~= "." then L.set(g, x, y, map[ch]) end
  end end
  for x = 3, 6 do L.set(g, x, 8, r[2]) end                    -- челюсть
  if f % 2 == 1 then
    for x = 3, 6 do L.set(g, x, 7, "K") end                   -- клац: рот открыт
  else
    for x = 3, 6, 2 do L.set(g, x, 7, "K") end                -- зубы
  end
  L.set(g, 2, 2, r[4]); L.set(g, 3, 1, r[4])
  if r == RAMPS.Purple then seam(g, r, 0) end
  return L.outline(g, 1, "K")
end

-- Луноликий: полумесяц, поворачивается на 90° каждый кадр, на кончике искорка
local function crescent(r, f)
  local g = L.grid(10, 10)
  local a = math.rad(-45 + f * 90)
  local ox, oy = math.cos(a) * 2.8, math.sin(a) * 2.8
  for y = 1, 8 do for x = 1, 8 do
    local dx, dy = x + 0.5 - 5, y + 0.5 - 5
    local ex, ey = dx - ox, dy - oy
    if dx * dx + dy * dy <= 15 and ex * ex + ey * ey > 7 then
      L.set(g, x, y, (dx * ox + dy * oy < -3) and r[3] or r[2])
    end
  end end
  -- светлая кромка по внешней дуге
  L.each(g, function(x, y)
    local dx, dy = x + 0.5 - 5, y + 0.5 - 5
    if dx * dx + dy * dy > 10 and dx * ox + dy * oy < -1 then L.set(g, x, y, r[4]) end
  end)
  if r == RAMPS.Purple then seam(g, r, f * 2) end
  return L.outline(g, 1, "K")
end

local THEMES = { Ghost = wisp, Eye = eyeball, Skull = skullBullet, Moonface = crescent }
for theme, draw in pairs(THEMES) do
  for tname, ramp in pairs(RAMPS) do
    local frames = { draw(ramp, 0), draw(ramp, 1), draw(ramp, 2), draw(ramp, 3) }
    L.save("Projectiles/" .. theme .. "/Bullet" .. tname, { frames[1] })
    L.save("Projectiles/" .. theme .. "/Bullet" .. tname .. "_spin", frames, { 0.06 })
  end
end

-- Вспышка появления: белый квадрат 6x6 со срезанными углами + контур
local spawn = L.grid(10, 10)
L.rect(spawn, 2, 2, 7, 7, "W")
for _, p in ipairs({ {2,2}, {7,2}, {2,7}, {7,7} }) do L.set(spawn, p[1], p[2], nil) end
L.save("Projectiles/Bullet_spawn", { L.outline(spawn, 1, "K") })

-- Исчезновение: белый пиксель 2x2
local despawn = L.grid(10, 10)
L.rect(despawn, 4, 4, 5, 5, "W")
L.save("Projectiles/Bullet_despawn", { despawn })

-- Трещины (только чёрные линии) для осколков фиолетовой пули
local crack = L.grid(10, 10)
for _, p in ipairs({ {3,2}, {4,3}, {5,4}, {5,5}, {4,6}, {4,7}, {6,5}, {7,6} }) do L.set(crack, p[1], p[2], "K") end
L.save("Projectiles/BulletCrack", { crack })

-- Искры 3x3 без контура
L.save("FX/Sparks", {
  L.rows({ ".g.", "gWg", ".g." }),
  L.rows({ "g.g", ".W.", "g.g" }),
  L.rows({ ".W.", "WWW", ".W." }),
  L.rows({ "...", ".W.", "..g" }),
}, { 0.05 })

-- Круг 64x64: «полупрозрачная» заливка шахматкой + сплошное кольцо 2 px
local circle = L.grid(64, 64)
for y = 0, 63 do
  for x = 0, 63 do
    local dx, dy = x + 0.5 - 32, y + 0.5 - 32
    local d = math.sqrt(dx * dx + dy * dy)
    if d <= 31.5 then
      if d >= 29.5 or (x + y) % 2 == 0 then L.set(circle, x, y, "W") end
    end
  end
end
L.save("FX/BoomCircle", { circle })
