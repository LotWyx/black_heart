-- Общая серая пуля 10x10 (последний запасной вариант, перекрашивается кодом), исчезновение, искры, круг «бум-круга»
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

-- Цветные пули, пули в стиле врагов, вспышка появления и трещина — в gen_projectiles_themed.lua (15x15)

-- Исчезновение: белый пиксель 2x2
local despawn = L.grid(10, 10)
L.rect(despawn, 4, 4, 5, 5, "W")
L.save("Projectiles/Bullet_despawn", { despawn })

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
