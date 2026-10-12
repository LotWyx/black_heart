-- Пули: в стиле врагов, запасные цветные, вспышка появления, трещина осколка. Кадры 17x17, по 4.
-- Рисуется как враги и фоны: силуэт → объём по шагам до нижнего-правого края (5 тонов с дизерингом),
-- светлая кайма сверху-слева, отражённый свет снизу, блики; вокруг — ореол цвета типа (дизеринг).
-- Цвет задаёт тип и одинаков у всех врагов: белый — обычный, фиолетовый — «из двух частей»
-- (при ударе распадается на половинки BulletPurpleHalf, вторую код отражает), синий — «не рубить».
local L = dofile("/home/lotwy/black_heart/Art/Source/bh_lib.lua")
local OUT = "/home/lotwy/black_heart/Art/Preview/"   -- превью; сами пули — в Assets/Resources/Art/Projectiles/

local N = 17
local function hex(h) return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) } end
for k, v in pairs({
  w1 = "#3c3c58", w2 = "#7a7a98", w3 = "#b8b8d0", w4 = "#e8e8f4", w5 = "#ffffff", wg = "#2a2a40",
  p1 = "#2a0c48", p2 = "#5a1e92", p3 = "#8c3ccc", p4 = "#b876f0", p5 = "#ecd2ff", pg = "#26083c",
  b1 = "#0a2058", b2 = "#1650b0", b3 = "#2e8ef0", b4 = "#7cc8ff", b5 = "#dcf4ff", bg = "#081a42",
}) do L.COLORS[k] = hex(v) end
-- r[1] глубокая тень … r[5] блик, r.g — ореол
local RAMPS = {
  White = { "w1", "w2", "w3", "w4", "w5", g = "wg" },
  Purple = { "p1", "p2", "p3", "p4", "p5", g = "pg" },
  Blue = { "b1", "b2", "b3", "b4", "b5", g = "bg" },
}
local BAYER = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 }
local function dither(x, y, d) return (BAYER[(y % 4) * 4 + (x % 4) + 1] + 0.5) / 16 < d end

local function grid() return L.grid(N, N) end
local function inDisc(x, y, cx, cy, r) local dx, dy = x + 0.5 - cx, y + 0.5 - cy; return dx * dx + dy * dy <= r * r end
local function disc(m, cx, cy, r) for y = 0, N - 1 do for x = 0, N - 1 do if inDisc(x, y, cx, cy, r) then L.set(m, x, y, "x") end end end end
local function inside(x, y) return x >= 0 and y >= 0 and x < N and y < N end
local function rows(m, list, ox, oy)
  for y, row in ipairs(list) do for x = 1, #row do
    if row:sub(x, x) ~= "." and inside(ox + x - 1, oy + y - 1) then L.set(m, ox + x - 1, oy + y - 1, "x") end
  end end
end
local function line(m, x0, y0, x1, y1, w)
  local n = math.ceil(math.max(math.abs(x1 - x0), math.abs(y1 - y0)) * 2) + 1
  for i = 0, n do
    local t = i / n
    local x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
    if w then disc(m, x, y, w)
    elseif inside(math.floor(x + 0.5), math.floor(y + 0.5)) then L.set(m, math.floor(x + 0.5), math.floor(y + 0.5), "x") end
  end
end
local function discMask(cx, cy, r) local g = grid(); disc(g, cx, cy, r); return g end

-- Объём силуэта: чем дальше до нижнего-правого края по диагонали, тем светлее; кайма света и отражённый свет
local function body(mask, r, out, lift)
  out = out or grid()
  lift = lift or 0
  L.each(mask, function(x, y)
    local k = 0
    while k < 6 and L.get(mask, x + k + 1, y + k + 1) do k = k + 1 end
    k = k + lift
    local c
    if k >= 5 then c = r[4]
    elseif k == 4 then c = dither(x, y, 0.5) and r[4] or r[3]
    elseif k >= 2 then c = r[3]
    elseif k == 1 then c = dither(x, y, 0.5) and r[3] or r[2]
    else c = r[2] end
    local up, left = not L.get(mask, x, y - 1), not L.get(mask, x - 1, y)
    local down, right = not L.get(mask, x, y + 1), not L.get(mask, x + 1, y)
    if (up or left) and not (down and right) then c = (up and left) and r[5] or r[4] end
    if down and right then c = r[1] end
    L.set(out, x, y, c)
  end)
  return out
