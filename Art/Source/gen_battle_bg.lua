-- Фон боя: тёмный готический зал, в центре сверху — витражное окно-роза
-- с сердцем, из него падает красный свет на пол. По бокам колонны и свечи (3 кадра мерцания).
-- Центр экрана спокойный и тёмный, чтобы пули и рамка читались.
-- Вывод: Assets/Resources/Art/Background/Hall.png (3 кадра), превью в контексте — Art/Preview/.
local L = dofile("/home/lotwy/black_heart/Art/Source/bh_lib.lua")
local PREVIEW = "/home/lotwy/black_heart/Art/Preview/"
local W, H = 320, 180

local function hex(h)
  return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) }
end
local P = {
  bg0 = "#050307", bg1 = "#08050b", bg2 = "#0c070f", bg3 = "#120810",   -- фон зала (сверху вниз)
  st0 = "#0a070c", st1 = "#120c15", st2 = "#1c1420", st3 = "#2a1c2a",   -- камень колонн
  rim1 = "#3a1018", rim2 = "#5e1622",                                   -- красный отсвет на камне
  lead = "#0a0408",                                                     -- свинцовые переплёты
  gl1 = "#3a0610", gl2 = "#5e0a16", gl3 = "#8a1020", gl4 = "#b81c28", gl5 = "#ff4a3a", gl6 = "#ffb08a",
  gw1 = "#1a060c", gw2 = "#2a0810",                                     -- свечение вокруг окна
  fl0 = "#070408", fl1 = "#0d080e", fl2 = "#120a12",                    -- пол
  rf1 = "#24070e", rf2 = "#3a0a14",                                     -- отражение света на полу
  cn = "#c8b8a8", cn2 = "#8a7a70", fa = "#ff8a3a", fb = "#ffd070", fc = "#fff4d0",
  ash = "#3a2028",
}
for k, v in pairs(P) do L.COLORS[k] = hex(v) end

local function hash(x, y, s)
  local n = x * 374761393 + y * 668265263 + s * 1442695041
  n = (n ~ (n >> 13)) * 1274126177
  return ((n ~ (n >> 16)) & 0xffff) / 65535
end
local function dither(x, y, density)   -- упорядоченный дизеринг 4x4
  local m = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 }
  return (m[(y % 4) * 4 + (x % 4) + 1] + 0.5) / 16 < density
end

local CX = 160
-- Стрельчатая арка: внутри, если ниже пяты и в пределах полуширины, либо выше пяты внутри обеих дуг
local function inArch(x, y, w, spring, bottom)
  local px = x + 0.5
  if y > bottom then return false end
  if y >= spring then return math.abs(px - CX) <= w end
  local r = 2 * w
  local d1 = (px - (CX + w)) ^ 2 + (y - spring) ^ 2
  local d2 = (px - (CX - w)) ^ 2 + (y - spring) ^ 2
  return d1 <= r * r and d2 <= r * r and math.abs(px - CX) <= w
end

local ROSE_X, ROSE_Y, ROSE_R = 160, 58, 26
local WIN_W, WIN_SPRING, WIN_BOTTOM = 34, 84, 136

local function heart(x, y, cx, cy, s)   -- пиксельное сердце (неявная функция)
  local u, v = (x + 0.5 - cx) / s, -(y + 0.5 - cy) / s + 0.2
  return (u * u + v * v - 1) ^ 3 - u * u * v * v * v <= 0
end

