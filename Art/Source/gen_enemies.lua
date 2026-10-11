-- Враги 80x80 в стиле Луноликого: объёмное затенение, палитра серых и кроваво-красных, тёмный контур.
-- Кадры: база, idle, замах, урон, смех, поражение (см. bh_shade.lua → S.animations).
local S = dofile("/home/lotwy/black_heart/Art/Source/bh_shade.lua")
local L = S.L
local mx = S.mx

local function rng(seed)
  local s = seed
  return function(n)
    s = (s * 1103515245 + 12345) % 2147483648
    return s % n
  end
end

-- Тёмно-красное свечение вокруг глаз (только поверх указанных деталей)
local function halo(out, owner, eyes, names)
  local set = {}
  for _, p in ipairs(eyes) do set[p[2] * S.W + p[1]] = true end
  for _, p in ipairs(eyes) do
    for _, o in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
      local x, y = p[1] + o[1], p[2] + o[2]
      local c = L.get(out, x, y)
      if not set[y * S.W + x] and (c == "k" or c == "d") then S.paint(out, owner, names, x, y, "c") end
    end
  end
end

-- ================= ПРИЗРАК: балахон, пустота вместо лица, когти =================
local function wraith(st)
  local parts = {}
  local hy = 49 - (st.armsUp or 0) * 7   -- высота кистей (в смехе руки взлетают)

  local robe = S.mask()
  L.poly(robe, { {27,27}, {53,27}, {60,40}, {64,54}, {66,63}, {14,63}, {16,54}, {20,40} }, "x")
  local r = rng(7 + (st.hem or 0))
  local x = 14
  while x < 64 do   -- рваный подол: зубцы разной ширины и длины
    local w = 4 + r(5)
    L.poly(robe, { {x, 62}, {math.min(66, x + w), 62}, {x + r(w + 1), 65 + r(9)} }, "x")
    x = x + w
  end
  parts[#parts + 1] = { name = "robe", ramp = "cloth", z = 1, mask = robe, round = 10 }

  local sleeve = { {21,31}, {30,35}, {19,hy + 3}, {11,hy} }
  local sleeves = S.mask()
  L.poly(sleeves, sleeve, "x")
  L.poly(sleeves, S.mirrorPts(sleeve), "x")
  parts[#parts + 1] = { name = "sleeve", ramp = "cloth", z = 2, mask = sleeves, round = 4, bias = 1 }

  local hood = S.mask()
  L.ellipse(hood, 40, 23, 14, 13, "x")
  L.poly(hood, { {29,17}, {49,14}, {46,2} }, "x")   -- острый кончик капюшона
  parts[#parts + 1] = { name = "hood", ramp = "cloth", z = 3, mask = hood, round = 7, bias = 1 }

  local hands = S.mask()
  L.ellipse(hands, 13, hy + 1, 3, 3, "x")
  L.ellipse(hands, S.W - 13, hy + 1, 3, 3, "x")
  for _, claw in ipairs({ { {12,hy + 2}, {5,hy + 7} }, { {13,hy + 3}, {8,hy + 10} }, { {15,hy + 3}, {13,hy + 11} } }) do
    L.line(hands, claw[1][1], claw[1][2], claw[2][1], claw[2][2], "x")
    L.line(hands, mx(claw[1][1]), claw[1][2], mx(claw[2][1]), claw[2][2], "x")
  end
  parts[#parts + 1] = { name = "hand", ramp = "bone", z = 4, mask = hands, round = 2 }

  local void = S.mask()
  L.ellipse(void, 40, 25, 8, 9, "x")
  parts[#parts + 1] = { name = "void", ramp = "void", z = 5, mask = void, round = 3 }

  local out, owner = S.render(parts)

  -- складки балахона: светлая кромка и тёмная линия
  for _, fold in ipairs({ { {31,37}, {28,50}, {26,62} }, { {40,41}, {41,52}, {40,64} }, { {49,37}, {52,50}, {54,62} } }) do
    local lit = {}
    for i, p in ipairs(fold) do lit[i] = { p[1] + 1, p[2] } end
    S.paintLine(out, owner, { robe = true }, lit, "n")
    S.paintLine(out, owner, { robe = true }, fold, "k")
  end
  for _, p in ipairs({ {22,58}, {23,59}, {47,60}, {57,55}, {33,61}, {34,61} }) do   -- дыры у подола
    S.paint(out, owner, { robe = true }, p[1], p[2], "k")
  end

  -- горящие глаза: злые косые или злорадный прищур
  local eyes = {}
  local function pair(x, y, c)
    L.set(out, x, y, c); eyes[#eyes + 1] = { x, y }
    L.set(out, mx(x), y, c); eyes[#eyes + 1] = { mx(x), y }
  end
  if st.squint then
    pair(33, 24, "a"); pair(34, 23, "l"); pair(35, 22, "h"); pair(36, 22, "j"); pair(37, 23, "l")
  else
    pair(33, 21, "b"); pair(34, 21, "a"); pair(35, 21, "l")
    pair(34, 22, "l"); pair(35, 22, "h"); pair(36, 22, "j"); pair(37, 22, "h")
    pair(36, 23, "a")
  end
  halo(out, owner, eyes, { void = true })

  -- пасть: верхние и нижние зубы, красная глотка
  local open = st.mouth or 0
  local yt, yb = 28 - open, 30 + open
  for y = yt + 1, yb - 1 do
    for x = 36, 44 do S.paint(out, owner, { void = true }, x, y, (y == yt + 1 or y == yb - 1) and "b" or "c") end
  end
  for x = 35, 45 do
    S.paint(out, owner, { void = true }, x, yt, (x % 2 == 0) and "f" or "k")
    S.paint(out, owner, { void = true }, x, yb, (x % 2 == 1) and "e" or "k")
  end

  return L.outline(out, 1, "k"), { eyes = eyes }
end

-- ================= ГЛАЗ: кровавое яблоко, злые веки, крылья, нервы =================
local function eyeball(st)
  local parts = {}
  local u = (st.flap or 0) * 3
  local lid = st.lid or 0
  local sway = st.sway or 0

  local wing = { {27,30}, {15,19 - u}, {6,10 - u * 2}, {9,23 - u}, {4,29 - u}, {11,34 - u}, {6,43 - u}, {15,42}, {21,47}, {27,42} }
  local membrane = S.mask()
  L.poly(membrane, wing, "x")
  L.poly(membrane, S.mirrorPts(wing), "x")
  parts[#parts + 1] = { name = "membrane", ramp = "membrane", z = 1, mask = membrane, round = 3 }

  local bones = S.mask()
  for side = 0, 1 do
    local function P(x, y) return side == 0 and { x, y } or { S.W - x, y } end
    S.taper(bones, P(27,32), P(16,22 - u), P(6,10 - u * 2), 1.6, 0.6)
    S.taper(bones, P(25,35), P(13,31 - u), P(4,29 - u), 1.3, 0.6)
    S.taper(bones, P(25,38), P(15,41 - u), P(6,43 - u), 1.3, 0.6)
  end
  parts[#parts + 1] = { name = "bones", ramp = "horn", z = 2, mask = bones, round = 1 }

  local tendrils = S.mask()
  S.taper(tendrils, {33,50}, {29 + sway,60}, {31 - sway,69}, 2.2, 0.7)
  S.taper(tendrils, {40,52}, {44 - sway,62}, {40 + sway,72}, 2.4, 0.7)
  S.taper(tendrils, {47,50}, {51 + sway,58}, {49 - sway,66}, 2.0, 0.7)
  parts[#parts + 1] = { name = "tendril", ramp = "flesh", z = 2, mask = tendrils, round = 2, bias = -1 }

  local ball = S.mask()
  L.ellipse(ball, 40, 35, 17, 17, "x")
  parts[#parts + 1] = { name = "ball", ramp = "pale", z = 3, mask = ball, round = 11 }

  local iris = S.mask()
  L.ellipse(iris, 41, 36, 7.5, 7.5, "x")
  parts[#parts + 1] = { name = "iris", ramp = "flesh", z = 5, mask = iris, round = 4 }

  -- веки со злым наклоном; в смехе прищур сильнее
  local lids, cover = S.mask(), S.mask()
  L.ellipse(cover, 40, 35, 19, 19, "x")
  L.each(cover, function(x, y)
    local upper = 24 + lid * 3 + (40 - x) * 0.16
    local lower = 47 - lid * 2 + (x - 40) * 0.05
    if y < upper or y > lower then L.set(lids, x, y, "x") end
  end)
  parts[#parts + 1] = { name = "lid", ramp = "fleshdark", z = 6, mask = lids, round = 3, bias = 1 }

  local out, owner = S.render(parts)

  -- шипы по верхнему краю века
  for a = -150, -30, 30 do
    local ca, sa = math.cos(math.rad(a)), math.sin(math.rad(a))
    L.line(out, math.floor(40 + ca * 19), math.floor(35 + sa * 19), math.floor(40 + ca * 23), math.floor(35 + sa * 23), "d")
  end

  -- кровавые прожилки на белке
  for _, v in ipairs({ { {24,34}, {29,36}, {32,34} }, { {25,42}, {30,41}, {33,43} }, { {56,39}, {52,38}, {49,40} },
                       { {54,29}, {50,31} }, { {33,49}, {35,46} }, { {47,48}, {46,45} } }) do
    S.paintLine(out, owner, { ball = true }, v, "b")
  end

  -- зрачок-щель и блик
  local pupil = {}
  for y = 30 + lid, 42 - lid do
    local w = (y >= 34 and y <= 38) and 1 or 0
    for x = 41 - w, 41 + w do
      if owner[y * S.W + x] and owner[y * S.W + x].name == "iris" then
        L.set(out, x, y, "k"); pupil[#pupil + 1] = { x, y }
      end
    end
  end
  S.paint(out, owner, { iris = true }, 37, 32, "j")
  S.paint(out, owner, { iris = true }, 38, 32, "f")
  S.paint(out, owner, { iris = true }, 37, 33, "j")

  -- капли крови с нервов
  for _, d in ipairs({ {31 - sway,70}, {40 + sway,73}, {49 - sway,67} }) do
    L.set(out, d[1], d[2] + 1, "a"); L.set(out, d[1], d[2] + 2, "l")
  end

  return L.outline(out, 1, "k"), { eyes = pupil }
end

-- ================= ЧЕРЕП: рога, горящие глазницы, клыки, рваный капюшон =================
local function skull(st)
  local parts = {}
  local j = st.jaw or 0

  local cloak = S.mask()
  L.poly(cloak, { {22,38}, {58,38}, {67,50}, {72,64}, {63,61}, {59,72}, {51,64}, {45,74}, {40,66},
                  {35,74}, {29,64}, {21,72}, {17,61}, {8,64}, {13,50} }, "x")
  parts[#parts + 1] = { name = "cloak", ramp = "cloth", z = 1, mask = cloak, round = 7 }

  local hornCurves = { { {27,19}, {12,20}, {12,2} }, { {53,19}, {68,20}, {68,2} } }
  local horns = S.mask()
  for _, c in ipairs(hornCurves) do S.taper(horns, c[1], c[2], c[3], 4.2, 0.7) end
  parts[#parts + 1] = { name = "horn", ramp = "horn", z = 2, mask = horns, round = 3 }

  local cranium = S.mask()
  L.ellipse(cranium, 40, 25, 17, 15, "x")
  L.poly(cranium, { {24,28}, {56,28}, {53,44}, {27,44} }, "x")
  parts[#parts + 1] = { name = "cranium", ramp = "bonedark", z = 3, mask = cranium, round = 12 }

  local jaw = S.mask()
  L.poly(jaw, { {28,47 + j}, {52,47 + j}, {49,55 + j}, {31,55 + j} }, "x")
  parts[#parts + 1] = { name = "jaw", ramp = "bonedark", z = 4, mask = jaw, round = 4, bias = -1 }

  local out, owner = S.render(parts)

  -- кольца на рогах (поперёк изгиба)
  for _, c in ipairs(hornCurves) do
    for t = 0.2, 0.8, 0.2 do
      local u1 = 1 - t
      local x = u1 * u1 * c[1][1] + 2 * u1 * t * c[2][1] + t * t * c[3][1]
      local y = u1 * u1 * c[1][2] + 2 * u1 * t * c[2][2] + t * t * c[3][2]
      local tx = 2 * u1 * (c[2][1] - c[1][1]) + 2 * t * (c[3][1] - c[2][1])
      local ty = 2 * u1 * (c[2][2] - c[1][2]) + 2 * t * (c[3][2] - c[2][2])
      local len = math.sqrt(tx * tx + ty * ty)
      local px, py = -ty / len, tx / len
      local rr = 4.2 * (1 - t) + 1
      S.paintLine(out, owner, { horn = true },
        { { math.floor(x - px * rr), math.floor(y - py * rr) }, { math.floor(x + px * rr), math.floor(y + py * rr) } }, "k")
    end
  end

  -- злые скошенные глазницы
  local socket = { {26,25}, {36,29}, {36,34}, {29,34}, {26,30} }
  local sockets = S.mask()
  L.poly(sockets, socket, "x")
  L.poly(sockets, S.mirrorPts(socket), "x")
  S.paintMask(out, owner, { cranium = true }, sockets, "k")

  -- горящие зрачки
  local eyes = {}
  local function pair(x, y, c)
    L.set(out, x, y, c); eyes[#eyes + 1] = { x, y }
    L.set(out, mx(x), y, c); eyes[#eyes + 1] = { mx(x), y }
  end
  pair(31, 30, (st.glow or 0) > 0 and "f" or "j")
  pair(30, 30, "l"); pair(32, 30, "h"); pair(31, 29, "h"); pair(31, 31, "a")
  pair(32, 31, "b"); pair(30, 29, "b")

  -- носовая впадина
  local nose = S.mask()
  L.poly(nose, { {37.5,36}, {42.5,36}, {40,41} }, "x")
  S.paintMask(out, owner, { cranium = true }, nose, "k")

  -- верхние зубы с клыками
  for x = 28, 51 do S.paint(out, owner, { cranium = true }, x, 41, "k") end
  for x = 28, 51 do
    local tooth = (x - 28) % 3 ~= 2
    S.paint(out, owner, { cranium = true }, x, 42, tooth and "e" or "k")
    S.paint(out, owner, { cranium = true }, x, 43, tooth and "i" or "k")
  end
  for _, x in ipairs({ 31, 32, 47, 48 }) do L.set(out, x, 44, "e"); L.set(out, x, 45, "i") end

  -- нижние зубы
  for x = 31, 48 do
    local tooth = (x - 31) % 3 ~= 2
    S.paint(out, owner, { jaw = true }, x, 47 + j, tooth and "f" or "k")
    S.paint(out, owner, { jaw = true }, x, 48 + j, tooth and "e" or "k")
  end

  -- трещины
  S.paintLine(out, owner, { cranium = true }, { {46,11}, {44,15}, {47,18}, {45,22} }, "k")
  S.paintLine(out, owner, { cranium = true }, { {30,13}, {32,16}, {31,19} }, "k")

  return L.outline(out, 1, "k"), { eyes = eyes }
end

S.animations("Ghost", wraith, {
  idle = { hem = 1 },
  laugh = { { squint = true, mouth = 1, armsUp = 1 }, { squint = true, mouth = 2, armsUp = 2 }, { squint = true, mouth = 1, armsUp = 1 } },
})
S.animations("Eye", eyeball, {
  idle = { flap = -1, sway = 1 },
  laugh = { { lid = 2, flap = 1 }, { lid = 2, flap = 0, sway = 1 }, { lid = 2, flap = -1 } },
})
S.animations("Skull", skull, {
  idle = { glow = 1 },
  laugh = { { jaw = 3, glow = 1 }, { jaw = 6, glow = 1 }, { jaw = 3, glow = 1 } },
})
