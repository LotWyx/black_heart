-- Black Heart: библиотека для рисования пиксель-арта в Aseprite (стиль Rhythm Doctor).
-- Рисунок строится в «сетке меток» (символ на пиксель), затем переводится в цвета палитры.
-- Запуск генераторов: dofile этого файла, затем gen_*.lua.

local L = {}

L.ART = "/home/lotwy/black_heart/Assets/Resources/Art/"
L.SRC = "/home/lotwy/black_heart/Art/Source/"

local function hex(h)
  return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) }
end

-- Строгая палитра из style bible
L.COLORS = {
  K = hex("#000000"), -- контур
  W = hex("#FFFFFF"), -- белый
  w = hex("#E8E8F0"), -- off-white
  g = hex("#B4B4C0"), -- светло-серый (тень на перекрашиваемых спрайтах)
  P = hex("#909090"), -- серый (сердце после удара)
  R = hex("#FF2E4C"), -- душа
  r = hex("#A3142E"), -- тень души
  O = hex("#FF8A1E"), -- оранжевый сигнал (замах)
  D = hex("#FF1A1A"), -- красная вспышка урона
  Y = hex("#FFD23F"), -- золото (только враги)
  V = hex("#4A2A7A"), -- тёмно-фиолетовый (только враги)
  T = hex("#1F6F6A"), -- тёмная бирюза (только враги)
  N = hex("#0B0B12"), -- фон
  s = hex("#9E3A63"), -- солнце: розовый
  p = hex("#B8664F"), -- солнце: персиковый
  y = hex("#C9A04A"), -- солнце: жёлтый
  v = hex("#382050"), -- фиолетовые контуры фона
}

-- Метки -> цвет по умолчанию. Кадры переопределяют часть меток (например, глаза -> белый).
L.LABELS = {
  K = "K", B = "W", S = "w", E = "K", M = "K", G = "W",
  Y = "Y", V = "V", T = "T", I = "Y", U = "K", C = "K",
  R = "R", r = "r", W = "W", w = "w", g = "g", P = "P", O = "O", D = "D",
  N = "N", s = "s", p = "p", y = "y", v = "v",
}

-- ---------- сетка ----------

function L.grid(w, h)
  return { w = w, h = h, d = {} }
end

function L.get(g, x, y)
  if x < 0 or y < 0 or x >= g.w or y >= g.h then return nil end
  return g.d[y * g.w + x + 1]
end

function L.set(g, x, y, c)
  if x < 0 or y < 0 or x >= g.w or y >= g.h then return end
  g.d[y * g.w + x + 1] = c
end

function L.copy(g)
  local n = L.grid(g.w, g.h)
  for k, v in pairs(g.d) do n.d[k] = v end
  return n
end

function L.each(g, fn)
  for y = 0, g.h - 1 do
    for x = 0, g.w - 1 do
      local c = g.d[y * g.w + x + 1]
      if c then fn(x, y, c) end
    end
  end
end