local function glassColor(x, y)
  local dx, dy = x + 0.5 - ROSE_X, y + 0.5 - ROSE_Y
  local r = math.sqrt(dx * dx + dy * dy)
  if r <= ROSE_R then
    -- роза: сердце в центре, кольца и 12 лепестков, переплёты на границах
    if heart(x, y, ROSE_X, ROSE_Y, 6) then
      return (dy < -2 and dx < 0) and "gl6" or "gl5"
    end
    if r > 7.2 and r < 8.6 then return "lead" end
    if r > 16.5 and r < 17.8 then return "lead" end
    if r > ROSE_R - 1.3 then return "lead" end
    local a = (math.deg(math.atan(dy, dx)) + 360) % 360
    local sector = math.floor(a / 30)
    if (a % 30) < (r > 17 and 1.6 or 3.2) and r > 8 then return "lead" end
    local base = r < 17 and ((sector % 2 == 0) and "gl4" or "gl3") or ((sector % 2 == 0) and "gl3" or "gl2")
    if r > 19 and r < 23 and math.abs(((a + 15) % 30) - 15) < 4 then return "gl4" end   -- бусины в лепестках
    return base
  end
  -- два ланцета под розой, ромбическая решётка, свет тускнеет книзу
  if y < ROSE_Y + ROSE_R + 2 then return "lead" end
  if math.abs(x + 0.5 - CX) < 2 then return "lead" end           -- средник
  if (x + y) % 7 == 0 or (x - y) % 7 == 0 then return "lead" end
  local cell = ((x + y) // 7 + (x - y) // 7) % 3
  local fade = (y - (ROSE_Y + ROSE_R)) / (WIN_BOTTOM - ROSE_Y - ROSE_R)
  if fade < 0.35 then return cell == 0 and "gl4" or "gl3" end
  if fade < 0.7 then return cell == 0 and "gl3" or "gl2" end
  return cell == 0 and "gl2" or "gl1"
end

-- ширина одной колонны: x0..x1, верх капители
local PILLARS = { { 6, 28, 0 }, { 62, 78, 14 } }

local function build(frame)
  local g = L.grid(W, H)
  local HORIZON = 146

  -- фон зала: вертикальный градиент с дизерингом
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      local t = y / HORIZON
      local c = t < 0.3 and (dither(x, y, t / 0.3) and "bg1" or "bg0")
             or t < 0.7 and (dither(x, y, (t - 0.3) / 0.4) and "bg2" or "bg1")
             or (dither(x, y, math.min(1, (t - 0.7) / 0.3)) and "bg3" or "bg2")
      L.set(g, x, y, c)
    end
  end

  -- пол: плитки в перспективе к точке схода под окном
  for y = HORIZON, H - 1 do
    local depth = (y - HORIZON + 1)
    local z = 400 / depth                    -- «расстояние» ряда
    for x = 0, W - 1 do
      local wx = (x + 0.5 - CX) * z / 100
      local row = math.floor(z / 6)
      local col = math.floor(wx / 3 + 1000)
      local c = ((row + col) % 2 == 0) and "fl1" or "fl2"
      if math.abs(z / 6 - math.floor(z / 6 + 0.5)) < 0.04 * depth / 6 + 0.05 then c = "fl0" end
      L.set(g, x, y, c)
    end
  end
  for x = 0, W - 1 do L.set(g, x, HORIZON, "fl0") end

  -- лучи света от окна вниз, едва заметные, расходятся к полу
  for y = WIN_BOTTOM - 10, H - 1 do
    local t = (y - (WIN_BOTTOM - 10)) / (H - WIN_BOTTOM + 10)
    local half = WIN_W - 6 + t * 46
    for x = math.floor(CX - half), math.ceil(CX + half) do
      local edge = 1 - math.abs(x + 0.5 - CX) / half
      if edge > 0 and dither(x, y, 0.18 * edge * (1 - t * 0.5)) then L.set(g, x, y, "gw1") end
    end
  end
  -- отражение окна на полу: красная полоса, дрожит по строкам
  for y = HORIZON + 1, H - 1 do
    local t = (y - HORIZON) / (H - HORIZON)
    local half = 10 + t * 26
    local wob = math.floor(math.sin(y * 1.7 + frame * 2.1) * 2)
    for x = math.floor(CX - half), math.ceil(CX + half) do
      local edge = 1 - math.abs(x + 0.5 - CX - wob) / half
      if edge > 0 then
        local d = edge * (1 - t * 0.6)
        if dither(x, y, d * 0.55) then L.set(g, x, y, d > 0.6 and "rf2" or "rf1") end
      end
    end
  end

  -- свечение вокруг окна (дизеринг, слабеет с расстоянием)
  for y = 0, WIN_BOTTOM + 12 do
    for x = CX - WIN_W - 22, CX + WIN_W + 22 do
      if not inArch(x, y, WIN_W + 5, WIN_SPRING, WIN_BOTTOM + 3) then
        for d = 1, 16 do
          if inArch(x, y, WIN_W + 5 + d, WIN_SPRING, WIN_BOTTOM + 3 + d) then
            local k = 1 - d / 17
            if dither(x, y, k * 0.7) then L.set(g, x, y, k > 0.6 and "gw2" or "gw1") end
            break
          end
        end
      end
    end
  end

  -- каменная рама окна и само окно
  for y = 0, H - 1 do
    for x = CX - WIN_W - 6, CX + WIN_W + 6 do
      if inArch(x, y, WIN_W + 5, WIN_SPRING, WIN_BOTTOM + 3) then
        local glass = inArch(x, y, WIN_W, WIN_SPRING, WIN_BOTTOM)
        if glass then
          L.set(g, x, y, glassColor(x, y))
        else
          local lit = (x < CX) and "st2" or "st1"
          if inArch(x, y, WIN_W + 2, WIN_SPRING, WIN_BOTTOM + 1) then lit = "rim1" end
          L.set(g, x, y, lit)
        end
      end
    end
  end
  -- подоконник
  for x = CX - WIN_W - 8, CX + WIN_W + 8 do
    L.set(g, x, WIN_BOTTOM + 4, "st3"); L.set(g, x, WIN_BOTTOM + 5, "st1"); L.set(g, x, WIN_BOTTOM + 6, "st0")
  end

  -- колонны (левые и зеркальные правые): тень с внешней стороны, красный отсвет со стороны окна
  for _, side in ipairs({ 1, -1 }) do
    for _, p in ipairs(PILLARS) do
      local x0, x1, top = p[1], p[2], p[3]
      if side == -1 then x0, x1 = W - 1 - p[2], W - 1 - p[1] end
      local inner = side == 1 and x1 or x0           -- сторона, обращённая к окну
      for y = top, HORIZON + 2 do
        for x = x0, x1 do
          local u = (x - x0) / (x1 - x0)
          if side == -1 then u = 1 - u end
          local c = u < 0.25 and "st0" or u < 0.7 and "st1" or u < 0.88 and "st2" or "rim1"
          if x == inner then c = "rim2" end
          if (y - top) % 34 == 0 or (y - top) % 34 == 1 then c = (c == "rim2" or c == "rim1") and "rim1" or "st0" end
          -- желобки-каннелюры
          if (x - x0) % 5 == 2 and c ~= "rim2" and c ~= "rim1" then c = "st0" end
          L.set(g, x, y, c)
        end
      end
      -- капитель и база шире ствола
      local function slab(y0, y1)
        for y = y0, y1 do
          for x = x0 - 3, x1 + 3 do
            local c = (y == y0) and "st3" or "st1"
            if x == (side == 1 and x1 + 3 or x0 - 3) then c = "rim1" end
            L.set(g, x, y, c)
          end
        end
        for x = x0 - 3, x1 + 3 do L.set(g, x, y1 + 1, "st0") end
      end
      if top > 0 then slab(top - 4, top - 1) end
      slab(HORIZON - 5, HORIZON - 1)
    end
  end
  -- арка между колоннами сверху (тёмный свод)
  for _, side in ipairs({ 1, -1 }) do
    local ax0, ax1 = 28, 62
    local acx, ar = (ax0 + ax1) / 2, (ax1 - ax0) / 2 + 3
    for y = 0, 16 do
      for x = ax0 - 2, ax1 + 2 do
        local d = math.sqrt((x + 0.5 - acx) ^ 2 + ((y - 16) * 1.6) ^ 2)
        if d >= ar - 4 and d <= ar then
          local xx = side == 1 and x or W - 1 - x
          L.set(g, xx, y, d > ar - 1 and "st2" or "st1")
        end
      end
    end
  end

  -- свечи на подставках между колоннами, пламя мерцает по кадрам
  for _, cx in ipairs({ 45, W - 46 }) do
    for y = 118, HORIZON - 1 do L.set(g, cx, y, "st2") end
    for x = cx - 3, cx + 3 do L.set(g, x, 118, "st3"); L.set(g, x, HORIZON - 1, "st2") end
    for y = 112, 117 do L.set(g, cx, y, "cn"); L.set(g, cx + 1, y, "cn2") end
    -- ореол
    for y = 100, 118 do
      for x = cx - 9, cx + 10 do
        local d = math.sqrt((x - cx) ^ 2 + (y - 108) ^ 2)
        if d < 9 and L.get(g, x, y):sub(1, 2) == "bg" and dither(x, y, (1 - d / 9) * 0.5) then L.set(g, x, y, "gw2") end
      end
    end
    local h = ({ 4, 5, 3 })[frame + 1]
    local lean = ({ 0, 1, 0 })[frame + 1]
    for i = 0, h - 1 do
      L.set(g, cx + (i >= h - 2 and lean or 0), 111 - i, i == 0 and "fb" or i == h - 1 and "fa" or "fb")
      if i < h - 2 then L.set(g, cx + 1, 111 - i, "fa") end
    end
    L.set(g, cx, 110, "fc")
  end

  -- пылинки в луче
  for i = 0, 26 do
    local x = math.floor(CX - 40 + hash(i, 1, 3) * 80)
    local y = math.floor(90 + hash(i, 2, 3) * 70 + (frame + i) % 3 - 1)
    if L.get(g, x, y) and L.get(g, x, y):sub(1, 2) ~= "gl" then L.set(g, x, y, (i % 3 == 0) and "rf2" or "ash") end
  end
  return g
end

local frames = { build(0), build(1), build(2) }
L.save("Background/Hall", frames, { 0.18 })

-- Превью в контексте: фон x3 с рамкой боя в обеих фазах (атака — внизу, защита — почти весь экран)
local function mock(g, box)
  local m = L.copy(g)
  local x0, y0, x1, y1 = box[1], box[2], box[3], box[4]
  for y = y0 - 4, y1 + 4 do
    for x = x0 - 4, x1 + 4 do
      local inside = x >= x0 and x <= x1 and y >= y0 and y <= y1
      local band = not inside and x >= x0 - 3 and x <= x1 + 3 and y >= y0 - 3 and y <= y1 + 3
      if inside then L.set(m, x, y, "bg0") elseif band then L.set(m, x, y, "cn") else L.set(m, x, y, "lead") end
    end
  end
  return m
end
local spr = Image(W * 3, H * 3 * 3 + 8, ColorMode.RGB)
spr:clear(app.pixelColor.rgba(20, 14, 20, 255))
local views = { frames[1], mock(frames[2], { 112, 100, 207, 156 }), mock(frames[3], { 24, 23, 295, 151 }) }
for i, gv in ipairs(views) do
  local img = L.toImage(gv)
  if i == 2 then   -- враг на своём месте сверху по центру, как в игре
    local enemy = Image{ fromFile = "/home/lotwy/black_heart/Assets/Resources/Art/Enemies/Ghost.png" }
    img:drawImage(enemy, Point(W // 2 - enemy.width // 2, 2))
  end
  for y = 0, H - 1 do for x = 0, W - 1 do
    local c = img:getPixel(x, y)
    for dy = 0, 2 do for dx = 0, 2 do spr:drawPixel(x * 3 + dx, (i - 1) * (H * 3 + 4) + y * 3 + dy, c) end end
  end end
end
os.execute('mkdir -p "' .. PREVIEW .. '"')
spr:saveAs(PREVIEW .. "preview_BattleBackground.png")
print("ok")
