-- Фон главного меню 320x180, 16-бит, кровавая гамма (как фон боя): почти чёрное небо
-- с тёмно-кровавым свечением у горизонта, кровавая луна, бурые облака, горы с красной кромкой,
-- тёмно-красное море с мерцающей лунной дорожкой. 3 кадра.
local L = dofile("/home/lotwy/black_heart/Art/Source/bh_lib.lua")

local function hex(h)
  return { tonumber(h:sub(2, 3), 16), tonumber(h:sub(4, 5), 16), tonumber(h:sub(6, 7), 16) }
end

-- Палитра меню (ключи не пересекаются с метками библиотеки)
local P = {
  ["1"] = "#050507", ["2"] = "#08070A", ["3"] = "#0D090D", ["4"] = "#140A0E", ["5"] = "#1C0B10",
  ["6"] = "#260D12", ["7"] = "#321014", ["8"] = "#421216", ["9"] = "#561518", ["0"] = "#6E1A1A",
  k = "#C42A22",                                   -- кровавое свечение горизонта под луной
  m = "#B0201E", n = "#7E1218", o = "#8E161A",     -- луна: основа, тень, кратеры
  q = "#D8402C", u = "#F07A50", h = "#5A0A10",     -- луна: свет, блик, ореол
  z = "#1A1216", Z = "#5A4038",                    -- облака: тень и край, подсвеченный луной
  a = "#08060A", b = "#3A1014",                    -- горы: силуэт и красная кромка
  c = "#1A0306", d = "#2A0508", e = "#3A070C",     -- море
  f = "#B42420", j = "#E04A34",                    -- лунная дорожка
  x = "#A89898", X = "#C42A22",                    -- звёзды: тусклые и красные
  H = "#FFD2B4",                                   -- бледный серп луны
  A = "#2E2226", F = "#3A1014",                    -- облака: полутон и красное подбрюшье
}
for key, value in pairs(P) do L.COLORS[key] = hex(value) end

local W, H, HORIZON = 320, 180, 120
local MX, MY, MR = 214, 84, 42      -- луна

local function dither(x, y) return (x + y) % 2 == 0 end

