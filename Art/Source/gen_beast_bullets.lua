-- Пули «Ненасытного» (Projectiles/Beast). Кадры 17x17, по 4, как в gen_projectiles_themed.lua:
--   белая — ухмыляющаяся кошачья голова (оскал раскрывается, уши подёргиваются);
--   фиолетовая — две маленькие морды рядом, при ударе разлетаются по одной (BulletPurpleHalf);
--   синяя («не рубить») — кошачья лапа-отпечаток, по подушечкам бежит блик.
-- Объём, контур и ореол — те же правила, что у остальных пуль. Кадры — Assets/Resources/Art/Projectiles/Beast/.
local ROOT = "/home/lotwy/Projects/black_heart/"
local OUT = ROOT .. "Art/Preview/Enemies/"   -- превью; кадры — в Assets/Resources/Art, исходники .aseprite — в Art/Source

local f = assert(io.open(ROOT .. "Art/Source/bh_lib.lua", "r"))
local L = assert(load(f:read("a"), "@bh_lib.lua"))()
f:close()
L.ART = ROOT .. "Assets/Resources/Art/"
L.SRC = ROOT .. "Art/Source/"

local N = 17
local function hex(h) return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) } end
for k, v in pairs({
  w1 = "#3c3c58", w2 = "#7a7a98", w3 = "#b8b8d0", w4 = "#e8e8f4", w5 = "#ffffff", wg = "#2a2a40",
  p1 = "#2a0c48", p2 = "#5a1e92", p3 = "#8c3ccc", p4 = "#b876f0", p5 = "#ecd2ff", pg = "#26083c",
  b1 = "#0a2058", b2 = "#1650b0", b3 = "#2e8ef0", b4 = "#7cc8ff", b5 = "#dcf4ff", bg = "#081a42",
}) do L.COLORS[k] = hex(v) end
local RAMPS = {
  White = { "w1", "w2", "w3", "w4", "w5", g = "wg" },
  Purple = { "p1", "p2", "p3", "p4", "p5", g = "pg" },
  Blue = { "b1", "b2", "b3", "b4", "b5", g = "bg" },
}

-- ---------- те же помощники, что в gen_projectiles_themed.lua ----------
local BAYER = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 }
local function dither(x, y, d) return (BAYER[(y % 4) * 4 + (x % 4) + 1] + 0.5) / 16 < d end
local function grid() return L.grid(N, N) end
local function inside(x, y) return x >= 0 and y >= 0 and x < N and y < N end
local function inDisc(x, y, cx, cy, r) local dx, dy = x + 0.5 - cx, y + 0.5 - cy; return dx * dx + dy * dy <= r * r end
local function disc(m, cx, cy, r) for y = 0, N - 1 do for x = 0, N - 1 do if inDisc(x, y, cx, cy, r) then L.set(m, x, y, "x") end end end end
local function rows(m, list, ox, oy)
  for y, row in ipairs(list) do for x = 1, #row do
    if row:sub(x, x) ~= "." and inside(ox + x - 1, oy + y - 1) then L.set(m, ox + x - 1, oy + y - 1, "x") end
  end end
end
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

-- ================= белая: ухмыляющаяся кошачья голова =================
-- Нарисована по пикселям: # — шерсть (объём считает body), e — глаз, g — блик в глазу, n — нос,
-- m — тёмная пасть, t — зуб. Два вида: пасть сомкнута и раскрыта (кадры чередуются)
local CAT_CLOSED = {
  "#...........#",
  "##.........##",
  "###.......###",
  "####.###.####",
  "#############",
  "#eeg#####gee#",
  "##ee#####ee##",
  "#m####n####m#",
  "#mtmtmtmtmtm#",
  ".mmmmmmmmmmm.",
  "..#########..",
  "...#######...",
}
local CAT_OPEN = {
  "#...........#",
  "##.........##",
  "###.......###",
  "####.###.####",
  "#############",
  "#eeg#####gee#",
  "##ee#####ee##",
  "#m####n####m#",
  "#mtmtmtmtmtm#",
  ".mmmmmmmmmmm.",
  "..mtmtmtmtm..",
  "...#######...",
}
local FACE = { e = "K", n = "K", m = "K" }
local function beastWhite(r, fr)
  local art = (fr % 2 == 1) and CAT_OPEN or CAT_CLOSED
  local ox, oy = 2, 3
  local m = grid()
  rows(m, art, ox, oy)
  if fr == 2 then L.set(m, ox + 12, oy, nil) end   -- правое ухо дёргается
  local out = body(m, r)
  paint(out, ox + 1, oy + 1, r[2]); paint(out, ox + 11, oy + 1, r[2])   -- глубина ушей
  for y, row in ipairs(art) do for x = 1, #row do
    local ch = row:sub(x, x)
    local c = FACE[ch] or (ch == "g" and r[5]) or (ch == "t" and r[5])
    if c then paint(out, ox + x - 1, oy + y - 1, c) end
  end end
  return finish(out, r)
