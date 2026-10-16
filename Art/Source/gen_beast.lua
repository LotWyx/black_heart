-- Враг «Ненасытный» (Beast) — чёрный лохматый зверь-кот, сидит на передних лапах
-- с когтями, хвост с кисточкой; высокие уши (правое рваное), два больших горящих глаза и три малых на лбу, оскал, усы.
-- Стиль как у gen_enemies.lua: тёмный силуэт с красной контровой каймой, дизеринг, чёрный контур + белая обводка.
-- Кадры (S.animations): base, idle (4), windup, hurt, laugh (3), defeat — 80x80.
-- Кадры — Assets/Resources/Art/Enemies/Beast*.png, исходники — Art/Source, превью — Art/Preview/Enemies/.
-- Запуск: aseprite -b --script Art/Source/gen_beast.lua
local ROOT = "/home/lotwy/Projects/black_heart/"
local OUT = ROOT .. "Art/Preview/Enemies/"   -- превью; кадры — в Assets/Resources/Art, исходники .aseprite — в Art/Source

-- Библиотеки генераторов ссылаются на старый путь проекта (~/black_heart): подменяем его при загрузке
local OLD = "/home/lotwy/black_heart/"
local realDofile = dofile
dofile = function(path)
  path = path:gsub(OLD, ROOT)
  local f = assert(io.open(path, "r"))
  local src = f:read("a"):gsub(OLD, ROOT)
  f:close()
  return assert(load(src, "@" .. path))()
end

local V = dofile(ROOT .. "Art/Source/bh_silhouette.lua")
dofile = realDofile
local S, L, W, H = V.S, V.L, V.W, V.H
local render, mask, part = V.render, V.mask, V.part
local paint, paintLine, mirPts, finish = V.paint, V.paintLine, V.mirPts, V.finish

-- сохранение кадров — в превью, не в Assets
L.ART = ROOT .. "Assets/Resources/Art/"
L.SRC = ROOT .. "Art/Source/"
os.execute('mkdir -p "' .. OUT .. '"')

local function shifted(pts, dy)
  local r = {}
  for i, p in ipairs(pts) do r[i] = { p[1], p[2] + dy } end
  return r
end