local function frame(f)
  local g = L.grid(W, H)

  -- Небо: 10 ступеней к горизонту, на стыках — шахматный дизеринг
  local sky = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" }
  for y = 0, HORIZON - 1 do
    local t = (y / (HORIZON - 1)) ^ 1.7 * #sky
    local band = math.min(#sky, math.floor(t) + 1)
    local frac = t - math.floor(t)
    for x = 0, W - 1 do
      local c = sky[band]
      if frac > 0.75 and band < #sky and dither(x, y) then c = sky[band + 1] end
      L.set(g, x, y, c)
    end
  end

  -- Звёзды в верхней части (часть мерцает от кадра к кадру)
  local seed = 11
  local function rnd(n) seed = (seed * 1103515245 + 12345) % 2147483648; return seed % n end
  for i = 1, 34 do
    local sx, sy = rnd(W), rnd(62)
    local dx, dy = sx - MX, sy - MY
    if dx * dx + dy * dy > (MR + 8) ^ 2 and (i + f) % 5 ~= 0 then
      L.set(g, sx, sy, i % 4 == 0 and "X" or "x")
    end
  end
  for _, s in ipairs({ {38, 14}, {292, 22}, {150, 30} }) do   -- крупные звёзды-крестики
    local c = (f == 1) and "X" or "x"
    L.set(g, s[1], s[2], "x")
    if f ~= 2 then
      L.set(g, s[1] - 1, s[2], c); L.set(g, s[1] + 1, s[2], c)
      L.set(g, s[1], s[2] - 1, c); L.set(g, s[1], s[2] + 1, c)
    end
  end

  -- Ореол луны: дизеринговое кольцо
  for y = MY - MR - 6, HORIZON - 1 do
    for x = MX - MR - 6, MX + MR + 6 do
      local d = math.sqrt((x + 0.5 - MX) ^ 2 + (y + 0.5 - MY) ^ 2)
      if d > MR and d <= MR + 3 and dither(x, y) then L.set(g, x, y, "h")
      elseif d > MR + 3 and d <= MR + 5 and (x % 2 == 0 and y % 2 == 0) then L.set(g, x, y, "h") end
    end
  end

  -- Луна: объёмное освещение сверху-слева (как у луны в бою), кратеры, бледный серп по краю
  local craters = { {200, 70, 6}, {229, 92, 8}, {212, 106, 5}, {233, 64, 4}, {195, 95, 4}, {218, 80, 3} }
  local lx, ly, lz = -0.7, -0.45, 0.55
  local ll = math.sqrt(lx * lx + ly * ly + lz * lz)
  lx, ly, lz = lx / ll, ly / ll, lz / ll
  for y = MY - MR, HORIZON - 1 do
    for x = MX - MR, MX + MR do
      local dx, dy = (x + 0.5 - MX) / MR, (y + 0.5 - MY) / MR
      local r2 = dx * dx + dy * dy
      if r2 <= 1 then
        local s = dx * lx + dy * ly + math.sqrt(1 - r2) * lz
        for _, cr in ipairs(craters) do
          if (x + 0.5 - cr[1]) ^ 2 + (y + 0.5 - cr[2]) ^ 2 < cr[3] ^ 2 then s = s - 0.22 end
        end
        local c = s > 0.8 and "u" or s > 0.55 and "q" or s > 0.25 and "m" or s > 0.0 and "o" or "n"
        if math.sqrt(r2) > 1 - 1.3 / MR and dx < -0.35 and dy < 0.2 then c = "H" end
        L.set(g, x, y, c)
      end
    end
  end

  -- Облака-полосы поверх луны и неба (верхний край подсвечен)
  -- каждое облако — несколько вытянутых эллипсов {cx, cy, rx, ry}
  local clouds = {
    { {196, 98, 46, 3}, {232, 95, 30, 3}, {262, 99, 28, 2} },
    { {150, 107, 40, 2}, {188, 105, 26, 3}, {220, 108, 30, 2} },
    { {282, 72, 30, 2}, {306, 70, 18, 2} },
    { {30, 54, 34, 2}, {58, 52, 20, 2} },
    { {96, 64, 34, 1.5}, {120, 62, 18, 2} },
  }
  local cloud = L.grid(W, H)
  for _, parts in ipairs(clouds) do
    for _, e in ipairs(parts) do
      L.ellipse(cloud, e[1], e[2], e[3], e[4] * 1.7, "z")                          -- тело облака
      L.ellipse(cloud, e[1] - e[3] * 0.3, e[2] - e[4], e[3] * 0.45, e[4] * 1.4, "z") -- пушистый верх
    end
  end
  L.each(cloud, function(x, y)
    local c = "z"
    if not L.get(cloud, x, y - 1) then c = "Z"            -- верхний край подсвечен луной
    elseif not L.get(cloud, x, y - 2) then c = "A"        -- полутон под краем
    elseif not L.get(cloud, x, y + 1) then c = "F" end    -- подбрюшье в отсвете горизонта
    L.set(g, x, y, c)
  end)

  -- Горы: силуэт с подсвеченным краем
  local left = { {0,96}, {22,84}, {40,98}, {66,80}, {96,104}, {120,112}, {146,120}, {0,120} }
  local right = { {268,120}, {290,110}, {306,102}, {319,98}, {319,120} }
  L.poly(g, left, "a"); L.poly(g, right, "a")
  L.polyline(g, { {0,96}, {22,84}, {40,98}, {66,80}, {96,104}, {120,112}, {146,120} }, "b")
  L.polyline(g, { {268,120}, {290,110}, {306,102}, {319,98} }, "b")

  -- Горизонт: светлая линия, ярче под луной
  for x = 0, W - 1 do
    L.set(g, x, HORIZON, (x > MX - 60 and x < MX + 60) and "k" or "9")
  end

  -- Вода: тёмные полосы с дизерингом
  for y = HORIZON + 1, H - 1 do
    local c = y < 132 and "e" or (y < 152 and "d" or "c")
    for x = 0, W - 1 do
      local cc = c
      if (y == 131 or y == 151) and dither(x, y) then cc = (y == 131) and "d" or "c" end
      L.set(g, x, y, cc)
    end
  end

  -- Редкая рябь: светлые штрихи на тёмных полосах, бегут от кадра к кадру
  for y = HORIZON + 4, H - 2, 4 do
    local len = 3 + (y - HORIZON) // 10
    for x = 0, W - 1 do
      if ((x + f * 2 + y * 13) % 37) < len then
        local under = L.get(g, x, y)
        if under == "c" then L.set(g, x, y, "d") elseif under == "d" then L.set(g, x, y, "e") end
      end
    end
  end

  -- Отражение луны: прерывистые штрихи, сужаются книзу, сдвигаются по кадрам (мерцание)
  for y = HORIZON + 2, H - 4, 3 do
    local half = math.floor(40 - (y - HORIZON) * 0.55)
    if half > 2 then
      for x = MX - half, MX + half do
        if ((x + f * 3 + y * 7) % 11) < 7 then
          local c = (y < HORIZON + 14) and "j" or "f"
          if ((x + f * 5) % 13) == 0 then c = "k" end
          L.set(g, x, y, c)
        end
      end
    end
  end

  return g
end

L.save("Background/MenuMoon", { frame(0), frame(1), frame(2) }, { 0.25 })
