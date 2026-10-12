-- Вступление (в духе интро Undertale): три картинки в стиле фонов игры, по 4 кадра лёгкой анимации.
-- Картинки больше окна показа (240x120): в игре окно медленно едет по ним (панорама).
--   Scene1 256x160 — Луноликий вместо луны хохочет над городом (панорама сверху вниз).
--   Scene2 288x120 — нечисть на улицах, из людей вытягивается красная энергия к красной луне (слева направо).
--   Scene3 240x160 — герой на крыше смотрит вверх на луну (снизу вверх).
-- Вывод: Assets/Resources/Art/Intro/Scene<N>.png (полоска кадров), превью — Art/Preview/preview_intro.png.
local ART = "/home/lotwy/black_heart/Assets/Resources/Art/Intro/"
local PREVIEW = "/home/lotwy/black_heart/Art/Preview/preview_intro.png"
local FRAMES = 4

local function hex(h) return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) } end
local C = {}
for k, v in pairs({
  -- небо
  k0 = "#040406", k1 = "#09080B", k2 = "#0F0C11", k3 = "#161018", k4 = "#1E121A",
  -- облака, подсвеченные красной луной: тень → край
  c1 = "#140E16", c2 = "#22141C", c3 = "#361A20", c4 = "#552426", c5 = "#7C342C", c6 = "#A44A34",
  -- луна
  m0 = "#2E060C", m1 = "#4A0A10", m2 = "#7E1218", m3 = "#B0201E", m4 = "#D8402C", m5 = "#F07A50", m6 = "#FFD2B4",
  -- глаза и зубы
  e1 = "#B4B4C0", e2 = "#E8E8F0", e3 = "#FFFFFF", bk = "#000000",
  -- цилиндр
  h0 = "#08070B", h1 = "#121019", h2 = "#1F1A26", h3 = "#332B3A",
  -- город: силуэты и красная кайма
  b0 = "#060508", b1 = "#0C0A10", b2 = "#141018", b3 = "#1E1822", r1 = "#3A0C12", r2 = "#6A1418", r3 = "#9A2020",
  -- окна и огни
  w1 = "#5A1A12", w2 = "#A8401E", w3 = "#F09050", w4 = "#FFE0A0",
  -- туман
  f1 = "#1A1016", f2 = "#2A1418",
  -- души и энергия
  s1 = "#A3142E", s2 = "#FF2E4C", s3 = "#FF8A9A", s4 = "#FFE0E6",
  -- нечисть и люди
  n0 = "#050407", n1 = "#0E0B12", p1 = "#2E2830", p2 = "#463E46", p3 = "#6A6070",
  -- мостовая
  g0 = "#0A080C", g1 = "#141016", g2 = "#201A20", g3 = "#2E2228",
}) do C[k] = hex(v) end

-- ---------- холст ----------
local function canvas(w, h) return { w = w, h = h, d = {} } end
local function put(cv, x, y, c)
  x, y = math.floor(x), math.floor(y)
  if c and x >= 0 and y >= 0 and x < cv.w and y < cv.h then cv.d[y * cv.w + x + 1] = c end
end
local function get(cv, x, y)
  x, y = math.floor(x), math.floor(y)
  if x < 0 or y < 0 or x >= cv.w or y >= cv.h then return nil end
  return cv.d[y * cv.w + x + 1]
end
local function copy(cv)
  local n = canvas(cv.w, cv.h)
  for k, v in pairs(cv.d) do n.d[k] = v end
  return n
end
local function dither(x, y) return (x + y) % 2 == 0 end
local function rect(cv, x0, y0, x1, y1, c) for y = y0, y1 do for x = x0, x1 do put(cv, x, y, c) end end end
local function poly(cv, pts, c)
  local minx, miny, maxx, maxy = 1e9, 1e9, -1e9, -1e9
  for _, p in ipairs(pts) do
    minx = math.min(minx, p[1]); maxx = math.max(maxx, p[1]); miny = math.min(miny, p[2]); maxy = math.max(maxy, p[2])
  end
  for y = math.floor(miny), math.ceil(maxy) do
    for x = math.floor(minx), math.ceil(maxx) do
      local px, py, inside, j = x + 0.5, y + 0.5, false, #pts
      for i = 1, #pts do
        local xi, yi, xj, yj = pts[i][1], pts[i][2], pts[j][1], pts[j][2]
        if ((yi > py) ~= (yj > py)) and (px < (xj - xi) * (py - yi) / (yj - yi) + xi) then inside = not inside end
        j = i
      end
      if inside then put(cv, x, y, type(c) == "function" and c(x, y) or c) end
    end
  end
end
local function ellipse(cv, cx, cy, rx, ry, c)
  for y = math.floor(cy - ry) - 1, math.ceil(cy + ry) + 1 do
    for x = math.floor(cx - rx) - 1, math.ceil(cx + rx) + 1 do
      local dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
      if dx * dx + dy * dy <= 1 then put(cv, x, y, type(c) == "function" and c(x, y, dx, dy) or c) end
    end
  end