end

-- ================= фиолетовая: две маленькие морды; половинка — одна =================
local MINI = {
  "#.....#",
  "##...##",
  "#######",
  "#######",
  "#######",
  ".#####.",
  "..###..",
}
local function miniCat(out, r, ox, oy, open)
  local m = grid()
  rows(m, MINI, ox, oy)
  body(m, r, out)
  paint(out, ox + 1, oy + 3, "K"); paint(out, ox + 5, oy + 3, "K")     -- глаза
  paint(out, ox + 1, oy + 2, r[5])
  paint(out, ox + 2, oy + 5, "K"); paint(out, ox + 3, oy + 5, open and "K" or r[5]); paint(out, ox + 4, oy + 5, "K")  -- оскал
  if open then paint(out, ox + 3, oy + 6, "K") end
end
local function beastPurple(r, fr)
  local out = grid()
  local bob = (fr == 1 or fr == 2) and 1 or 0
  miniCat(out, r, 1, 4 + bob, fr == 1)
  miniCat(out, r, 9, 5 - bob, fr == 3)
  return finish(out, r)
end
local function beastHalf(r, fr)
  local out = grid()
  miniCat(out, r, 5, 4 + ((fr == 1 or fr == 2) and 1 or 0), fr % 2 == 1)
  return finish(out, r)
end

-- ================= синяя: кошачья лапа-отпечаток =================
local function beastBlue(r, fr)
  local m = grid()
  disc(m, 8.5, 12.2, 3.3)                       -- подушечка из трёх долей
  disc(m, 6.4, 13.2, 2.2); disc(m, 10.6, 13.2, 2.2)
  local wig = ({ 0, 1, 0, -1 })[fr + 1]           -- пальцы по очереди поджимаются
  local toes = { { 3.4, 7.6 }, { 6.6, 4.4 }, { 10.4, 4.4 }, { 13.6, 7.6 } }
  for i, t in ipairs(toes) do
    disc(m, t[1], t[2] + ((i % 2 == 0) and wig * 0.5 or -wig * 0.5), 1.45)
  end
  local out = body(m, r, nil, 1)
  local glint = toes[fr + 1]                     -- блик перебегает с пальца на палец
  paint(out, math.floor(glint[1]), math.floor(glint[2] - 0.5), r[5])
  paint(out, 7, 11, r[5])
  return finish(out, r)
end

-- ================= клыки (особый вид пуль у «Пасти» и «Когтей»: Projectiles/Beast/Fang<Тип>) =================
-- Клык нарисован остриём вверх: игра поворачивает его по ходу полёта (IOrientedMotion). По клыку бежит блик.
local FANG = {
  "....#..",
  "....#..",
  "...##..",
  "...##..",
  "..###..",
  "..####.",
  ".#####.",
  ".#####.",
  "######.",
  "#######",
  "#######",
  ".#####.",
  "..###..",
}
local MINI_FANG = {
  "...#.",
  "...#.",
  "..##.",
  "..##.",
  ".###.",
  ".####",
  "#####",
  "#####",
  "#####",
  ".###.",
  "..#..",
}
local function fangGlint(out, r, ox, oy, h, fr)
  local gy = oy + math.floor((h - 3) * (fr / 3)) + 1     -- блик скользит от острия к корню
  for y = oy, oy + h - 1 do
    for x = ox, ox + 6 do
      if y == gy and L.get(out, x, y) then L.set(out, x, y, r[5]); break end
    end
  end
