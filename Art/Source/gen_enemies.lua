-- Враги: Призрак, Глаз, Череп. 80x80.
-- Стиль как у фонов: тёмные силуэты с красной контровой каймой (враг висит перед витражом),
-- яркие только главные детали (кость, глаз, свечение); переходы тонов — упорядоченным дизерингом,
-- без «купольного» объёма. Кадры: base, idle (4), laugh (3); замах, урон и поражение строит S.animations.
-- Вывод — Assets/Resources/Art/Enemies/, превью на фоне зала — Art/Preview/Enemies/.
local V = dofile("/home/lotwy/black_heart/Art/Source/bh_silhouette.lua")
local S, L, W, H = V.S, V.L, V.W, V.H
local OUT = "/home/lotwy/black_heart/Art/Preview/Enemies/"   -- только превью
local MAT, dither, render, mask, part = V.MAT, V.dither, V.render, V.mask, V.part
local paint, paintLine, mirX, mirPts, finish = V.paint, V.paintLine, V.mirX, V.mirPts, V.finish

-- ================= ПРИЗРАК: рваный балахон, пустота под капюшоном, костлявые руки, амулет =================
local function ghost(st)
  local parts = {}
  local by = st.bob or 0
  local ph = st.phase or 0
  local up = st.armsUp or 0

  local robe = mask()
  L.poly(robe, { {26,25 + by}, {54,25 + by}, {58,34 + by}, {55,45 + by}, {49,54 + by}, {31,54 + by}, {25,45 + by}, {22,34 + by} }, "x")
  -- рваные хвосты вместо ног, колышутся по фазе
  for i, t in ipairs({ { 33, -5, 70 }, { 40, 4, 76 }, { 47, 5, 69 }, { 29, -7, 62 }, { 51, 7, 62 } }) do
    local sway = math.sin((ph + i) * 1.6) * 2
    S.taper(robe, { t[1], 50 + by }, { t[1] + t[2] + sway, (50 + t[3]) / 2 + by }, { t[1] + t[2] * 0.4 - sway, t[3] + by }, 4.2, 0.6)
  end
  part(parts, "robe", "cloth", 1, robe)

  local hood = mask()
  L.poly(hood, { {25,27 + by}, {27,15 + by}, {33,8 + by}, {40,3 + by}, {46,4 + by}, {51,9 + by}, {54,17 + by}, {56,28 + by}, {47,31 + by}, {33,31 + by} }, "x")
  part(parts, "hood", "cloth", 3, hood)

  -- рукава и костлявые кисти: в покое тянутся вниз-в стороны, в смехе взлетают к голове
  local hand = { { 12, 47 - up * 16 + by }, { mirX(12), 47 - up * 16 + by } }
  local sleeves = mask()
  S.taper(sleeves, { 24, 32 + by }, { 17, 38 + by - up * 6 }, { hand[1][1] + 3, hand[1][2] - 3 }, 4.5, 5.5)
  S.taper(sleeves, { 56, 32 + by }, { 63, 38 + by - up * 6 }, { hand[2][1] - 3, hand[2][2] - 3 }, 4.5, 5.5)
  part(parts, "sleeve", "cloth", 4, sleeves)
  local hands = mask()
  for i, hp in ipairs(hand) do
    local sx = i == 1 and -1 or 1
    L.ellipse(hands, hp[1], hp[2], 2.6, 2.2, "x")
    for f = -1, 2 do   -- длинные пальцы-когти
      local fx = hp[1] + sx * (f * 1.6)
      local tipY = hp[2] + 6 - math.abs(f) + (up > 0 and -10 or 0)
      L.line(hands, math.floor(fx + 0.5), hp[2] + (up > 0 and -1 or 1), math.floor(fx + sx * (f == 2 and 2 or 1) + 0.5), tipY, "x")
    end
  end
  part(parts, "hand", "bone", 5, hands, { noBorder = true })

  local out, owner = render(parts)

  -- складки балахона
  for _, f in ipairs({ { {32,34}, {30,46}, {29,56} }, { {40,34}, {40,48}, {41,57} }, { {48,34}, {50,46}, {51,56} }, { {24,44}, {21,56} }, { {56,44}, {59,56} } }) do
    local pts = {}
    for i, p in ipairs(f) do pts[i] = { p[1], p[2] + by } end
    paintLine(out, owner, { robe = true }, pts, "dk0")
  end
  paintLine(out, owner, { hood = true }, { {36,10 + by}, {31,20 + by}, {29,28 + by} }, "dk1")

  -- пустота под капюшоном: глубокая тьма, слабое красное свечение изнутри
  local void = mask()
  L.ellipse(void, 40, 21 + by, 8.5, 9, "x")
  L.each(void, function(x, y)
    local d = ((x + 0.5 - 40) / 8.5) ^ 2 + ((y + 0.5 - 22 - by) / 9) ^ 2
    paint(out, owner, { hood = true }, x, y, d > 0.8 and "dk1" or "K")
  end)
  -- глаза: горящие щели (в смехе — дуги)
  local eyes = {}
  local ey = 20 + by
  for _, ex in ipairs({ 34, 43 }) do
    if (st.mouth or 0) > 0 then
      for i, o in ipairs({ { 0, 1 }, { 1, 0 }, { 2, 0 }, { 3, 1 } }) do
        L.set(out, ex + o[1], ey + o[2], (i == 2 or i == 3) and "gl3" or "gl2"); eyes[#eyes + 1] = { ex + o[1], ey + o[2] }
      end
    else
      -- злые щели, скошенные к переносице, с ореолом
      local inner = ex < 40 and 1 or -1
      local px = ex < 40 and { {0,0}, {1,0}, {2,1}, {3,1} } or { {0,1}, {1,1}, {2,0}, {3,0} }
      for _, o in ipairs(px) do
        L.set(out, ex + o[1], ey + o[2] - 1, "rim1")
        L.set(out, ex + o[1], ey + o[2] + 1, "rim2")
      end
      for i, o in ipairs(px) do
        local c = (i == 2 or i == 3) and "gl3" or "gl2"
        L.set(out, ex + o[1], ey + o[2], c); eyes[#eyes + 1] = { ex + o[1], ey + o[2] }
      end
    end
  end
  if (st.mouth or 0) > 0 then   -- зубастая ухмылка из пустоты
    for x = 35, 45 do
      L.set(out, x, ey + 6 + ((x % 2 == 0) and 0 or 1), "gl1")
    end
  end
  -- амулет-сердце на груди на цепочке
  paintLine(out, owner, { robe = true }, { {33,26 + by}, {40,33 + by}, {47,26 + by} }, "dk3")
  local gem = { "..r.r..", ".rRRRr.", ".rRhRr.", "..rRr..", "...r..." }
  for gy, row in ipairs(gem) do
    for gx = 1, #row do
      local ch = row:sub(gx, gx)
      if ch ~= "." then paint(out, owner, { robe = true }, 36 + gx, 31 + gy + by, ch == "r" and "gem1" or ch == "R" and "gem2" or "gem3") end
    end
  end
  return finish(out, eyes)
end

-- ================= ГЛАЗ: огромный глаз в мясистых веках, перепончатые крылья, свисающие нервы =================
local function eye(st)
  local parts = {}
  local flap = st.flap or 0
  local by = st.bob or 0
  local look = st.look or 0
  local lid = st.lid or 0   -- 0 открыт, 1 прищур, 2 почти закрыт

  -- крылья: кости-спицы и перепонка с фестонами по краю
  local wings = mask()
  local bones = {}
  for side = -1, 1, 2 do
    local function X(x) return side == -1 and x or W - x end
    local root = { X(27), 30 + by }
    local tips = { { X(5), 10 + by - flap * 3 }, { X(2), 26 + by - flap * 2 }, { X(8), 40 + by - flap } }
    local poly = { root, { X(20), 14 + by - flap * 2 }, tips[1] }
    -- нижний край — дуги между концами спиц
    local function scallop(a, b)
      for t = 1, 3 do
        local u = t / 4
        poly[#poly + 1] = { a[1] + (b[1] - a[1]) * u + (side == -1 and 2 or -2), a[2] + (b[2] - a[2]) * u + 2 }
      end
      poly[#poly + 1] = b
    end
    scallop(tips[1], tips[2]); scallop(tips[2], tips[3])
    poly[#poly + 1] = { X(22), 42 + by }
    L.poly(wings, poly, "x")
    for _, t in ipairs(tips) do bones[#bones + 1] = { root, t } end
    bones[#bones + 1] = { root, { X(20), 14 + by - flap * 2 } }
  end
  part(parts, "wing", "cloth", 1, wings)

  -- нервы-щупальца снизу
  local nerves = mask()
  for i, nx in ipairs({ 33, 38, 43, 47 }) do
    local sw = math.sin((st.phase or 0) * 1.3 + i * 1.7) * 3
    S.taper(nerves, { nx, 44 + by }, { nx + sw, 56 + by }, { nx - sw * 0.6 + (i - 2.5), 66 + by + (i % 2) * 4 }, 2.2, 0.6)
  end
  part(parts, "nerve", "flesh", 2, nerves)

  local body = mask()
  L.ellipse(body, 40, 31 + by, 17, 16, "x")
  -- шипы-ресницы по верхнему краю века
  for i = -3, 3 do
    local a = math.rad(-90 + i * 22)
    local bx, byy = 40 + math.cos(a) * 15, 31 + by + math.sin(a) * 14
    L.poly(body, { {bx - 2, byy}, {bx + 2, byy}, {40 + math.cos(a) * 22, 31 + by + math.sin(a) * 21} }, "x")
  end
  part(parts, "body", "flesh", 3, body)

  local ball = mask()
  L.ellipse(ball, 40, 32 + by, 12.5, 10.5, "x")
  part(parts, "ball", "white", 4, ball, { noRim = true })

  local out, owner = render(parts)

  -- спицы крыльев: кость с каймой
  for _, b in ipairs(bones) do
    paintLine(out, owner, { wing = true }, { { math.floor(b[1][1]), math.floor(b[1][2]) }, { math.floor(b[2][1]), math.floor(b[2][2]) } }, "rim2")
  end
  -- прожилки на белке
  for _, v in ipairs({ { {29,30}, {32,31}, {34,29} }, { {30,36}, {33,35} }, { {51,29}, {48,31}, {46,30} }, { {50,36}, {47,35}, {45,37} } }) do
    local pts = {}
    for i, p in ipairs(v) do pts[i] = { p[1], p[2] + by } end
    paintLine(out, owner, { ball = true }, pts, "vn")
  end
  -- радужка и зрачок-щель
  local ix, iy = 40 + look * 3, 32 + by
  local iris = mask()
  L.ellipse(iris, ix, iy, 6.5, 6.5, "x")
  L.each(iris, function(x, y)
    local d = math.sqrt((x + 0.5 - ix) ^ 2 + (y + 0.5 - iy) ^ 2)
    local c = d > 5.5 and "ir1" or d > 4 and "ir2" or (d > 2.5 and dither(x, y, 0.5)) and "ir3" or "ir4"
    if y < iy - 3 and d > 4 then c = "ir1" end
    paint(out, owner, { ball = true }, x, y, c)
  end)
  local eyes = {}
  for y = iy - 5, iy + 4 do
    local w = (math.abs(y - iy) < 3) and 1 or 0
    for x = ix - w, ix do paint(out, owner, { ball = true }, math.floor(x), math.floor(y), "K") end
  end
  paint(out, owner, { ball = true }, ix - 4, iy - 4, "sc5"); paint(out, owner, { ball = true }, ix - 3, iy - 4, "sc5")
  paint(out, owner, { ball = true }, ix - 4, iy - 3, "sc5")
  eyes = { { ix - 1, iy }, { ix, iy }, { ix + 2, iy - 2 }, { ix - 3, iy + 2 } }

  -- веки: верхнее опускается при прищуре, нижнее поднимается
  local topLid = 22 + by + lid * 5
  local botLid = 42 + by - lid * 3
  L.each(ball, function(x, y)
    if y < topLid + math.floor(((x - 40) / 13) ^ 2 * 3) then paint(out, owner, { ball = true }, x, y, y == topLid - 1 and "fl1" or "fl2") end
    if y > botLid - math.floor(((x - 40) / 13) ^ 2 * 2) then paint(out, owner, { ball = true }, x, y, "fl1") end
  end)
  -- тёмная складка века над глазом
  for x = 28, 52 do
    local y = topLid + math.floor(((x - 40) / 13) ^ 2 * 3) - 1
    paint(out, owner, { ball = true, body = true }, x, y, "fl0")
  end
  return finish(out, eyes)
end

-- ================= ЧЕРЕП: череп с бараньими рогами, трещина, позвонки, клубящийся мрак =================
local function skull(st)
  local parts = {}
  local by = st.bob or 0
  local jaw = st.jaw or 0
  local ph = st.phase or 0

  -- мрак позади: облако с хвостами-клубами вниз
  local smoke = mask()
  L.ellipse(smoke, 40, 32 + by, 22, 15, "x")
  for i, sx in ipairs({ 22, 30, 40, 50, 58 }) do
    local sw = math.sin(ph * 1.4 + i * 2.1) * 2.5
    S.taper(smoke, { sx, 38 + by }, { sx + sw, 50 + by }, { sx - sw + (i - 3) * 2, 58 + by + (i % 2) * 5 }, 4.5, 0.8)
  end
  part(parts, "smoke", "cloth", 1, smoke)

  -- рога: из висков вверх-наружу, закручиваются вниз
  local horns = mask()
  for side = -1, 1, 2 do
    local function X(x) return side == -1 and x or W - x end
    S.taper(horns, { X(29), 14 + by }, { X(18), 1 + by }, { X(9), 12 + by }, 4.2, 3.2)
    S.taper(horns, { X(9), 12 + by }, { X(4), 24 + by }, { X(13), 26 + by }, 3.2, 1.2)
  end
  part(parts, "horn", "horn", 2, horns)

  -- позвонки, покачиваются под челюстью
  local spine = mask()
  for i = 0, 3 do
    local sx = 40 + math.floor(math.sin(ph * 1.2 + i) * 1.5 + 0.5)
    L.rect(spine, sx - 3 + (i == 3 and 1 or 0), 49 + by + i * 6, sx + 2 - (i == 3 and 1 or 0), 52 + by + i * 6, "x")
    L.rect(spine, sx - 5 + i, 50 + by + i * 6, sx + 4 - i, 51 + by + i * 6, "x")
  end
  part(parts, "spine", "bone", 3, spine)

  local cranium = mask()
  L.ellipse(cranium, 40, 21 + by, 14.5, 14, "x")
  L.poly(cranium, { {28,26 + by}, {52,26 + by}, {50,36 + by}, {44,39 + by}, {36,39 + by}, {30,36 + by} }, "x")
  part(parts, "skull", "bone", 4, cranium)

  local jawM = mask()
  L.poly(jawM, { {31,38 + by + jaw}, {49,38 + by + jaw}, {48,44 + by + jaw}, {43,47 + by + jaw}, {37,47 + by + jaw}, {32,44 + by + jaw} }, "x")
  part(parts, "jaw", "bone", 5, jawM)

  local out, owner = render(parts)

  -- кольца-гребни на рогах
  for side = -1, 1, 2 do
    local function X(x) return side == -1 and x or W - 1 - x end
    for _, r in ipairs({ { {25,7}, {28,10} }, { {20,3}, {21,7} }, { {14,4}, {13,8} }, { {9,9}, {12,11} }, { {6,16}, {9,15} } }) do
      paintLine(out, owner, { horn = true }, { { X(r[1][1]), r[1][2] + by }, { X(r[2][1]), r[2][2] + by } }, "hn0")
    end
  end
  -- трещина на лбу
  paintLine(out, owner, { skull = true }, { {43,8 + by}, {41,12 + by}, {44,15 + by}, {42,19 + by} }, "bn1")
  paintLine(out, owner, { skull = true }, { {44,8 + by}, {42,12 + by}, {45,15 + by} }, "bn5")

  -- глазницы: злые, скошенные к переносице, внутри — красный огонь
  local eyes = {}
  for side = -1, 1, 2 do
    local cx = 40 + side * 6
    local sock = mask()
    -- злая глазница: верхний край опускается к переносице
    local pts = { {30,21}, {37,24}, {38.5,27}, {37,30}, {31.5,30}, {29,27} }
    if side == 1 then pts = mirPts(pts) end
    for i, p in ipairs(pts) do pts[i] = { p[1], p[2] + by } end
    L.poly(sock, pts, "x")
    L.each(sock, function(x, y) paint(out, owner, { skull = true }, x, y, "K") end)
    local gx, gy = cx + (side == -1 and 0 or -1), 26 + by
    L.set(out, gx, gy, "gl3"); L.set(out, gx + 1, gy, "gl2"); L.set(out, gx, gy + 1, "gl1"); L.set(out, gx + 1, gy + 1, "gl1")
    L.set(out, gx - 1, gy + 1, "rim2"); L.set(out, gx + 2, gy + 1, "rim2")
    eyes[#eyes + 1] = { gx, gy }; eyes[#eyes + 1] = { gx + 1, gy }
  end
  -- носовая впадина — перевёрнутое сердце
  for _, p in ipairs({ {39,31}, {41,31}, {38,32}, {39,32}, {40,32}, {41,32}, {42,32}, {39,33}, {40,33}, {41,33}, {40,34} }) do
    paint(out, owner, { skull = true }, p[1], p[2] + by, "K")
  end
  -- зубы: верхний ряд на черепе, нижний на челюсти
  for x = 33, 47 do
    paint(out, owner, { skull = true }, x, 37 + by, (x % 2 == 0) and "K" or "bn4")
    paint(out, owner, { skull = true }, x, 38 + by, "K")
    paint(out, owner, { jaw = true }, x, 39 + by + jaw, (x % 2 == 1) and "K" or "bn4")
  end
  if jaw > 0 then   -- открытая пасть: тьма и отблеск изнутри
    for y = 39 + by, 38 + by + jaw do for x = 33, 47 do
      if not owner[y * W + x] or owner[y * W + x].name ~= "jaw" then L.set(out, x, y, (y == 38 + by + jaw and x % 3 == 0) and "rim2" or "K") end
    end end
  end
  return finish(out, eyes)
end

-- ================= сохранение и превью =================
local ENEMIES = {
  { "Ghost", ghost, {
    idle = { { phase = 0 }, { phase = 1, bob = -1 }, { phase = 2, bob = -1 }, { phase = 3 } },
    laugh = { { mouth = 1, armsUp = 1 }, { mouth = 1, armsUp = 1, bob = -2, phase = 1 }, { mouth = 1, armsUp = 1, phase = 2 } } } },
  { "Eye", eye, {
    idle = { { flap = 0, look = 0 }, { flap = 1, bob = -1, look = -1, phase = 1 }, { flap = 2, bob = -1, look = 0, phase = 2 }, { flap = 1, look = 1, phase = 3 } },
    laugh = { { lid = 1, flap = 2 }, { lid = 2, flap = 0, bob = -2 }, { lid = 1, flap = 2 } } } },
  { "Skull", skull, {
    idle = { { phase = 0 }, { phase = 1, bob = -1 }, { phase = 2, bob = -1 }, { phase = 3 } },
    laugh = { { jaw = 3 }, { jaw = 5, bob = -2, phase = 1 }, { jaw = 3, phase = 2 } } } },
}

-- в игру: Assets/Resources/Art/Enemies/<Имя>[_idle|_windup|_hurt|_laugh|_defeat].png (замах, урон, поражение — из базы)
local rows = {}
for _, e in ipairs(ENEMIES) do
  local name, build, anim = e[1], e[2], e[3]
  S.animations(name, build, { idleFrames = anim.idle, laugh = anim.laugh, laughShift = false })
  local idle, laugh = {}, {}
  for i, st in ipairs(anim.idle) do idle[i] = (build(st)) end
  for i, st in ipairs(anim.laugh) do laugh[i] = (build(st)) end
  rows[#rows + 1] = idle
  rows[#rows + 1] = laugh
end

V.preview(rows, OUT .. "preview_enemies.png")
print("ok")