-- st: bob — подъём, phase — колыхание меха, mouth — насколько раскрыт оскал (0..2), ear — подёргивание ушей
local function beast(st)
  local parts = {}
  local by = st.bob or 0
  local ph = st.phase or 0
  local mo = st.mouth or 0
  local ear = st.ear or 0

  -- ===== хвост: из-за спины вбок и вверх, пушистый кончик; покачивается =====
  local tsw = math.sin(ph * 1.2) * 2
  local tail = mask()
  S.taper(tail, { 52, 62 + by }, { 66, 63 + by }, { 70 + tsw * 0.5, 50 + by }, 3.4, 2.6)
  S.taper(tail, { 70 + tsw * 0.5, 50 + by }, { 73 + tsw, 40 + by }, { 68 + tsw, 33 + by }, 2.6, 1.6)
  for i, o in ipairs({ { -3, -3 }, { 1, -4 }, { 4, -1 }, { -4, 1 } }) do   -- кисточка из игл
    local tx, ty = 68 + tsw, 33 + by
    S.taper(tail, { tx, ty }, { tx + o[1] * 0.6, ty + o[2] * 0.6 }, { tx + o[1] * 1.3, ty + o[2] * 1.3 }, 1.6, 0.4)
  end
  part(parts, "tail", "cloth", 0, tail)

  -- ===== туловище: мощная грудь и плечи сидящего зверя, мех торчит короткими иглами =====
  local body = mask()
  L.ellipse(body, 40, 53 + by, 17, 12, "x")
  L.ellipse(body, 27, 49 + by, 7.5, 7, "x")
  L.ellipse(body, 53, 49 + by, 7.5, 7, "x")
  for i, a in ipairs({ 196, 212, 228, 312, 328, 344, 160, 20 }) do
    local r = math.rad(a)
    local dx, dy = math.cos(r), -math.sin(r)
    local ex, ey = 40 + dx * 18, 52 + dy * 12 + by
    local sw = math.sin(ph * 1.3 + i) * 0.8
    S.taper(body, { ex - dx * 2, ey - dy * 2 }, { ex + dx * 2.5, ey + dy * 2.5 + sw }, { ex + dx * 5, ey + dy * 5 + sw }, 2.4, 0.4)
  end
  for _, sx in ipairs({ 36, 40, 44 }) do   -- лохмы между лапами
    S.taper(body, { sx, 62 + by }, { sx + 0.5, 65 + by }, { sx + 1, 68 + by }, 2.2, 0.4)
  end
  part(parts, "body", "cloth", 1, body)

  -- ===== передние лапы с когтями =====
  local legs = mask()
  for side = -1, 1, 2 do
    local function X(x) return side == -1 and x or W - x end
    S.taper(legs, { X(32), 57 + by }, { X(31), 66 + by }, { X(30), 72 + by }, 4.4, 3.6)
    L.ellipse(legs, X(29), 73.5 + by, 4.8, 2.6, "x")
  end
  part(parts, "leg", "cloth", 4, legs)
  -- ===== уши: левое целое, правое с вырванным клоком; в смехе подёргиваются =====
  local ears = mask()
  L.poly(ears, shifted({ {26,24}, {20 - ear, 1}, {36,16} }, by), "x")
  L.poly(ears, shifted({ {54,24}, {60 + ear, 2}, {57 + ear, 8}, {60, 10}, {44,16} }, by), "x")
  part(parts, "ear", "cloth", 2, ears)

  -- ===== голова: широкая морда, щёки-бакенбарды клочьями, вихры на макушке =====
  local head = mask()
  L.ellipse(head, 40, 28 + by, 16, 12.5, "x")
  for side = -1, 1, 2 do
    local function X(x) return side == -1 and x or W - x end
    S.taper(head, { X(28), 29 + by }, { X(20), 32 + by }, { X(14), 34 + by }, 3.6, 0.6)
    S.taper(head, { X(28), 35 + by }, { X(21), 40 + by }, { X(17), 45 + by }, 3.4, 0.6)
    S.taper(head, { X(33), 38 + by }, { X(30), 43 + by }, { X(29), 47 + by }, 2.6, 0.5)
  end
  S.taper(head, { 37, 18 + by }, { 35, 14 + by }, { 33, 12 + by }, 2.2, 0.5)
  S.taper(head, { 43, 18 + by }, { 46, 15 + by }, { 47, 12 + by }, 2.2, 0.5)
  part(parts, "head", "cloth", 3, head)

  local out, owner = render(parts)

  -- тень от головы на груди: голова отделяется от тела
  L.each(body, function(x, y)
    if y <= 45 + by then paint(out, owner, { body = true }, x, y, (y <= 43 + by or (x + y) % 2 == 0) and "dk0" or "dk1") end
  end)

  -- шерсть на груди и лапах: штрихи-пряди
  for _, f in ipairs({ { {26,47}, {24,52} }, { {54,47}, {56,52} }, { {31,46}, {30,50} }, { {49,46}, {50,50} },
                       { {29,60}, {28,64} }, { {51,60}, {52,64} } }) do
    paintLine(out, owner, { body = true }, shifted(f, by), "dk0")
  end
  for side = -1, 1, 2 do
    local function X(x) return side == -1 and x or W - 1 - x end
    paintLine(out, owner, { leg = true }, shifted({ { X(33), 60 }, { X(32), 68 } }, by), "dk0")
    -- пальцы и когти
    for _, o in ipairs({ -2, 0, 2 }) do
      local cx = X(29) + o
      if o ~= 2 then paint(out, owner, { leg = true }, cx + 1, 74 + by, "dk0") end
      L.set(out, cx, 75 + by, "bn5")
      L.set(out, cx, 76 + by, "bn3")
    end
  end

  -- внутренность ушей — тёмная плоть
  local inner = mask()
  L.poly(inner, shifted({ {27,20}, {22 - ear, 5}, {33,16} }, by), "x")
  L.poly(inner, shifted({ {53,20}, {58 + ear, 5}, {56, 11}, {47,16} }, by), "x")
  L.each(inner, function(x, y) paint(out, owner, { ear = true }, x, y, (y % 2 == 0) and "fl1" or "fl0") end)

  -- шерсть: штрихи на морде, красные царапины на груди (как на референсе)
  for _, f in ipairs({ { {31,20}, {28,27} }, { {49,20}, {52,27} }, { {31,38}, {27,44} }, { {49,38}, {53,44} } }) do
    paintLine(out, owner, { head = true }, shifted(f, by), "dk0")
  end
  for _, f in ipairs({ { {28,52}, {31,57} }, { {31,50}, {34,56} }, { {34,51}, {36,55} }, { {49,51}, {46,57} }, { {52,49}, {49,55} } }) do
    paintLine(out, owner, { body = true }, shifted(f, by), "rim2")
  end

  -- ===== глаза: два больших горящих и три малых на лбу =====
  local eyes = {}
  local function eye(cx, cy, r)
    local m = mask()
    L.ellipse(m, cx, cy, r, r * 0.9, "x")
    L.each(m, function(x, y)
      local d = math.sqrt((x + 0.5 - cx) ^ 2 + (y + 0.5 - cy) ^ 2) / r
      local c = d > 0.8 and "ir2" or d > 0.45 and "gl1" or "gl3"
      L.set(out, x, y, c)
      eyes[#eyes + 1] = { x, y }
    end)
  end
  eye(34, 27 + by, 3.4)
  eye(46, 27 + by, 3.4)
  eye(36, 20 + by, 1.6)
  eye(40, 18 + by, 1.8)
  eye(44, 20 + by, 1.6)
  -- злой изгиб над большими глазами
  paintLine(out, owner, { head = true }, shifted({ {29,22}, {33,23}, {37,24} }, by), "K")
  paintLine(out, owner, { head = true }, mirPts(shifted({ {29,22}, {33,23}, {37,24} }, by)), "K")

  -- усы: светлые штрихи на щеках
  for _, w in ipairs({ { {27,29}, {21,27} }, { {27,31}, {20,31} } }) do   -- выше пасти, чтобы не сливались с зубами
    paintLine(out, owner, { head = true }, shifted(w, by), "sc1")
    paintLine(out, owner, { head = true }, mirPts(shifted(w, by)), "sc1")
  end

  -- нос
  for _, p in ipairs({ {39,32}, {40,32}, {41,32}, {40,33} }) do L.set(out, p[1], p[2] + by, "K") end

  -- ===== оскал: от уха до уха, зубы-пилы, клыки; в смехе пасть раскрывается, виден язык =====
  local function top(x) return 34 - ((x - 40) / 13) ^ 2 * 5 end
  local function bottom(x) return 39 + mo * 3 - ((x - 40) / 13) ^ 2 * (6 + mo * 3) end
  for x = 27, 53 do
    local y0 = math.floor(top(x) + 0.5) + by
    local y1 = math.max(y0, math.floor(bottom(x) + 0.5) + by)
    for y = y0, y1 do L.set(out, x, y, "K") end
    -- зубы-пилы по 3 px: верхние остриём вниз, нижние — вверх (вперемешку, как у оскала на референсе)
    local k = x % 3
    L.set(out, x, y0, k == 1 and "bn5" or "bn4")
    if k == 1 and y1 > y0 + 1 then L.set(out, x, y0 + 1, "bn4") end
    if k == 1 and y1 > y0 + 3 then L.set(out, x, y0 + 2, "bn3") end
    if y1 > y0 + 2 then
      L.set(out, x, y1, k == 2 and "bn4" or "bn3")
      if k == 2 and y1 - 1 > y0 + 2 then L.set(out, x, y1 - 1, "bn3") end
    end
  end
  -- клыки
  for _, cx in ipairs({ 32, 48 }) do
    local y0 = math.floor(top(cx) + 0.5) + by
    L.set(out, cx, y0, "bn5"); L.set(out, cx, y0 + 1, "bn5"); L.set(out, cx, y0 + 2, "bn4")
    if mo == 0 then L.set(out, cx, y0 + 3, "bn3") end
  end
  -- язык в раскрытой пасти
  if mo > 0 then
    local ty = math.floor(bottom(40) + 0.5) + by - 1
    local tongue = mask()
    L.ellipse(tongue, 40, ty, 4 + mo, 1.6 + mo * 0.5, "x")
    L.each(tongue, function(x, y)
      if L.get(out, x, y) == "K" then L.set(out, x, y, (y <= ty - 1) and "rim3" or "vn") end
    end)
    L.set(out, 40, ty - 1, "fl3")
  end

  return finish(out, eyes)
end

local ANIM = {
  idle = { { phase = 0 }, { phase = 1, bob = -1 }, { phase = 2, bob = -1 }, { phase = 3 } },
  laugh = { { mouth = 1, ear = 1 }, { mouth = 2, ear = 2, bob = -2, phase = 1 }, { mouth = 1, ear = 1, phase = 2 } },
}
S.animations("Beast", beast, { idleFrames = ANIM.idle, laugh = ANIM.laugh, laughShift = false })

local idle, laugh = {}, {}
for i, st in ipairs(ANIM.idle) do idle[i] = (beast(st)) end
for i, st in ipairs(ANIM.laugh) do laugh[i] = (beast(st)) end
V.preview({ idle, laugh }, OUT .. "preview_beast.png")

-- Сравнение с текущими врагами на фоне зала (x3): Безликий, Всевидящий, Пустоглазый, новый
local SC = 3
local hall = Image{ fromFile = ROOT .. "Assets/Resources/Art/Background/Hall.png" }
local list = {
  Image{ fromFile = ROOT .. "Assets/Resources/Art/Enemies/Ghost.png" },
  Image{ fromFile = ROOT .. "Assets/Resources/Art/Enemies/Eye.png" },
  Image{ fromFile = ROOT .. "Assets/Resources/Art/Enemies/Skull.png" },
  L.toImage((beast({}))),
}
local sheet = Image(#list * (W * SC + 4) - 4, H * SC, ColorMode.RGB)
for i, spr in ipairs(list) do
  local img = Image(W, H, ColorMode.RGB)
  img:drawImage(hall, Point(-120, 0))
  img:drawImage(spr, Point(0, 0))
  for y = 0, H - 1 do for x = 0, W - 1 do
    local c = img:getPixel(x, y)
    for dy = 0, SC - 1 do for dx = 0, SC - 1 do sheet:drawPixel((i - 1) * (W * SC + 4) + x * SC + dx, y * SC + dy, c) end end
  end end
end
sheet:saveAs(OUT .. "compare_enemies_beast.png")
print("ok")