end
local function line(cv, x0, y0, x1, y1, c)
  x0, y0, x1, y1 = math.floor(x0), math.floor(y0), math.floor(x1), math.floor(y1)
  local dx, dy = math.abs(x1 - x0), -math.abs(y1 - y0)
  local sx, sy = x0 < x1 and 1 or -1, y0 < y1 and 1 or -1
  local err = dx + dy
  while true do
    put(cv, x0, y0, c)
    if x0 == x1 and y0 == y1 then break end
    local e2 = 2 * err
    if e2 >= dy then err = err + dy; x0 = x0 + sx end
    if e2 <= dx then err = err + dx; y0 = y0 + sy end
  end
end

-- Кайма: пиксели тела, у которых сосед в направлении (dx,dy) не тело, перекрашиваются.
-- Проверка по снимку, чтобы кайма не «протекала» вглубь.
local function rim(cv, x0, y0, x1, y1, isBody, dx, dy, c, only)
  local src = copy(cv)
  for y = y0, y1 do for x = x0, x1 do
    local nx, ny = x + dx, y + dy
    if isBody(get(src, x, y)) and nx >= 0 and ny >= 0 and nx < cv.w and ny < cv.h and not isBody(get(src, nx, ny))
       and (not only or only(x, y)) then put(cv, x, y, c) end
  end end
end
local function is(...) local set = {}; for _, v in ipairs({ ... }) do set[v] = true end; return function(c) return set[c] end end

-- ---------- шум ----------
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
  for o = 0, 3 do sum = sum + noise(x * 2 ^ o, y * 2 ^ o, seed + o) * amp; norm = norm + amp; amp = amp * 0.5 end
  return sum / norm
end

-- ---------- общие элементы ----------

