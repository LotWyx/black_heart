-- Объёмное (cel) затенение для спрайтов врагов в стиле Луноликого.
-- Каждая деталь — маска; по расстоянию до края строится «купол», по нормали и свету сверху-слева
-- выбирается один из 5 тонов палитры детали. Между деталями — тёмная граница, снаружи — контур.
local L = dofile("/home/lotwy/black_heart/Art/Source/bh_lib.lua")
local S = { L = L, W = 80, H = 80 }

local function hex(h)
  return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) }
end

-- Палитра Луноликого: почти чёрный контур, серые, кроваво-красные
S.PALETTE = {
  k = "#08080C", d = "#222228", n = "#484850", m = "#808088", i = "#C0C0C6", e = "#DEDEE2", f = "#FAFAFA",
  c = "#46040C", b = "#820C18", a = "#C41C24", l = "#E82424", h = "#EE4634", j = "#FF966E",
}
for key, value in pairs(S.PALETTE) do L.COLORS[key] = hex(value) end

-- Тоновые ряды материалов: глубокая тень, тень, основа, свет, блик
S.RAMP = {
  cloth = { "k", "d", "d", "n", "m" },
  bone = { "n", "m", "i", "e", "f" },
  pale = { "m", "i", "e", "f", "f" },
  flesh = { "c", "b", "a", "l", "h" },
  horn = { "k", "d", "n", "m", "i" },
  membrane = { "k", "c", "b", "b", "a" },
  void = { "k", "k", "k", "d", "d" },
  bonedark = { "n", "m", "i", "i", "e" },   -- кость без ярких бликов
  fleshdark = { "k", "c", "b", "b", "a" },  -- тёмная плоть (веки)
}

local LX, LY, LZ = -0.5, -0.7, 0.55   -- свет сверху-слева-спереди (ось y вниз)
do
  local len = math.sqrt(LX * LX + LY * LY + LZ * LZ)
  LX, LY, LZ = LX / len, LY / len, LZ / len
end

local N4 = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }

function S.mask() return L.grid(S.W, S.H) end

local function inside(x, y) return x >= 0 and y >= 0 and x < S.W and y < S.H end