-- Сетка из строк ('.' — пусто)
function L.rows(rows)
  local g = L.grid(#rows[1], #rows)
  for y, row in ipairs(rows) do
    for x = 1, #row do
      local c = row:sub(x, x)
      if c ~= "." then L.set(g, x - 1, y - 1, c) end
    end
  end
  return g
end

-- Вставить сетку src в dst со смещением
function L.blit(dst, src, ox, oy)
  L.each(src, function(x, y, c) L.set(dst, x + ox, y + oy, c) end)
end

function L.shift(g, dx, dy)
  local n = L.grid(g.w, g.h)
  L.each(g, function(x, y, c) L.set(n, x + dx, y + dy, c) end)
  return n
end

-- ---------- фигуры (по центрам пикселей) ----------

function L.ellipse(g, cx, cy, rx, ry, c, only)
  for y = math.floor(cy - ry) - 1, math.ceil(cy + ry) + 1 do
    for x = math.floor(cx - rx) - 1, math.ceil(cx + rx) + 1 do
      local dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
      if dx * dx + dy * dy <= 1 and (not only or only(x, y)) then L.set(g, x, y, c) end
    end
  end
end

function L.rect(g, x0, y0, x1, y1, c)
  for y = y0, y1 do for x = x0, x1 do L.set(g, x, y, c) end end
end

function L.poly(g, pts, c)
  local minx, miny, maxx, maxy = 1e9, 1e9, -1e9, -1e9
  for _, p in ipairs(pts) do
    minx = math.min(minx, p[1]); maxx = math.max(maxx, p[1])
    miny = math.min(miny, p[2]); maxy = math.max(maxy, p[2])
  end
  for y = math.floor(miny), math.ceil(maxy) do
    for x = math.floor(minx), math.ceil(maxx) do
      local px, py, inside = x + 0.5, y + 0.5, false
      local j = #pts
      for i = 1, #pts do
        local xi, yi, xj, yj = pts[i][1], pts[i][2], pts[j][1], pts[j][2]
        if ((yi > py) ~= (yj > py)) and (px < (xj - xi) * (py - yi) / (yj - yi) + xi) then
          inside = not inside
        end
        j = i
      end
      if inside then L.set(g, x, y, c) end
    end
  end
end

function L.line(g, x0, y0, x1, y1, c, thick)
  thick = thick or 1
  local dx, dy = math.abs(x1 - x0), -math.abs(y1 - y0)
  local sx, sy = x0 < x1 and 1 or -1, y0 < y1 and 1 or -1
  local err = dx + dy
  while true do
    for ty = 0, thick - 1 do for tx = 0, thick - 1 do L.set(g, x0 + tx, y0 + ty, c) end end
    if x0 == x1 and y0 == y1 then break end
    local e2 = 2 * err
    if e2 >= dy then err = err + dy; x0 = x0 + sx end
    if e2 <= dx then err = err + dx; y0 = y0 + sy end
  end
end

function L.polyline(g, pts, c, thick)
  for i = 1, #pts - 1 do L.line(g, pts[i][1], pts[i][2], pts[i + 1][1], pts[i + 1][2], c, thick) end
end

-- ---------- обработка ----------

-- Плоская тень: пиксели метки from, у которых в направлении (dx,dy) в пределах depth
-- кончается тело, перекрашиваются в to
function L.shade(g, from, to, dx, dy, depth)
  local n = L.copy(g)
  L.each(g, function(x, y, c)
    if c == from then
      for k = 1, depth do
        if not L.get(g, x + dx * k, y + dy * k) then L.set(n, x, y, to); break end
      end
    end
  end)
  return n
end

-- Расширить силуэт на width пикселей меткой c (первый шаг — 8 соседей, дальше — 4)
function L.outline(g, width, c)
  local n = L.copy(g)
  for i = 1, width do
    local src = L.copy(n)
    local nb = (i == 1) and { {1,0},{-1,0},{0,1},{0,-1},{1,1},{1,-1},{-1,1},{-1,-1} }
                         or { {1,0},{-1,0},{0,1},{0,-1} }
    for y = 0, g.h - 1 do
      for x = 0, g.w - 1 do
        if not L.get(src, x, y) then
          for _, o in ipairs(nb) do
            if L.get(src, x + o[1], y + o[2]) then L.set(n, x, y, c); break end
          end
        end
      end
    end
  end
  return n
end

function L.recolor(g, map)
  local n = L.grid(g.w, g.h)
  L.each(g, function(x, y, c) L.set(n, x, y, map[c] or c) end)
  return n
end

function L.fillAll(g, c)
  local n = L.grid(g.w, g.h)
  L.each(g, function(x, y) L.set(n, x, y, c) end)
  return n
end

function L.bounds(g)
  local minx, miny, maxx, maxy = 1e9, 1e9, -1, -1
  L.each(g, function(x, y)
    minx = math.min(minx, x); maxx = math.max(maxx, x)
    miny = math.min(miny, y); maxy = math.max(maxy, y)
  end)
  return minx, miny, maxx, maxy
end

-- ---------- вывод ----------

local function colorOf(label, over)
  local key = (over and over[label]) or L.LABELS[label] or label
  local c = L.COLORS[key]
  assert(c, "unknown color for label " .. tostring(label))
  return app.pixelColor.rgba(c[1], c[2], c[3], 255)
end

function L.toImage(g, over)
  local img = Image(g.w, g.h, ColorMode.RGB)
  img:clear(app.pixelColor.rgba(0, 0, 0, 0))
  L.each(g, function(x, y, c) img:putPixel(x, y, colorOf(c, over)) end)
  return img
end

-- frames: список { grid = ..., over = {...} } или просто сетки
local function frameImage(f)
  if f.grid then return L.toImage(f.grid, f.over) end
  return L.toImage(f)
end

-- Сохраняет полоску кадров в Assets/Resources/Art/<id>.png и исходник в Art/Source/<id>.aseprite
function L.save(id, frames, durations)
  local imgs = {}
  for i, f in ipairs(frames) do imgs[i] = frameImage(f) end
  local fw, fh = imgs[1].width, imgs[1].height

  local strip = Image(fw * #imgs, fh, ColorMode.RGB)
  strip:clear(app.pixelColor.rgba(0, 0, 0, 0))
  for i, img in ipairs(imgs) do strip:drawImage(img, Point((i - 1) * fw, 0)) end
  os.execute('mkdir -p "' .. (L.ART .. id):match("(.*/)") .. '"')
  strip:saveAs(L.ART .. id .. ".png")

  local spr = Sprite(fw, fh, ColorMode.RGB)
  for i, img in ipairs(imgs) do
    if i > 1 then spr:newEmptyFrame() end
    spr:newCel(spr.layers[1], spr.frames[i], img, Point(0, 0))
    spr.frames[i].duration = (durations and (durations[i] or durations[#durations])) or 0.1
  end
  spr:saveAs(L.SRC .. id:gsub("/", "_") .. ".aseprite")
  spr:close()
  print("saved " .. id .. " (" .. #imgs .. " x " .. fw .. "x" .. fh .. ")")
end

return L
