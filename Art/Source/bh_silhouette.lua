-- Общая отрисовка врагов в стиле фонов: тёмные силуэты с красной контровой каймой, яркие только
-- главные детали, переходы тонов упорядоченным дизерингом. Используют gen_enemies.lua и gen_moonface.lua.
local S = dofile("/home/lotwy/black_heart/Art/Source/bh_shade.lua")
local L = S.L
local W, H = 80, 80

local function hex(h)
  return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) }
end
local P = {
  dk0 = "#0b080e", dk1 = "#141017", dk2 = "#1e1722", dk3 = "#2c2230",      -- тёмная ткань/дым
  fl0 = "#1c070c", fl1 = "#300a12", fl2 = "#4a1019", fl3 = "#6a1722",      -- тёмная плоть
  hn0 = "#16110f", hn1 = "#2a211d", hn2 = "#40332c", hn3 = "#5a4a3e",      -- рог
  bn1 = "#5a4c4a", bn2 = "#8a7c74", bn3 = "#b8aa9c", bn4 = "#ddd0bf", bn5 = "#f7efe2",   -- кость
  sc1 = "#6e5c5e", sc2 = "#a8989a", sc3 = "#d8ccc6", sc4 = "#f2eae4", sc5 = "#ffffff",   -- белок глаза
  rim1 = "#4a1018", rim2 = "#8c1a24", rim3 = "#d8382c",                    -- красная кайма
  brim = "#f0a08c",                                                        -- кайма на кости
  ir1 = "#7a0c16", ir2 = "#c41c24", ir3 = "#ff4a3a", ir4 = "#ff9a6e",      -- радужка
  vn = "#b02430",                                                          -- прожилки
  gl1 = "#ff4a3a", gl2 = "#ffb08a", gl3 = "#fff4d0",                       -- свечение
  gem1 = "#6a0a14", gem2 = "#d81c28", gem3 = "#ff8a7a",
}
for k, v in pairs(P) do L.COLORS[k] = hex(v) end

-- Материалы: dark-материалы — тон по вертикали с дизерингом; bright — плоский cel к нижнему-правому краю
local MAT = {
  cloth = { kind = "dark", ramp = { "dk0", "dk1", "dk2", "dk3" }, rim = { "rim1", "rim2" } },
  flesh = { kind = "dark", ramp = { "fl0", "fl1", "fl2", "fl3" }, rim = { "rim2", "rim3" } },
  horn  = { kind = "dark", ramp = { "hn0", "hn1", "hn2", "hn3" }, rim = { "rim1", "rim2" } },
  bone  = { kind = "bright", ramp = { "bn1", "bn2", "bn3", "bn4", "bn5" }, rim = { "brim" } },
  white = { kind = "bright", ramp = { "sc1", "sc2", "sc3", "sc4", "sc5" }, rim = { "sc4" } },
}

local BAYER = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 }
local function dither(x, y, d) return (BAYER[(y % 4) * 4 + (x % 4) + 1] + 0.5) / 16 < d end
local N4 = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }

local function bbox(mask)
  local x0, y0, x1, y1 = 1e9, 1e9, -1e9, -1e9
  L.each(mask, function(x, y)
    x0 = math.min(x0, x); x1 = math.max(x1, x); y0 = math.min(y0, y); y1 = math.max(y1, y)
  end)
  return x0, y0, x1, y1
end