end
local function beastFangWhite(r, fr)
  local m = grid()
  rows(m, FANG, 5, 2)
  local out = body(m, r)
  paint(out, 8, 9, r[2]); paint(out, 8, 10, r[2]); paint(out, 7, 11, r[2])   -- ложбинка вдоль клыка
  fangGlint(out, r, 5, 2, #FANG, fr)
  return finish(out, r)
end
local function miniFang(out, r, ox, oy, fr)
  local m = grid()
  rows(m, MINI_FANG, ox, oy)
  body(m, r, out)
  fangGlint(out, r, ox, oy, #MINI_FANG, fr)
end
local function beastFangPurple(r, fr)
  local out = grid()
  local bob = (fr == 1 or fr == 2) and 1 or 0
  miniFang(out, r, 2, 3 + bob, fr)
  miniFang(out, r, 9, 3 - bob + 1, (fr + 2) % 4)
  return finish(out, r)
end
local function beastFangHalf(r, fr)
  local out = grid()
  miniFang(out, r, 6, 3 + ((fr == 1 or fr == 2) and 1 or 0), fr)
  return finish(out, r)
end
local FANGS = { White = beastFangWhite, Purple = beastFangPurple, PurpleHalf = beastFangHalf, Blue = beastFangWhite }

-- ================= сохранение и превью =================
local function save(id, draw, rr)
  local frames = { draw(rr, 0), draw(rr, 1), draw(rr, 2), draw(rr, 3) }
  L.save(id, { frames[1] })
  L.save(id .. "_spin", frames, { 0.14 })
  return frames
end
local SET = { White = beastWhite, Purple = beastPurple, PurpleHalf = beastHalf, Blue = beastBlue }
local TYPES = { "White", "Purple", "PurpleHalf", "Blue" }
local RAMP_OF = { White = RAMPS.White, Purple = RAMPS.Purple, PurpleHalf = RAMPS.Purple, Blue = RAMPS.Blue }
os.execute('mkdir -p "' .. OUT .. '"')
for _, t in ipairs(TYPES) do
  save("Projectiles/Beast/Bullet" .. t, SET[t], RAMP_OF[t])
  save("Projectiles/Beast/Fang" .. t, FANGS[t], RAMP_OF[t])
end

-- Сравнение: строки — Безликий, Всевидящий, Пустоглазый, новый; столбцы — белая, фиолетовая, половинка, синяя (4 кадра)
local SC, cell = 6, N + 2
local rowsList = {
  ROOT .. "Assets/Resources/Art/Projectiles/Ghost/",
  ROOT .. "Assets/Resources/Art/Projectiles/Eye/",
  ROOT .. "Assets/Resources/Art/Projectiles/Skull/",
  ROOT .. "Assets/Resources/Art/Projectiles/Beast/",
  ROOT .. "Assets/Resources/Art/Projectiles/Beast/Fang",
}
local sheet = Image((#TYPES * 4 * cell + (#TYPES - 1) * 6) * SC, #rowsList * (cell + 2) * SC, ColorMode.RGB)
sheet:clear(app.pixelColor.rgba(6, 3, 8, 255))
for row, dir in ipairs(rowsList) do
  for ti, t in ipairs(TYPES) do
    local prefix = dir:sub(-1) == "/" and (dir .. "Bullet") or dir   -- …/Beast/Fang — клыки
    local strip = Image{ fromFile = prefix .. t .. "_spin.png" }
    for fi = 0, 3 do
      for y = 0, N - 1 do for x = 0, N - 1 do
        local c = strip:getPixel(fi * N + x, y)
        if app.pixelColor.rgbaA(c) > 0 then
          local px_ = ((ti - 1) * (4 * cell + 6) + fi * cell + x) * SC
          local py = ((row - 1) * (cell + 2) + 1 + y) * SC
          for dy = 0, SC - 1 do for dx = 0, SC - 1 do sheet:drawPixel(px_ + dx, py + dy, c) end end
        end
      end end
    end
  end
end
sheet:saveAs(OUT .. "compare_bullets_beast.png")
print("ok")