-- Расстояние до края маски (шаги по 8 соседям)
function S.distance(mask)
  local W, d, queue, head = S.W, {}, {}, 1
  L.each(mask, function(x, y)
    for _, o in ipairs(N4) do
      if not L.get(mask, x + o[1], y + o[2]) then
        d[y * W + x] = 1
        queue[#queue + 1] = { x, y }
        break
      end
    end
  end)
  while head <= #queue do
    local p = queue[head]
    head = head + 1
    local dv = d[p[2] * W + p[1]]
    for oy = -1, 1 do
      for ox = -1, 1 do
        local nx, ny = p[1] + ox, p[2] + oy
        if (ox ~= 0 or oy ~= 0) and L.get(mask, nx, ny) and not d[ny * W + nx] then
          d[ny * W + nx] = dv + 1
          queue[#queue + 1] = { nx, ny }
        end
      end
    end
  end
  return d
end

-- parts: { name, ramp, z, mask, round (радиус «купола»), bias (сдвиг тона), noBorder }
-- Возвращает сетку меток и owner[индекс] -> деталь
function S.render(parts)
  local W = S.W
  table.sort(parts, function(a, b) return a.z < b.z end)
  local owner = {}
  for _, part in ipairs(parts) do
    L.each(part.mask, function(x, y) owner[y * W + x] = part end)
  end

  local out = L.grid(S.W, S.H)
  for _, part in ipairs(parts) do
    local dist = S.distance(part.mask)
    local R = part.round or 8
    local function height(x, y)
      local dv = dist[y * W + x]
      if not dv then return 0 end
      local t = math.min(dv, R) / R
      return R * math.sqrt(1 - (1 - t) * (1 - t))
    end
    local ramp = S.RAMP[part.ramp] or part.ramp
    L.each(part.mask, function(x, y)
      if owner[y * W + x] ~= part then return end
      local gx = (height(x + 1, y) - height(x - 1, y)) * 0.5
      local gy = (height(x, y + 1) - height(x, y - 1)) * 0.5
      local len = math.sqrt(gx * gx + gy * gy + 1)
      local s = (-gx * LX - gy * LY + LZ) / len
      local tone = s > 0.86 and 5 or s > 0.66 and 4 or s > 0.4 and 3 or s > 0.12 and 2 or 1
      tone = math.max(1, math.min(5, tone + (part.bias or 0)))
      L.set(out, x, y, ramp[tone])
    end)
  end

  -- Тёмная граница: пиксель нижней детали рядом с верхней
  local borders = {}
  L.each(out, function(x, y)
    local p = owner[y * W + x]
    if p.noBorder then return end
    for _, o in ipairs(N4) do
      local nx, ny = x + o[1], y + o[2]
      local q = inside(nx, ny) and owner[ny * W + nx]
      if q and q ~= p and q.z > p.z and not q.noBorder then
        borders[#borders + 1] = { x, y }
        break
      end
    end
  end)
  for _, b in ipairs(borders) do L.set(out, b[1], b[2], "k") end
  return out, owner
end

-- Рисовать только поверх перечисленных деталей
function S.paint(out, owner, names, x, y, c)
  if not inside(x, y) then return end
  local p = owner[y * S.W + x]
  if p and (names == nil or names[p.name]) then L.set(out, x, y, c) end
end

function S.paintMask(out, owner, names, mask, c)
  L.each(mask, function(x, y, label) S.paint(out, owner, names, x, y, c or label) end)
end

-- Ломаная только поверх деталей
function S.paintLine(out, owner, names, pts, c, thick)
  local m = S.mask()
  L.polyline(m, pts, "x", thick)
  S.paintMask(out, owner, names, m, c)
end

-- Зеркально по вертикальной оси холста
function S.mx(x) return S.W - 1 - x end
function S.mirrorPts(pts)
  local r = {}
  for i, p in ipairs(pts) do r[i] = { S.W - p[1], p[2] } end
  return r
end

-- Утолщённая кривая (квадратичная Безье) с сужением: рога, щупальца, кости крыльев
function S.taper(mask, p0, p1, p2, r0, r1, steps)
  steps = steps or 40
  for i = 0, steps do
    local t = i / steps
    local u = 1 - t
    local x = u * u * p0[1] + 2 * u * t * p1[1] + t * t * p2[1]
    local y = u * u * p0[2] + 2 * u * t * p1[2] + t * t * p2[2]
    local r = r0 + (r1 - r0) * t
    L.ellipse(mask, x, y, math.max(0.6, r), math.max(0.6, r), "x")
  end
end

-- ---------- анимации из готовых кадров ----------

local function outerOutline(g)
  local set = {}
  L.each(g, function(x, y, c)
    if c == "k" then
      for _, o in ipairs(N4) do
        if not L.get(g, x + o[1], y + o[2]) then set[y * S.W + x] = true; break end
      end
    end
  end)
  return set
end

local function withOuter(g, label)
  local n, set = L.copy(g), outerOutline(g)
  for key in pairs(set) do L.set(n, key % S.W, key // S.W, label) end
  return n
end

local function eyeSet(eyes)
  local set = {}
  for _, p in ipairs(eyes or {}) do set[p[2] * S.W + p[1]] = true end
  return set
end

-- Замах C: глаза вспыхивают белым с оранжевым ореолом
local function glowEyes(g, eyes)
  local n, set = L.copy(g), eyeSet(eyes)
  for key in pairs(set) do
    local x, y = key % S.W, key // S.W
    L.set(n, x, y, "W")
    for _, o in ipairs(N4) do
      local nx, ny = x + o[1], y + o[2]
      if L.get(g, nx, ny) and not set[ny * S.W + nx] then L.set(n, nx, ny, "O") end
    end
  end
  return n
end

-- Урон A: белый силуэт, контур и глаза чёрные
local function whiteSilhouette(g, eyes)
  local n, outer, set = L.grid(S.W, S.H), outerOutline(g), eyeSet(eyes)
  L.each(g, function(x, y)
    local key = y * S.W + x
    L.set(n, x, y, (outer[key] or set[key]) and "K" or "W")
  end)
  return n
end

local function recolorEyes(g, eyes, label)
  local n = L.copy(g)
  for _, p in ipairs(eyes or {}) do L.set(n, p[1], p[2], label) end
  return n
end

-- Трещины поперёк тела (для поражения)
local function cracked(g)
  local n = L.copy(g)
  local minx, miny, maxx, maxy = L.bounds(g)
  local cx, cy = (minx + maxx) // 2, (miny + maxy) // 2
  local m = L.grid(S.W, S.H)
  L.polyline(m, { { minx + 6, cy - 4 }, { cx - 9, cy + 2 }, { cx - 3, cy - 5 }, { cx + 4, cy + 4 }, { maxx - 6, cy - 3 } }, "x")
  L.polyline(m, { { cx - 3, cy - 5 }, { cx - 1, cy - 15 }, { cx + 3, cy - 20 } }, "x")
  L.polyline(m, { { cx + 4, cy + 4 }, { cx + 1, cy + 14 } }, "x")
  L.each(m, function(x, y) if L.get(n, x, y) then L.set(n, x, y, "k") end end)
  return n
end

-- build(state) -> сетка с контуром, meta { eyes = {{x, y}, ...} }
function S.animations(name, build, opts)
  local base, meta = build({})
  local idleGrid = build(opts.idle or {})
  local idle = { base, L.shift(idleGrid, 0, -1) }

  local windup = {
    withOuter(base, "O"),
    L.outline(L.fillAll(L.outline(base, 2, "O"), "O"), 1, "K"),
    glowEyes(base, meta.eyes),
  }

  local hurt = { whiteSilhouette(base, meta.eyes), recolorEyes(base, meta.eyes, "W") }

  local laugh = {}
  for i, st in ipairs(opts.laugh) do
    laugh[i] = L.shift((build(st)), 0, ({ 0, -2, 0 })[i] or 0)
  end

  local minx, miny, maxx, maxy = L.bounds(base)
  local cy = (miny + maxy) // 2
  local bar = L.grid(S.W, S.H)
  L.rect(bar, minx, cy - 3, maxx, cy + 2, "W")
  bar = L.outline(bar, 1, "K")
  local dot = L.grid(S.W, S.H)
  L.rect(dot, 39, cy - 1, 40, cy, "W")
  local defeat = { cracked(base), whiteSilhouette(base, {}), bar, dot }

  L.save("Enemies/" .. name, { base })
  L.save("Enemies/" .. name .. "_idle", idle, { 0.4 })
  L.save("Enemies/" .. name .. "_windup", windup, { 0.1 })
  L.save("Enemies/" .. name .. "_hurt", hurt, { 0.05 })
  L.save("Enemies/" .. name .. "_laugh", laugh, { 0.1 })
  L.save("Enemies/" .. name .. "_defeat", defeat, { 0.12, 0.05, 0.05, 0.05 })
end

return S