end
local function paint(out, x, y, c) if inside(x, y) and L.get(out, x, y) then L.set(out, x, y, c) end end
local function paintAny(out, x, y, c) if inside(x, y) then L.set(out, x, y, c) end end

-- Контур и ореол: чёрная обводка 1 px, вокруг — редкие пиксели свечения цвета типа
local function finish(out, r)
  local g = L.outline(out, 1, "K")
  local glow = {}
  L.each(g, function(x, y)
    for _, o in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
      local nx, ny = x + o[1], y + o[2]
      if inside(nx, ny) and not L.get(g, nx, ny) and (nx + ny) % 2 == 0 then glow[#glow + 1] = { nx, ny } end
    end
  end)
  for _, p in ipairs(glow) do L.set(g, p[1], p[2], r.g) end
  return g
end

-- ================= ПРИЗРАК =================
-- белый: огонёк-душа — круглое тело, три языка пламени, горячая сердцевина, лицо
local function ghostWhite(r, f)
  local m = grid()
  disc(m, 8.5, 10.5, 4.8)
  local sway = ({ 0, 1, 0, -1 })[f + 1]
  for i, t in ipairs({ { 5.5, 4.5, -1 }, { 8.5, 1.5, 0 }, { 11.5, 4.5, 1 } }) do
    local tipX = t[1] + sway * (i == 2 and 1 or 0.5) + t[3] * ((f % 2 == 0) and 0 or 0.5)
    for y = math.floor(t[2]), 9 do
      local k = (y - t[2]) / (9 - t[2])
      local w = 0.6 + k * 2.2
      local cx = tipX + (t[1] - tipX) * k
      for x = math.floor(cx - w), math.ceil(cx + w) do
        if math.abs(x + 0.5 - cx) <= w and inside(x, y) then L.set(m, x, y, "x") end
      end
    end
  end
  local out = body(m, r, nil, 1)
  for y = 0, N - 1 do for x = 0, N - 1 do
    if L.get(out, x, y) and inDisc(x, y, 8.5 + sway * 0.3, 9, 2.6) then L.set(out, x, y, inDisc(x, y, 8.5, 9, 1.6) and r[5] or r[4]) end
  end end
  for _, e in ipairs({ { 6, 10 }, { 10, 10 } }) do
    paint(out, e[1], e[2], "K"); paint(out, e[1] + 1, e[2], "K"); paint(out, e[1], e[2] + 1, "K"); paint(out, e[1] + 1, e[2] + 1, "K")
    paint(out, e[1], e[2], r[5])
  end
  paint(out, 8, 13, "K"); paint(out, 9, 13, "K")
  paint(out, 5, 12, r[2]); paint(out, 12, 12, r[2])
  local sp = ({ { 2, 6 }, { 14, 8 }, { 2, 9 }, { 14, 5 } })[f + 1]
  paintAny(out, sp[1], sp[2], r[5])
  return finish(out, r)
end

-- маленькое привидение-«простыня»: купол, волнистый подол, глаза смотрят вбок
local SHEET = {
  "..###..",
  ".#####.",
  "#######",
  "#######",
  "#######",
  "#######",
  "#######",
  "#######",
  "#######",
}
local function sheetGhost(out, r, ox, oy, f, mirror)
  local m = grid()
  rows(m, SHEET, ox, oy)
  local hem = (f % 2 == 0) and { 0, 2, 4, 6 } or { 1, 3, 5 }
  for _, hx in ipairs(hem) do if inside(ox + hx, oy + #SHEET) then L.set(m, ox + hx, oy + #SHEET, "x") end end
  body(m, r, out)
  local ex = mirror and { ox + 1, ox + 4 } or { ox + 2, ox + 4 }
  for _, x in ipairs(ex) do
    paint(out, x, oy + 3, r[5]); paint(out, x + 1, oy + 3, "K")
    paint(out, x, oy + 4, "K"); paint(out, x + 1, oy + 4, "K")
  end
  paint(out, ox + 3, oy + 6, "K")
  paint(out, ox + 2, oy + 1, r[5])
end
-- фиолетовый: два привидения рядом с зазором — при ударе разлетаются по одному
local function ghostPurple(r, f)
  local out = grid()
  local bob = (f == 1 or f == 2) and 1 or 0
  sheetGhost(out, r, 1, 3 + bob, f, true)
  sheetGhost(out, r, 9, 4 - bob, f + 1, false)
  return finish(out, r)
end
local function ghostHalf(r, f)
  local out = grid()
  sheetGhost(out, r, 5, 3 + ((f == 1 or f == 2) and 1 or 0), f, false)
  return finish(out, r)
end

-- синий: фонарь — кольцо, крыша, светящееся стекло с огоньком, рёбра решётки, ножки
local function ghostBlue(r, f)
  local m = grid()
  rows(m, {
    ".....###.....",
    "....##.##....",
    ".....###.....",
    "....#####....",
    "...#######...",
    "..#########..",
    "..#########..",
    "..#########..",
    "..#########..",
    "..#########..",
    "..#########..",
    "...#######...",
    "...##...##...",
  }, 2, 2)
  local out = body(m, r)
  for y = 8, 12 do for x = 5, 11 do
    local d = math.abs(x + 0.5 - 8.5) / 3.5 + math.abs(y + 0.5 - 10.5) / 3
    L.set(out, x, y, d < 0.6 and r[5] or d < 1.05 and r[4] or r[3])
  end end
  for y = 8, 12 do paint(out, 4, y, r[2]); paint(out, 12, y, r[1]) end
  local flame = ({ { { 8, 9 }, { 8, 10 } }, { { 7, 9 }, { 8, 10 } }, { { 8, 8 }, { 8, 9 }, { 8, 10 } }, { { 9, 9 }, { 8, 10 } } })[f + 1]
  for _, p in ipairs(flame) do L.set(out, p[1], p[2], r[5]) end
  for _, p in ipairs(flame) do   -- тёмная кромка пламени, чтобы оно читалось на светлом стекле
    for _, o in ipairs({ { 1, 0 }, { -1, 0 } }) do
      local x, y = p[1] + o[1], p[2] + o[2]
      if L.get(out, x, y) == r[5] then L.set(out, x, y, r[3]) end
    end
  end
  L.set(out, 8, 11, r[2])
  L.set(out, 6, 9, r[5])
  L.set(out, 6, 6, r[5]); L.set(out, 8, 2, r[5])
  return finish(out, r)
end

-- ================= ГЛАЗ =================
local LOOK = { { 0, 0 }, { 1.3, 0 }, { 0, 1.1 }, { -1.3, 0 } }
-- глазное яблоко: белок с объёмом, радужка с тёмным кольцом, зрачок, двойной блик
local function eyeball(out, r, cx, cy, rad, lx, ly, ir)
  local m = discMask(cx, cy, rad)
  body(m, { r[2], r[3], r[4], r[5], r[5] }, out)
  local ix, iy = cx + lx, cy + ly
  L.each(m, function(x, y)
    if inDisc(x, y, ix, iy, ir) then
      local d = math.sqrt((x + 0.5 - ix) ^ 2 + (y + 0.5 - iy) ^ 2) / ir
      L.set(out, x, y, d > 0.78 and r[1] or (d > 0.5 and dither(x, y, 0.5)) and r[2] or r[3])
    end
  end)
  L.each(m, function(x, y) if inDisc(x, y, ix, iy, math.max(0.9, ir * 0.42)) then L.set(out, x, y, "K") end end)
  paint(out, math.floor(ix - ir * 0.5), math.floor(iy - ir * 0.5), r[5])
  if ir > 2 then paint(out, math.floor(ix + ir * 0.4), math.floor(iy + ir * 0.4), r[4]) end
end
-- белый: большой глаз с прожилками и тенью века; взгляд бегает
local function eyeWhite(r, f)
  local out = grid()
  local lk = LOOK[f + 1]
  eyeball(out, r, 8.5, 8.5, 6.4, lk[1], lk[2], 3.2)
  for _, v in ipairs({ { 3, 8, 4.5, 9 }, { 3.5, 11.5, 5, 10.5 }, { 14, 7, 12.5, 8 }, { 13, 11.5, 11.5, 10.5 } }) do
    local tmp = grid(); line(tmp, v[1], v[2], v[3], v[4])
    L.each(tmp, function(x, y) paint(out, x, y, r[2]) end)
  end
  for x = 5, 12 do paint(out, x, 2, r[2]) end
  for x = 6, 11 do paint(out, x, 3, r[3]) end
  return finish(out, r)
end
-- маленький глаз (для фиолетовых); закрытый — веко и ресницы
local function smallEye(out, r, cx, f, closed)
  if closed then
    body(discMask(cx, 8.5, 3.9), r, out)
    for x = math.floor(cx - 4), math.floor(cx + 4) do paint(out, x, 9, "K") end
    for x = math.floor(cx - 2), math.floor(cx + 2), 2 do paintAny(out, x, 10, "K") end
  else
    local lk = LOOK[f + 1]
    eyeball(out, r, cx, 8.5, 3.9, lk[1] * 0.5, lk[2] * 0.5, 1.6)
  end
end
-- фиолетовый: пара глаз с зазором, моргают по очереди — при ударе разлетаются по одному
local function eyePurple(r, f)
  local out = grid()
  smallEye(out, r, 4.3, f, f == 1)
  smallEye(out, r, 12.7, f, f == 3)
  return finish(out, r)
end
local function eyeHalf(r, f)
  local out = grid()
  smallEye(out, r, 8.5, f, f == 2)
  return finish(out, r)
end
-- синий: оберег-«назар» — тёмный круг, белое и голубое кольца, зрачок, орнамент, бегущий блик
local function eyeBlue(r, f)
  local m = discMask(8.5, 8.5, 6.6)
  local out = body(m, { r[1], r[1], r[2], r[2], r[3] })
  L.each(m, function(x, y)
    if inDisc(x, y, 8.5, 8.5, 4.9) then L.set(out, x, y, (x + y < 16) and r[5] or r[4]) end
    if inDisc(x, y, 8.5, 8.5, 3.6) then L.set(out, x, y, (dither(x, y, 0.4) and x + y > 18) and r[2] or r[3]) end
    if inDisc(x, y, 8.5, 8.5, 1.8) then L.set(out, x, y, "K") end
  end)
  for i = 0, 7 do
    local a = i * math.pi / 4 + math.pi / 8
    paint(out, math.floor(8.5 + math.cos(a) * 5.8), math.floor(8.5 + math.sin(a) * 5.8), r[3])
  end
  local a = math.rad(-135 + f * 90)
  paint(out, math.floor(8.5 + math.cos(a) * 5.7), math.floor(8.5 + math.sin(a) * 5.7), r[5])
  paint(out, 7, 7, r[5])
  return finish(out, r)
end

-- ================= ЧЕРЕП =================
local SKULL = {
  "...#######...",
  "..#########..",
  ".###########.",
  ".###########.",
  ".###########.",
  ".###########.",
  ".###########.",
  ".###########.",
  "..#########..",
  "...#######...",
}
local JAW = { "...#######...", "....#####...." }
local function skullMask(m, ox, oy, open)
  rows(m, SKULL, ox, oy)
  local jy = oy + #SKULL + (open and 1 or 0)
  rows(m, JAW, ox, jy)
  return jy
end
-- детали черепа: глазницы с огоньками, нос-сердечко, зубы, трещинка
local function skullDetails(out, r, ox, oy, jy, keep)
  local function p(x, y, c) if not keep or keep(x, y) then paint(out, x, y, c) end end
  for _, ex in ipairs({ ox + 2, ox + 8 }) do
    for y = oy + 4, oy + 6 do for x = ex, ex + 2 do p(x, y, "K") end end
    p(ex + 1, oy + 6, r[3])
    p(ex, oy + 3, r[2]); p(ex + 2, oy + 3, r[2])          -- надбровья
  end
  p(ox + 6, oy + 7, "K"); p(ox + 6, oy + 8, "K")           -- нос
  for x = ox + 3, ox + 9, 2 do p(x, oy + 9, "K") end       -- верхние зубы
  for x = ox + 4, ox + 8, 2 do p(x, jy, "K") end           -- нижние зубы
  p(ox + 8, oy + 1, r[2]); p(ox + 9, oy + 2, r[2]); p(ox + 9, oy + 3, r[2])   -- трещинка
end
-- белый: череп клацает челюстью
local function skullWhite(r, f)
  local m = grid()
  local jy = skullMask(m, 2, 2, f % 2 == 1)
  local out = body(m, r)
  skullDetails(out, r, 2, 2, jy)
  return finish(out, r)
end
-- маленький череп (для фиолетовых): глазницы с огоньками, нос, зубы, челюсть клацает
local MINI_SKULL = {
  ".#####.",
  "#######",
  "#######",
  "#######",
  "#######",
  ".#####.",
}
local function miniSkull(out, r, ox, oy, open)
  local m = grid()
  rows(m, MINI_SKULL, ox, oy)
  local jy = oy + #MINI_SKULL + (open and 1 or 0)
  rows(m, { ".#####.", "..###.." }, ox, jy)
  body(m, r, out)
  for _, ex in ipairs({ ox + 1, ox + 4 }) do
    for y = oy + 2, oy + 3 do for x = ex, ex + 1 do paint(out, x, y, "K") end end
    paint(out, ex, oy + 3, r[3])                     -- огонёк в глазнице
  end
  paint(out, ox + 3, oy + 4, "K")                    -- нос
  paint(out, ox + 2, oy + 5, "K"); paint(out, ox + 4, oy + 5, "K")   -- зубы
  paint(out, ox + 3, jy, "K")
  paint(out, ox + 1, oy + 1, r[5])
end
-- фиолетовый: два маленьких черепа рядом, клацают по очереди — при ударе разлетаются по одному
local function skullPurple(r, f)
  local out = grid()
  local bob = (f == 1 or f == 2) and 1 or 0
  miniSkull(out, r, 1, 3 + bob, f == 1)
  miniSkull(out, r, 9, 4 - bob, f == 3)
  return finish(out, r)
end
local function skullHalf(r, f)
  local out = grid()
  miniSkull(out, r, 5, 3 + ((f == 1 or f == 2) and 1 or 0), f % 2 == 1)
  return finish(out, r)
end
-- синий: скрещённые кости с шишками на концах; по костям бежит блик
local function skullBlue(r, f)
  local m = grid()
  line(m, 4.5, 4.5, 12.5, 12.5, 1.1)
  line(m, 12.5, 4.5, 4.5, 12.5, 1.1)
  for _, e in ipairs({ { 3.6, 3.6, 1, 1 }, { 13.4, 3.6, -1, 1 }, { 3.6, 13.4, 1, -1 }, { 13.4, 13.4, -1, -1 } }) do
    disc(m, e[1] + e[3] * 1.3, e[2] - e[4] * 0.1, 1.45)     -- две шишки на конце кости
    disc(m, e[1] - e[3] * 0.1, e[2] + e[4] * 1.3, 1.45)
  end
  local out = body(m, r, nil, 1)
  local glint = ({ { 6, 6 }, { 7, 7 }, { 10, 10 }, { 11, 11 } })[f + 1]
  paint(out, glint[1], glint[2], r[5]); paint(out, 16 - glint[1], glint[2], r[5])
  paint(out, 8, 8, r[2])
  return finish(out, r)
end

-- ================= ЛУНОЛИКИЙ =================
-- белый: полумесяц с лицом (глаз, ухмылка), кратеры, мерцающая звёздочка
local function moonWhite(r, f)
  local m = grid()
  local bob = (f == 2) and -1 or 0
  for y = 0, N - 1 do for x = 0, N - 1 do
    if inDisc(x, y, 8.5, 8.5 + bob, 6.6) and not inDisc(x, y, 11.6, 7.4 + bob, 5.4) then L.set(m, x, y, "x") end
  end end
  local out = body(m, r, nil, 1)
  paint(out, 5, 7 + bob, "K"); paint(out, 6, 7 + bob, "K"); paint(out, 6, 6 + bob, (f == 1) and r[3] or "K")
  paint(out, 6, 11 + bob, "K"); paint(out, 7, 12 + bob, "K"); paint(out, 8, 12 + bob, "K")
  paint(out, 4, 10 + bob, r[2]); paint(out, 5, 4 + bob, r[2]); paint(out, 4, 5 + bob, r[3])
  local st = ({ { 14, 3 }, { 14, 14 }, { 13, 8 }, { 15, 5 } })[f + 1]
  paintAny(out, st[1], st[2], r[5]); paintAny(out, st[1], st[2] - 1, r[4]); paintAny(out, st[1], st[2] + 1, r[4])
  return finish(out, r)
end
-- половина полной луны (левая) с кратерами; mirror — правая
local function halfMoon(out, r, shift, mirror)
  local m = grid()
  L.each(discMask(8.5, 8.5, 6.6), function(x, y) if x <= 7 then L.set(m, (mirror and (16 - x) or x) + shift, y, "x") end end)
  body(m, r, out)
  local function px_(x, y, c) paint(out, (mirror and (16 - x) or x) + shift, y, c) end
  for _, c in ipairs({ { 5, 5 }, { 4, 10 }, { 6, 12 } }) do px_(c[1], c[2], r[1]); px_(c[1] - 1, c[2] - 1, r[4]); px_(c[1] + 1, c[2], r[2]) end
  px_(7, 7, r[2]); px_(7, 8, r[2])
end
-- фиолетовый: полная луна, расколотая на две половины с зазором; половинки расходятся
local function moonPurple(r, f)
  local out = grid()
  local gap = (f == 1 or f == 2) and 1 or 0
  halfMoon(out, r, -gap, false)
  halfMoon(out, r, gap + 1, true)
  return finish(out, r)
end
local function moonHalf(r, f)
  local out = grid()
  halfMoon(out, r, 3, false)
  local c = ({ { 7, 5 }, { 8, 8 }, { 7, 11 }, { 9, 6 } })[f + 1]
  paint(out, c[1], c[2], r[5])
  return finish(out, r)
end
-- синий: четырёхлучевая звезда со светящимся центром — пульсирует, по углам вспыхивают искорки
local function moonBlue(r, f)
  local m = grid()
  local long = ({ 7.2, 6.4, 5.6, 6.4 })[f + 1]
  for y = 0, N - 1 do for x = 0, N - 1 do
    local dx, dy = x + 0.5 - 8.5, y + 0.5 - 8.5
    if math.sqrt(math.abs(dx)) + math.sqrt(math.abs(dy)) <= math.sqrt(long) then L.set(m, x, y, "x") end
  end end
  local out = body(m, r, nil, 1)
  for y = 0, N - 1 do for x = 0, N - 1 do
    if L.get(out, x, y) and inDisc(x, y, 8.5, 8.5, 2) then L.set(out, x, y, inDisc(x, y, 8.5, 8.5, 1.1) and r[5] or r[4]) end
  end end
  local sp = ({ { 3, 3 }, { 13, 3 }, { 13, 13 }, { 3, 13 } })[f + 1]
  paintAny(out, sp[1], sp[2], r[5]); paintAny(out, sp[1] + 1, sp[2], r[3]); paintAny(out, sp[1] - 1, sp[2], r[3])
  paintAny(out, sp[1], sp[2] + 1, r[3]); paintAny(out, sp[1], sp[2] - 1, r[3])
  return finish(out, r)
end

-- ================= запасные (если у врага нет своих пуль) =================
local function orbWhite(r, f)
  local out = body(discMask(8.5, 8.5, 6), r, nil, 1)
  local a = math.rad(f * 22.5)
  for i = 0, 3 do
    local ang = a + i * math.pi / 2
    paint(out, math.floor(8.5 + math.cos(ang) * 3.4), math.floor(8.5 + math.sin(ang) * 3.4), r[2])
  end
  return finish(out, r)
end
local function orbPurple(r, f)
  local out = grid()
  local gap = (f == 1 or f == 2) and 1 or 0
  for side = -1, 1, 2 do
    local m = grid()
    L.each(discMask(8.5, 8.5, 6), function(x, y)
      if (side < 0 and x <= 7) or (side > 0 and x >= 9) then L.set(m, x + side * gap, y, "x") end
    end)
    body(m, r, out, 1)
  end
  return finish(out, r)
end
local function orbHalf(r, f)
  local m = grid()
  L.each(discMask(8.5, 8.5, 6), function(x, y) if x <= 7 then L.set(m, x + 3, y, "x") end end)
  return finish(body(m, r, nil, 1), r)
end
local function crystalBlue(r, f)
  local m = grid()
  for y = 0, N - 1 do for x = 0, N - 1 do
    if math.abs(x + 0.5 - 8.5) + math.abs(y + 0.5 - 8.5) <= 6.5 then L.set(m, x, y, "x") end
  end end
  local out = body(m, r, nil, 1)
  for y = 3, 14 do paint(out, 8, y, r[2]) end
  local glint = ({ { 6, 6 }, { 7, 5 }, { 5, 7 }, { 6, 7 } })[f + 1]
  paint(out, glint[1], glint[2], r[5])
  return finish(out, r)
end

-- ================= сохранение =================
local function save(id, draw, r)
  local frames = { draw(r, 0), draw(r, 1), draw(r, 2), draw(r, 3) }
  L.save(id, { frames[1] })
  L.save(id .. "_spin", frames, { 0.14 })
  return frames
end

save("Projectiles/BulletWhite", orbWhite, RAMPS.White)
save("Projectiles/BulletPurple", orbPurple, RAMPS.Purple)
save("Projectiles/BulletPurpleHalf", orbHalf, RAMPS.Purple)
save("Projectiles/BulletBlue", crystalBlue, RAMPS.Blue)

-- вспышка появления: белый квадрат со срезанными углами; трещина — чёрный зигзаг (для запасных осколков)
local spawn = grid()
L.rect(spawn, 4, 4, 12, 12, "W")
for _, p in ipairs({ { 4, 4 }, { 12, 4 }, { 4, 12 }, { 12, 12 } }) do L.set(spawn, p[1], p[2], nil) end
L.save("Projectiles/Bullet_spawn", { L.outline(spawn, 1, "K") })
local crack = grid()
for _, p in ipairs({ { 6, 4 }, { 7, 5 }, { 8, 6 }, { 8, 7 }, { 7, 8 }, { 7, 9 }, { 8, 10 }, { 9, 11 }, { 9, 8 }, { 10, 9 }, { 11, 10 } }) do L.set(crack, p[1], p[2], "K") end
L.save("Projectiles/BulletCrack", { crack })

local ENEMIES = {
  { "Ghost", { White = ghostWhite, Purple = ghostPurple, PurpleHalf = ghostHalf, Blue = ghostBlue } },
  { "Eye", { White = eyeWhite, Purple = eyePurple, PurpleHalf = eyeHalf, Blue = eyeBlue } },
  { "Skull", { White = skullWhite, Purple = skullPurple, PurpleHalf = skullHalf, Blue = skullBlue } },
  { "Moonface", { White = moonWhite, Purple = moonPurple, PurpleHalf = moonHalf, Blue = moonBlue } },
}
local TYPES = { "White", "Purple", "PurpleHalf", "Blue" }
local RAMP_OF = { White = RAMPS.White, Purple = RAMPS.Purple, PurpleHalf = RAMPS.Purple, Blue = RAMPS.Blue }

-- превью: строки — враги, блоки — белый / фиолетовый / осколок фиолетового / синий, по 4 кадра; фон как у рамки боя
local SC = 6
local cell = N + 2
local sheet = Image((#TYPES * 4 * cell + (#TYPES - 1) * 6) * SC, #ENEMIES * (cell + 2) * SC, ColorMode.RGB)
sheet:clear(app.pixelColor.rgba(6, 3, 8, 255))
for row, e in ipairs(ENEMIES) do
  for ti, t in ipairs(TYPES) do
    local frames = save("Projectiles/" .. e[1] .. "/Bullet" .. t, e[2][t], RAMP_OF[t])
    for fi, g in ipairs(frames) do
      local img = L.toImage(g)
      for y = 0, N - 1 do for x = 0, N - 1 do
        local c = img:getPixel(x, y)
        if app.pixelColor.rgbaA(c) > 0 then
          local px_ = ((ti - 1) * (4 * cell + 6) + (fi - 1) * cell + x) * SC
          local py = ((row - 1) * (cell + 2) + 1 + y) * SC
          for dy = 0, SC - 1 do for dx = 0, SC - 1 do sheet:drawPixel(px_ + dx, py + dy, c) end end
        end
      end end
    end
  end
end
sheet:saveAs(OUT .. "preview_enemy_bullets.png")
print("ok")