-- parts: { name, mat, z, mask, noRim }
local function render(parts)
  table.sort(parts, function(a, b) return a.z < b.z end)
  local owner, out = {}, L.grid(W, H)
  for _, p in ipairs(parts) do L.each(p.mask, function(x, y) owner[y * W + x] = p end) end

  for _, p in ipairs(parts) do
    local m, mat = p.mask, MAT[p.mat]
    local _, y0, _, y1 = bbox(m)
    local hgt = math.max(1, y1 - y0)
    L.each(m, function(x, y)
      if owner[y * W + x] ~= p then return end
      local r, c = mat.ramp
      if mat.kind == "dark" then
        local fy = (y - y0) / hgt
        c = r[2]
        if fy < 0.3 and dither(x, y, (0.3 - fy) / 0.3 * 0.7) then c = r[3] end
        if fy > 0.55 and dither(x, y, (fy - 0.55) / 0.45 * 0.85) then c = r[1] end
      else
        local k = 0   -- шагов до края по диагонали вниз-вправо: чем меньше, тем глубже тень
        while k < 6 and L.get(m, x + k + 1, y + k + 1) do k = k + 1 end
        if k >= 5 then c = r[4]
        elseif k == 4 then c = dither(x, y, 0.5) and r[4] or r[3]
        elseif k == 3 then c = r[3]
        elseif k == 2 then c = dither(x, y, 0.5) and r[3] or r[2]
        else c = r[2] end
        if not L.get(m, x, y - 1) or not L.get(m, x - 1, y) then c = r[5] end
        if k == 0 and not L.get(m, x + 1, y + 1) and not L.get(m, x, y + 1) then c = r[1] end
      end
      L.set(out, x, y, c)
    end)
  end

  -- граница между деталями: пиксель нижней детали у верхней — почти чёрный
  local border = {}
  L.each(out, function(x, y)
    local p = owner[y * W + x]
    for _, o in ipairs(N4) do
      local q = owner[(y + o[2]) * W + x + o[1]]
      if q and q ~= p and q.z > p.z and not q.noBorder and not p.noBorder then border[#border + 1] = { x, y }; break end
    end
  end)
  for _, b in ipairs(border) do L.set(out, b[1], b[2], "dk0") end

  -- контровая кайма: пиксели силуэта, у которых сверху или сбоку пусто
  local rim = {}
  L.each(out, function(x, y)
    local p = owner[y * W + x]
    if not p or p.noRim then return end
    local up = not owner[(y - 1) * W + x]
    local side = not owner[y * W + x - 1] or not owner[y * W + x + 1]
    if up or side then
      local rr = MAT[p.mat].rim
      rim[#rim + 1] = { x, y, up and rr[#rr] or rr[1] }
      if up and MAT[p.mat].kind == "dark" and owner[(y + 1) * W + x] == p then rim[#rim + 1] = { x, y + 1, rr[1] } end
    end
  end)
  for _, q in ipairs(rim) do L.set(out, q[1], q[2], q[3]) end
  return out, owner
end

local function mask() return L.grid(W, H) end
local function part(parts, name, mat, z, m, o)
  local p = { name = name, mat = mat, z = z, mask = m }
  for k, v in pairs(o or {}) do p[k] = v end
  parts[#parts + 1] = p
  return p
end
local function paint(out, owner, names, x, y, c)
  if x < 0 or y < 0 or x >= W or y >= H then return end
  local p = owner[y * W + x]
  if p and (not names or names[p.name]) then L.set(out, x, y, c) end
end
local function paintLine(out, owner, names, pts, c)
  local m = mask()
  L.polyline(m, pts, "x")
  L.each(m, function(x, y) paint(out, owner, names, x, y, c) end)
end
local function mirX(x) return W - 1 - x end
local function mirPts(pts) local r = {} for i, p in ipairs(pts) do r[i] = { W - p[1], p[2] } end return r end
-- Чёрный контур и снаружи белая обводка 1 px — враг не сливается с тёмным фоном
local function finish(out, eyes) return L.outline(L.outline(out, 1, "K"), 1, "W"), { eyes = eyes } end

-- Превью: кадры на фоне зала (кусок с окном), увеличение x3; rows — список рядов кадров
local function preview(rows, path)
  local bg = Image{ fromFile = "/home/lotwy/black_heart/Assets/Resources/Art/Background/Hall.png" }
  local SC, cols = 3, 1
  for _, r in ipairs(rows) do cols = math.max(cols, #r) end
  local sheet = Image(cols * (W * SC + 4) - 4, #rows * (H * SC + 4) - 4, ColorMode.RGB)
  sheet:clear(app.pixelColor.rgba(20, 14, 20, 255))
  for r, frames in ipairs(rows) do
    for i, g in ipairs(frames) do
      local img = Image(W, H, ColorMode.RGB)
      img:drawImage(bg, Point(-120, 0))
      img:drawImage(L.toImage(g), Point(0, 0))
      for y = 0, H - 1 do for x = 0, W - 1 do
        local c = img:getPixel(x, y)
        for dy = 0, SC - 1 do for dx = 0, SC - 1 do
          sheet:drawPixel((i - 1) * (W * SC + 4) + x * SC + dx, (r - 1) * (H * SC + 4) + y * SC + dy, c)
        end end
      end end
    end
  end
  os.execute('mkdir -p "' .. path:match("(.*/)") .. '"')
  sheet:saveAs(path)
end

return {
  S = S, L = L, W = W, H = H, MAT = MAT, dither = dither, bbox = bbox, render = render,
  mask = mask, part = part, paint = paint, paintLine = paintLine, mirX = mirX, mirPts = mirPts,
  finish = finish, preview = preview,
}
