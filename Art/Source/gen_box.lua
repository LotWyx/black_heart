-- Детали рамки боя: уголки и метки на серединах сторон.
-- Основной спрайт — белое с чёрным контуром; *Accent — пиксели акцентной линии (белые, красятся в коде
-- в цвет внутренней линии рамки: акцент / замах / остановка времени).
-- Белая полоса рамки (3 px) в уголке — столбцы/строки 2..4, внутренняя акцентная линия — 6.
local L = dofile("/home/lotwy/black_heart/Art/Source/bh_lib.lua")

local corner = {   -- левый верхний; остальные — отражением в коде. a — акцентные пиксели
  "KKKKKKK...",
  "KWWWWWK...",
  "KWWaWWWWWW",
  "KWaaaWWWWW",
  "KWWaWWWWWW",
  "KWWWW.....",
  "KKWWW.aaaa",
  "..WWW.a...",
  "..WWW.a.a.",
  "..WWW.a...",
}
local notch = {    -- метка в середине верхней стороны
  "..KKK..",
  ".KWWWK.",
  "WWWaWWW",
  "WWaaaWW",
  "WWWaWWW",
  "...a...",
}

local function layers(rows)
  local base, acc = {}, {}
  for i, r in ipairs(rows) do
    base[i] = r:gsub("a", "W")
    acc[i] = r:gsub("[KW]", "."):gsub("a", "W")
  end
  return L.rows(base), L.rows(acc)
end

local function transpose(rows)
  local out = {}
  for x = 1, #rows[1] do
    local s = ""
    for y = 1, #rows do s = s .. rows[y]:sub(x, x) end
    out[x] = s
  end
  return out
end

local b, a = layers(corner)
L.save("UI/BoxCorner", { b }); L.save("UI/BoxCornerAccent", { a })
b, a = layers(notch)
L.save("UI/BoxNotchH", { b }); L.save("UI/BoxNotchHAccent", { a })
b, a = layers(transpose(notch))   -- метка левой стороны
L.save("UI/BoxNotchV", { b }); L.save("UI/BoxNotchVAccent", { a })
