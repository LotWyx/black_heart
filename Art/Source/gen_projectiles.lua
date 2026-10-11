-- Пуля (10x10, перекрашивается кодом), трещины, искры, круг «бум-круга»
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