-- Небо: ступенчатый градиент с дизерингом на стыках (темнее вверху)
local function sky(cv, y0, y1, bands)
  for y = y0, y1 do
    local t = (y - y0) / math.max(1, y1 - y0)
    local i = math.min(#bands, math.floor(t * #bands) + 1)
    for x = 0, cv.w - 1 do
      local c = bands[i]
      local edge = (t * #bands) % 1
      if i < #bands and edge > 0.85 and dither(x, y) then c = bands[i + 1] end
      put(cv, x, y, c)
    end
  end
end

-- Облака: плотные массы, край подсвечен со стороны луны (mx, my); drift — сдвиг по кадрам
local function clouds(cv, y0, y1, mx, my, seed, drift, scaleX, scaleY, bandY, bandH)
  for y = y0, y1 do
    for x = 0, cv.w - 1 do
      local function dens(px, py)
        local band = math.exp(-((py - bandY) / bandH) ^ 2)
        return fbm((px + drift) / scaleX, py / scaleY, seed) * (0.45 + 0.75 * band)
      end
      local v = dens(x, y)
      if v > 0.43 then
        local dx, dy = mx - x, my - y
        local dist = math.max(1, math.sqrt(dx * dx + dy * dy))
        local light = (v - dens(x + dx / dist * 4, y + dy / dist * 4)) * 14
        local near = math.max(0, 1 - dist / 190)
        local lvl = light * (0.3 + near)
        local c = lvl > 0.95 and near > 0.45 and "c6" or lvl > 0.7 and near > 0.3 and "c5" or lvl > 0.42 and "c4"
                  or lvl > 0.2 and "c3" or lvl > 0.03 and "c2" or "c1"
        if v < 0.45 and not dither(x, y) then c = nil end
        put(cv, x, y, c)
      end
    end
  end
end

-- Ореол вокруг луны: дизер-кольца
local function halo(cv, mx, my, r, width)
  for y = math.floor(my - r - width), math.ceil(my + r + width) do
    for x = math.floor(mx - r - width), math.ceil(mx + r + width) do
      local d = math.sqrt((x + 0.5 - mx) ^ 2 + (y + 0.5 - my) ^ 2)
      if d > r and d < r + width * 0.4 and dither(x, y) then put(cv, x, y, "m1")
      elseif d >= r + width * 0.4 and d < r + width and x % 2 == 0 and y % 2 == 0 then put(cv, x, y, "m1") end
    end
  end
end

-- Диск луны: освещён слева сверху, кратеры, светлый серп по краю
local function moonDisk(cv, mx, my, r, craters, bright)
  local lx, ly, lz = -0.75, -0.4, 0.55
  local ll = math.sqrt(lx * lx + ly * ly + lz * lz); lx, ly, lz = lx / ll, ly / ll, lz / ll
  ellipse(cv, mx, my, r, r, function(x, y, dx, dy)
    local dz = math.sqrt(math.max(0, 1 - dx * dx - dy * dy))
    local s = dx * lx + dy * ly + dz * lz
    for _, cr in ipairs(craters) do
      local d2 = (dx - cr[1]) ^ 2 + (dy - cr[2]) ^ 2
      if d2 < cr[3] ^ 2 then s = s - 0.2 elseif d2 < (cr[3] + 0.05) ^ 2 and dx < cr[1] then s = s + 0.08 end
    end
    s = s + (fbm(x / 6, y / 6, 77) - 0.5) * 0.25   -- рябь поверхности
    s = s * (bright or 1)
    local c = s > 0.8 and "m5" or s > 0.52 and "m4" or s > 0.25 and "m3" or s > 0 and "m2" or "m1"
    if math.sqrt(dx * dx + dy * dy) > 1 - 1.4 / r and dx < -0.45 and dy < 0.4 then c = "m6" end
    return c
  end)
end

-- Силуэт города: дома с острыми крышами, шпили, трубы; кайма сверху, редкие горящие окна.
-- seed задаёт застройку, base — линия земли, rimSide — откуда светит луна (-1 слева, 1 справа)
local function city(cv, x0, x1, base, minH, maxH, seed, shade, rimC, frame, windowsLit)
  local x = x0
  local idx = 0
  while x < x1 do
    idx = idx + 1
    local w = 10 + math.floor(hash(idx, 1, seed) * 16)
    local h = minH + math.floor(hash(idx, 2, seed) * (maxH - minH))
    local top = base - h
    local roof = hash(idx, 3, seed)
    rect(cv, x, top, x + w - 1, base, shade)
    if roof < 0.45 then        -- острая крыша
      poly(cv, { {x - 1, top + 1}, {x + w / 2, top - 6 - math.floor(hash(idx, 4, seed) * 6)}, {x + w, top + 1} }, shade)
    elseif roof < 0.7 then     -- ступенчатый фронтон
      rect(cv, x + 2, top - 3, x + w - 3, top, shade); rect(cv, x + 4, top - 5, x + w - 5, top - 3, shade)
    elseif roof < 0.85 then    -- шпиль
      local cx = x + math.floor(w / 2)
      poly(cv, { {cx - 3, top + 1}, {cx, top - 18}, {cx + 3, top + 1} }, shade)
      line(cv, cx, top - 22, cx, top - 18, shade); line(cv, cx - 1, top - 20, cx + 1, top - 20, shade)
    end
    if hash(idx, 5, seed) > 0.55 then rect(cv, x + w - 4, top - 5, x + w - 3, top, shade) end   -- труба
    -- окна: сетка, часть тлеет красным, мерцает по кадрам
    for wy = top + 4, base - 4, 5 do
      for wx = x + 2, x + w - 3, 4 do
        local hv = hash(wx * 7 + wy, idx, seed + 9)
        if hv > windowsLit then
          local flick = hash(wx, wy, seed + frame) > 0.85
          local c = hv > 0.97 and (flick and "w2" or "w3") or (hv > 0.9 and "w2" or "w1")
          put(cv, wx, wy, c); put(cv, wx, wy + 1, c == "w3" and "w2" or c)
        end
      end
    end
    x = x + w + math.floor(hash(idx, 6, seed) * 3)
  end
  -- кайма по верхним краям
  rim(cv, math.max(0, x0), 1, math.min(cv.w - 1, x1), base - 1, is(shade), 0, -1, rimC)
end

-- Цилиндр: тулья с бликом, красная лента, поля
local function tophat(cv, cx, brimY, crownW, crownH, brimW, tilt)
  local function sk(y) return math.floor((brimY - y) * tilt) end
  for y = brimY - crownH, brimY do
    local o = sk(y)
    for x = cx - crownW / 2, cx + crownW / 2 - 1 do
      local rel = (x - (cx - crownW / 2)) / crownW
      local c = rel < 0.12 and "h3" or rel < 0.3 and "h2" or rel > 0.85 and "h0" or "h1"
      if y >= brimY - 9 and y <= brimY - 4 then c = (y == brimY - 9) and "m4" or (rel < 0.3 and "m3" or "m2") end
      put(cv, x + o, y, c)
    end
  end
  ellipse(cv, cx + sk(brimY) + 1, brimY + 1, brimW / 2, 3.5, function(x, y, dx, dy) return dy < -0.3 and "h2" or "h0" end)
  for x = cx - crownW / 2, cx + crownW / 2 - 1 do put(cv, x + sk(brimY - crownH), brimY - crownH, "h3") end
end

-- Лицо Луноликого на диске: квадратные глаза со злыми бровями и ухмылка с зубами-пилами.
-- open — насколько раскрыт рот (смех), glare — глаза светятся
local function moonFace(cv, cx, cy, s, open, angry)
  for side = -1, 1, 2 do
    local ex = cx + side * 9 * s
    local ey = cy - 4 * s
    local hw, hh = math.floor(5 * s), math.floor(4 * s)
    for y = -hh, hh do
      for x = -hw, hw do
        local px = ex + x
        local cutoff = angry and (-hh + (hw - side * x) * 0.5) or -hh   -- злая бровь срезает верх к переносице
        if y >= cutoff then
          local edge = math.abs(x) == hw or y == hh or y <= cutoff + 0.99
          put(cv, px, ey + y, edge and "e1" or "e3")
        end
      end
    end
    -- зрачок смотрит вниз, на город
    local pxo = -side * 1
    rect(cv, ex - 1 + pxo, ey + 0, ex + 1 + pxo, ey + 2, "bk")
    put(cv, ex + pxo, ey + 1, "m3")
    -- бровь
    local ox, ix = ex + side * (hw + 1), ex - side * (hw + 2)   -- внешний край выше, у переносицы ниже
    line(cv, ox, ey - hh - 2, ix, ey - hh + math.floor(hw * 0.9), "m0")
    line(cv, ox, ey - hh - 3, ix, ey - hh + math.floor(hw * 0.9) - 1, "m0")
    line(cv, ox, ey - hh - 4, ix, ey - hh + math.floor(hw * 0.9) - 2, "m1")
  end
  -- рот: широкая дуга, в смехе раскрывается
  local mw = math.floor(17 * s)
  local my = cy + math.floor(7 * s)
  local mh = math.floor((3 + open * 6) * s)
  for x = -mw, mw do
    local curve = math.floor((x / mw) ^ 2 * -4 * s)   -- уголки вверх
    local depth = math.floor(mh * math.sqrt(math.max(0, 1 - (x / mw) ^ 2)))
    for y = 0, depth do put(cv, cx + x, my + curve + y, "bk") end
    if depth >= 0 then put(cv, cx + x, my + curve - 1, "m1") end
  end
  -- зубы-пилы сверху и снизу
  for x = -mw + 2, mw - 2, 3 do
    local curve = math.floor((x / mw) ^ 2 * -4 * s)
    local depth = math.floor(mh * math.sqrt(math.max(0, 1 - (x / mw) ^ 2)))
    put(cv, cx + x, my + curve, "e2"); put(cv, cx + x + 1, my + curve, "e2"); put(cv, cx + x, my + curve + 1, "e1")
    if depth > 3 then
      put(cv, cx + x + 1, my + curve + depth, "e2"); put(cv, cx + x + 2, my + curve + depth, "e1")
      put(cv, cx + x + 1, my + curve + depth - 1, "e1")
    end
  end
  if open > 0.5 then   -- язык в глубине рта
    for x = -math.floor(5 * s), math.floor(5 * s) do put(cv, cx + x, my + mh - 1, "m2"); put(cv, cx + x, my + mh - 2, dither(x, 0) and "m1" or "bk") end
  end
end

-- Летучая мышь 7x3, крылья вверх/вниз
local function bat(cv, x, y, up)
  put(cv, x, y, "b0"); put(cv, x + 1, y, "b0"); put(cv, x, y + 1, "b0")
  if up then
    put(cv, x - 1, y - 1, "b0"); put(cv, x - 2, y - 2, "b0"); put(cv, x + 2, y - 1, "b0"); put(cv, x + 3, y - 2, "b0")
  else
    put(cv, x - 1, y + 1, "b0"); put(cv, x - 2, y + 1, "b0"); put(cv, x + 2, y + 1, "b0"); put(cv, x + 3, y + 1, "b0")
  end
end

-- Туман: полосы дизера
local function fog(cv, y0, y1, frame, seed)
  for y = y0, y1 do
    for x = 0, cv.w - 1 do
      local v = noise((x + frame * 2) / 30, y / 4, seed)
      if v > 0.62 and dither(x, y) then put(cv, x, y, "f2") elseif v > 0.5 and (x + y * 3) % 4 == 0 then put(cv, x, y, "f1") end
    end
  end
end

-- =====================================================================
-- Сцена 1: Луноликий вместо луны хохочет над городом
-- =====================================================================
local function scene1(f)
  local W, H = 256, 160
  local cv = canvas(W, H)
  local MX, MY, MR = 128, 74, 36
  local bob = (f % 2 == 1) and 1 or 0          -- голова трясётся от хохота
  local open = ({ 0.4, 1, 0.55, 1 })[f + 1]

  sky(cv, 0, H - 1, { "k0", "k1", "k2", "k3", "k4" })
  clouds(cv, 0, 128, MX, MY, 11, f * 1.5, 46, 15, 74, 40)
  halo(cv, MX, MY + bob, MR, 12)
  -- лучи злого света: тонкие расходящиеся линии дизера
  for k = 0, 11 do
    local a = k / 12 * math.pi * 2 + 0.13
    for d = MR + 13, MR + 30 + (k % 3) * 8 do
      local x, y = MX + math.cos(a) * d, MY + bob + math.sin(a) * d
      if math.floor(d) % 2 == (f % 2) and y < 118 then put(cv, x, y, "m1") end
    end
  end
  moonDisk(cv, MX, MY + bob, MR, { {0.35, -0.55, 0.12}, {-0.55, 0.45, 0.14}, {0.62, 0.3, 0.1}, {-0.15, 0.7, 0.09}, {0.7, -0.2, 0.08} })
  moonFace(cv, MX, MY + bob, 1.55, open, true)
  tophat(cv, MX + 2, MY + bob - MR + 7 - (f == 1 and 2 or f == 3 and 1 or 0), 42, 34, 76, 0.12)

  -- мыши кружат
  for i, b in ipairs({ {40, 52}, {200, 40}, {216, 66}, {60, 84} }) do
    bat(cv, b[1] + f * (i % 2 == 0 and -3 or 3), b[2] + ((f + i) % 2), (f + i) % 2 == 0)
  end

  -- город: дальний ряд, ближний ряд, туман между ними
  city(cv, -4, W + 4, 136, 10, 26, 3, "b2", "r1", f, 0.93)
  fog(cv, 120, 142, f, 4)
  city(cv, -8, W + 8, 160, 14, 34, 8, "b0", "r2", f, 0.9)
  -- собор по центру ближнего ряда
  local cx = 128
  poly(cv, { {cx - 18, 160}, {cx - 18, 128}, {cx, 112}, {cx + 18, 128}, {cx + 18, 160} }, "b0")
  poly(cv, { {cx - 4, 114}, {cx, 92}, {cx + 4, 114} }, "b0")
  line(cv, cx, 86, cx, 92, "b0"); line(cv, cx - 2, 89, cx + 2, 89, "b0")
  ellipse(cv, cx, 138, 5, 6, function(x, y, dx, dy) return (dx * dx + dy * dy < 0.3) and (f % 2 == 0 and "w3" or "w2") or "w1" end)
  line(cv, cx, 132, cx, 144, "b0"); line(cv, cx - 5, 138, cx + 5, 138, "b0")
  rim(cv, cx - 19, 86, cx + 19, 159, is("b0", "r2"), 0, -1, "r2")
  return cv
end

-- =====================================================================
-- Сцена 2: нечисть на улицах, из людей вытягивается энергия к красной луне
-- =====================================================================

-- Нечисть: бывший человек — сгорбленный, тощие руки до колен, красные глаза. Покачивается при ходьбе.
local function ghoul(cv, x, base, h, f, dir)
  local sway = (f % 2 == 0) and 0 or 1
  local hip, sh = base - math.floor(h * 0.42), base - math.floor(h * 0.78)
  -- ноги, полусогнутые
  for o = -1, 1 do
    line(cv, x - 1 + o, hip, x - 3 - dir + o, base, "n0")
    line(cv, x + 2 + o, hip, x + 3 + sway + o, base, "n0")
  end
  -- горбатая спина
  poly(cv, { {x - 3, hip + 1}, {x - 4 - dir, sh + 3}, {x - 1 - dir * 2, sh - 2}, {x + dir * 4, sh - 1},
             {x + 3 + dir * 3, sh + 4}, {x + 3, hip + 1} }, "n0")
  -- голова свисает вперёд
  local hx, hy = x + dir * 6, sh + 1 + sway
  ellipse(cv, hx, hy, 3, 3.2, "n0")
  -- руки: длинные, висят ниже колен, с когтями
  local ax = x + dir * 3
  for o = 0, 1 do
    line(cv, ax + o, sh + 2, ax + dir * 4 + o, hip, "n0"); line(cv, ax + dir * 4 + o, hip, ax + dir * (5 + sway) + o, base - 3, "n0")
  end
  put(cv, ax + dir * (6 + sway), base - 2, "n0"); put(cv, ax + dir * (4 + sway), base - 2, "n0")
  for o = 0, 1 do
    line(cv, x - dir * 2 + o, sh + 2, x - dir * 4 + o, hip + 2, "n0"); line(cv, x - dir * 4 + o, hip + 2, x - dir * (3 - sway) + o, base - 4, "n0")
  end
  -- глаза светятся
  put(cv, hx + dir, hy, "s2"); put(cv, hx + dir * 2, hy, "s3"); put(cv, hx - dir, hy, "s1")
  -- кайма от луны (свет справа сверху)
  local body = is("n0", "s1", "s2", "s3")
  rim(cv, x - 10, base - h - 6, x + 12, base, body, 1, -1, "r2")
end

-- Человек: бледный, в пальто, запрокинут назад — из груди вытягивается душа.
-- weak — уже почти без сил (на коленях)
local function person(cv, x, base, f, weak)
  local lean = weak and 2 or 1
  local h = weak and 16 or 23
  local hip = base - (weak and 4 or 10)
  local chestY = base - h + 8
  if weak then
    rect(cv, x - 4, base - 3, x + 3, base, "p1")                                   -- колени на мостовой
  else
    line(cv, x - 1, hip, x - 2, base, "p1"); line(cv, x, hip, x - 1, base, "p1")   -- ноги
    line(cv, x + 2, hip, x + 3, base, "p1"); line(cv, x + 3, hip, x + 4, base, "p1")
  end
  -- пальто, корпус выгнут назад
  poly(cv, { {x - 3, hip + 1}, {x - 3 - lean, chestY - 3}, {x + 3 - lean, chestY - 3}, {x + 4, hip + 1} }, "p1")
  rim(cv, x - 6, chestY - 4, x + 6, hip + 1, is("p1"), 1, 0, "p2")
  -- голова запрокинута, лицо бледное
  local hx, hy = x - lean - 1, chestY - 6
  ellipse(cv, hx, hy, 2.6, 2.8, "p3")
  put(cv, hx - 1, hy - 2, "p1"); put(cv, hx, hy - 2, "p1"); put(cv, hx + 1, hy - 2, "p1")   -- волосы
  put(cv, hx + 1, hy, "n1")                                                              -- раскрытый рот
  -- руки безвольно раскинуты
  local wave = f % 2
  line(cv, x - 3 - lean, chestY - 1, x - 7 - lean, chestY + 4 + wave, "p1")
  line(cv, x + 3 - lean, chestY - 1, x + 7, chestY + 3 + (1 - wave), "p1")
  put(cv, x - 7 - lean, chestY + 5 + wave, "p3"); put(cv, x + 7, chestY + 4 + (1 - wave), "p3")
  return x - lean, chestY
end

-- Поток души: кривая от груди к луне, частицы бегут по ней (сдвиг по кадрам)
local function soulStream(cv, sx, sy, tx, ty, f, seed)
  local mx1, my1 = sx + (tx - sx) * 0.25 + (hash(seed, 1, 5) - 0.5) * 40, sy - 30
  local mx2, my2 = sx + (tx - sx) * 0.7, ty + 30 + hash(seed, 2, 5) * 10
  local function at(t)
    local u = 1 - t
    return u ^ 3 * sx + 3 * u * u * t * mx1 + 3 * u * t * t * mx2 + t ^ 3 * tx,
           u ^ 3 * sy + 3 * u * u * t * my1 + 3 * u * t * t * my2 + t ^ 3 * ty
  end
  -- тусклый след
  for i = 0, 120 do
    local x, y = at(i / 120)
    if (i + f) % 3 == 0 then put(cv, x, y, "s1") end
  end
  -- яркие сгустки, бегут к луне
  for k = 0, 5 do
    local t = ((k + f / FRAMES) / 6) % 1
    local x, y = at(t)
    put(cv, x, y, "s3"); put(cv, x + 1, y, "s2"); put(cv, x - 1, y, "s2"); put(cv, x, y + 1, "s2"); put(cv, x, y - 1, "s1")
    if k % 2 == 0 then put(cv, x, y, "s4") end
  end
  -- сияние у груди
  for _, o in ipairs({ {0,0,"s4"}, {-1,0,"s3"}, {1,0,"s3"}, {0,-1,"s3"}, {0,1,"s2"}, {-2,0,"s2"}, {2,0,"s2"} }) do
    put(cv, sx + o[1], sy + o[2], o[3])
  end
end

local function scene2(f)
  local W, H = 288, 120
  local cv = canvas(W, H)
  local MX, MY, MR = 236, 24, 17

  sky(cv, 0, 80, { "k0", "k1", "k2", "k3" })
  clouds(cv, 0, 60, MX, MY, 23, f * 1.5, 40, 12, 30, 22)
  halo(cv, MX, MY, MR, 10)
  moonDisk(cv, MX, MY, MR, { {0.3, -0.4, 0.15}, {-0.4, 0.4, 0.15} }, 1.15)
  moonFace(cv, MX, MY, 0.75, 0.3 + (f % 2) * 0.3, true)

  -- дальние крыши
  city(cv, -4, W + 4, 74, 14, 32, 17, "b2", "r1", f, 0.95)
  -- дома улицы: фасады с окнами и вывесками
  local houses = { {0, 30}, {34, 22}, {70, 34}, {116, 26}, {150, 38}, {200, 24}, {236, 30}, {270, 26} }
  for i, hsd in ipairs(houses) do
    local x0, w = hsd[1], hsd[2]
    local top = 34 + math.floor(hash(i, 1, 31) * 14)
    rect(cv, x0, top, x0 + w - 1, 100, "b1")
    poly(cv, { {x0 - 2, top + 1}, {x0 + w / 2, top - 10}, {x0 + w + 1, top + 1} }, "b1")
    rim(cv, x0 - 2, top - 10, x0 + w + 1, 99, is("b1", "r1"), 0, -1, "r1")
    for wy = top + 5, 86, 11 do
      for wx = x0 + 4, x0 + w - 7, 8 do
        local lit = hash(wx, wy, 41) > 0.7
        rect(cv, wx, wy, wx + 3, wy + 5, lit and "w1" or "b0")
        if lit then put(cv, wx + 1, wy + 2, hash(wx, wy, 41 + f) > 0.5 and "w2" or "w1") end
        line(cv, wx - 1, wy + 6, wx + 4, wy + 6, "b3")
      end
    end
    rect(cv, x0 + math.floor(w / 2) - 3, 88, x0 + math.floor(w / 2) + 2, 100, "b0")   -- дверь
  end
  -- мостовая
  for y = 100, H - 1 do
    for x = 0, W - 1 do
      local row = math.floor((y - 100) / 4)
      local brick = ((x + row * 5) % 10 == 0) or ((y - 100) % 4 == 0)
      local c = brick and "g0" or (noise(x / 5, y / 3, 61) > 0.6 and "g2" or "g1")
      if y > 108 and not brick and math.abs(x - MX + 40) < 30 and dither(x, y) then c = "g3" end   -- отсвет луны в лужах
      put(cv, x, y, c)
    end
  end
  -- фонари с красным пламенем
  for _, lx in ipairs({ 58, 144, 226 }) do
    line(cv, lx, 72, lx, 102, "b0"); line(cv, lx - 1, 102, lx + 1, 102, "b0")
    rect(cv, lx - 2, 68, lx + 2, 72, "b0")
    put(cv, lx, 70, f % 2 == 0 and "w3" or "w2"); put(cv, lx - 1, 70, "w2"); put(cv, lx + 1, 70, "w1"); put(cv, lx, 69, "w2")
  end
  fog(cv, 92, 106, f, 7)

  -- нечисть и люди; у людей вытягивается душа
  local walk = f % 2
  ghoul(cv, 20 + walk, 104, 19, f, 1)
  ghoul(cv, 98 - walk, 106, 22, f + 1, -1)
  ghoul(cv, 182 + walk, 105, 17, f, 1)
  ghoul(cv, 262 - walk, 104, 21, f + 1, -1)
  local px1, py1 = person(cv, 48, 105, f, false)
  local px2, py2 = person(cv, 126, 104, f + 1, true)
  local px3, py3 = person(cv, 210, 106, f, false)
  soulStream(cv, px1, py1, MX - 6, MY + 8, f, 1)
  soulStream(cv, px2, py2, MX - 4, MY + 12, f + 1, 2)
  soulStream(cv, px3, py3, MX - 2, MY + 14, f + 2, 3)
  -- из окон тоже тянутся тонкие нити
  soulStream(cv, 84, 52, MX - 8, MY + 4, f + 3, 4)
  return cv
end

-- =====================================================================
-- Сцена 3: герой на краю крыши смотрит вверх на Луноликого
-- =====================================================================
local function hero(cv, x, base, f)
  -- ~50 px: стоит на краю крыши в три четверти спиной к зрителю, голова запрокинута к луне (вверх-вправо).
  -- Длинный плащ и шарф треплет ветер (кадры), в груди светится красная душа.
  local wind = ({ 0, 1, 2, 1 })[f + 1]
  local K = "n0"
  -- плащ: широкий, развевается вправо рваным краем
  poly(cv, { {x - 7, base - 38}, {x + 7, base - 38}, {x + 12 + wind, base - 20}, {x + 19 + wind * 2, base - 6},
             {x + 22 + wind * 2, base - 1 + (f % 2)}, {x + 15 + wind, base - 3}, {x + 11 + wind, base}, {x + 6, base - 2},
             {x - 8, base - 2}, {x - 10, base - 20} }, K)
  -- сапоги
  rect(cv, x - 5, base - 10, x - 2, base, K); rect(cv, x + 2, base - 10, x + 5, base, K)
  rect(cv, x - 6, base - 1, x - 2, base, K); rect(cv, x + 2, base - 1, x + 6, base, K)
  -- плечи
  poly(cv, { {x - 9, base - 36}, {x + 8, base - 36}, {x + 7, base - 30}, {x - 9, base - 30} }, K)
  -- рука сжата в кулак у бедра, вторая придерживает плащ
  line(cv, x - 9, base - 33, x - 11, base - 20, K); line(cv, x - 10, base - 33, x - 12, base - 20, K)
  ellipse(cv, x - 11.5, base - 19, 2, 2, K)
  -- шея и голова, запрокинута вверх-вправо
  rect(cv, x - 1, base - 40, x + 2, base - 36, K)
  ellipse(cv, x + 1, base - 44, 5, 5.5, K)
  put(cv, x + 6, base - 46, K); put(cv, x + 6, base - 45, K)                 -- нос/подбородок смотрит вверх
  -- волосы: пряди по ветру влево-назад
  for i = 0, 5 do
    local yy = base - 47 + i + ((i + f) % 2)
    line(cv, x - 4, yy, x - 7 - (i % 3) - wind, yy + 1, K)
  end
  -- шарф: лента от шеи по ветру вправо, два хвоста
  local sx, sy = x + 2, base - 38
  rect(cv, x - 3, base - 39, x + 4, base - 36, "s1")
  for tail = 0, 1 do
    for i = 0, 26 - tail * 6 do
      local yy = sy + tail * 2 + math.sin(i * 0.42 + f * 1.6 + tail) * (1 + i * 0.1) + i * 0.12
      local c = (i % 7 == 6) and "m2" or "s1"
      put(cv, sx + i, yy, c); if i < 18 - tail * 4 then put(cv, sx + i, yy + 1, "m2") end
    end
  end
  -- кайма: луна справа сверху
  local body = is(K)
  rim(cv, x - 16, base - 54, x + 30, base, body, 1, 0, "m3")
  rim(cv, x - 16, base - 54, x + 30, base, body, 0, -1, "r3")
  rim(cv, x - 16, base - 54, x + 30, base, is(K, "m3", "r3"), 1, -1, "r2")
  -- лицо в профиль ловит свет
  put(cv, x + 5, base - 46, "m5"); put(cv, x + 5, base - 45, "m4"); put(cv, x + 4, base - 43, "m3"); put(cv, x + 6, base - 47, "m4")
  put(cv, x + 3, base - 46, "e2")                                                -- блеск глаза
  -- складки плаща
  line(cv, x + 2, base - 28, x + 8 + wind, base - 6, "b1"); line(cv, x - 4, base - 28, x - 5, base - 4, "b1")
  -- душа: красное сердце сквозь плащ, пульсирует
  local hx, hy = x - 1, base - 27
  local glow = (f % 2 == 0) and 5 or 6
  for yy = -glow, glow do for xx = -glow - 1, glow + 1 do
    if xx * xx + yy * yy * 1.4 < glow * glow and dither(hx + xx, hy + yy) and get(cv, hx + xx, hy + yy) == K then put(cv, hx + xx, hy + yy, "s1") end
  end end
  local heart = { ".##.##.", "#######", "#######", ".#####.", "..###..", "...#..." }
  for ry, row in ipairs(heart) do
    for rx = 1, #row do
      if row:sub(rx, rx) == "#" then
        local c = (ry == 2 and rx == 2) and "s4" or (ry <= 2 and rx <= 3) and "s3" or (ry >= 4 and rx >= 4) and "s1" or "s2"
        put(cv, hx - 3 + rx - 1, hy - 3 + ry - 1, c)
      end
    end
  end
end

local function scene3(f)
  local W, H = 240, 160
  local cv = canvas(W, H)
  local MX, MY, MR = 150, 40, 28

  sky(cv, 0, H - 1, { "k0", "k1", "k2", "k3", "k4" })
  clouds(cv, 0, 110, MX, MY, 37, f * 1.5, 44, 14, 44, 30)
  halo(cv, MX, MY, MR, 12)
  moonDisk(cv, MX, MY, MR, { {0.3, -0.5, 0.13}, {-0.5, 0.4, 0.15}, {0.6, 0.35, 0.1} }, 1.1)
  moonFace(cv, MX, MY, 1.2, 0.35 + (f % 2) * 0.25, true)
  tophat(cv, MX + 2, MY - MR + 6, 32, 26, 58, 0.12)

  -- нити душ поднимаются из далёкого города к луне
  for k = 0, 4 do
    local sx = 30 + k * 46
    for i = 0, 40 do
      local t = i / 40
      local x = sx + (MX - sx) * t ^ 1.6 + math.sin(t * 7 + k) * 4
      local y = 132 - (132 - (MY + MR - 4)) * t
      if (i + f * 2 + k) % 5 == 0 then put(cv, x, y, "s2") elseif (i + f) % 3 == 0 then put(cv, x, y, "s1") end
    end
  end
  -- дальний город внизу, в тумане
  city(cv, -4, W + 4, 140, 8, 22, 51, "b2", "r1", f, 0.94)
  fog(cv, 126, 142, f, 9)
  -- крыша, на которой стоит герой: ближний план слева
  poly(cv, { {-2, 160}, {-2, 136}, {40, 128}, {96, 136}, {104, 160} }, "b0")
  rim(cv, 0, 126, 104, 159, is("b0"), 0, -1, "r2")
  for x = 4, 94, 6 do line(cv, x, 140 + math.floor(x / 20), x + 3, 160, "b1") end   -- черепица
  rect(cv, 16, 116, 22, 132, "b0"); rect(cv, 15, 115, 23, 116, "b1")                -- труба
  put(cv, 18, 112 - f, "f2"); put(cv, 19, 108 - f, "f1")                           -- дымок
  hero(cv, 58, 131, f)
  -- пепел летит по ветру
  for k = 0, 14 do
    local x = (hash(k, 1, 91) * W + f * 5 + k * 3) % W
    local y = hash(k, 2, 91) * 120 + f
    put(cv, x, y, k % 3 == 0 and "c5" or "c3")
  end
  return cv
end

-- ---------- вывод ----------
local function toImage(cv)
  local img = Image(cv.w, cv.h, ColorMode.RGB)
  img:clear(app.pixelColor.rgba(0, 0, 0, 255))
  for y = 0, cv.h - 1 do
    for x = 0, cv.w - 1 do
      local c = cv.d[y * cv.w + x + 1]
      if c then
        local rgb = C[c]
        assert(rgb, "unknown color " .. tostring(c))
        img:putPixel(x, y, app.pixelColor.rgba(rgb[1], rgb[2], rgb[3], 255))
      end
    end
  end
  return img
end

local function saveScene(name, fn)
  local imgs = {}
  for f = 0, FRAMES - 1 do imgs[#imgs + 1] = toImage(fn(f)) end
  local fw, fh = imgs[1].width, imgs[1].height
  local strip = Image(fw * FRAMES, fh, ColorMode.RGB)
  for i, img in ipairs(imgs) do strip:drawImage(img, Point((i - 1) * fw, 0)) end
  os.execute('mkdir -p "' .. ART .. '"')
  strip:saveAs(ART .. name .. ".png")
  print("saved " .. name .. " " .. fw .. "x" .. fh)
  return imgs[1]
end

local a = saveScene("Scene1", scene1)
local b = saveScene("Scene2", scene2)
local c = saveScene("Scene3", scene3)
local prev = Image(288, a.height + b.height + c.height + 8, ColorMode.RGB)
prev:clear(app.pixelColor.rgba(20, 20, 24, 255))
prev:drawImage(a, Point(0, 0)); prev:drawImage(b, Point(0, a.height + 4)); prev:drawImage(c, Point(0, a.height + b.height + 8))
prev:saveAs(PREVIEW)
print("preview " .. PREVIEW)
