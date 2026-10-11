-- Иконки подсказок обучения: перчатка-палец (сенсор), мышь с ЛКМ (ПК), красный крестик
local L = dofile("/home/lotwy/black_heart/Art/Source/bh_lib.lua")
L.COLORS.A = { 0xDB, 0x40, 0x6B }   -- акцент интерфейса (нажатая ЛКМ, «касание»)

-- Перчатка с вытянутым указательным пальцем; кончик пальца — пиксель (6, 1)
local FINGER = {
  "......KK........",
  ".....KWWK.......",
  ".....KWWK.......",
  ".....KWWK.......",
  ".....KWWKKK.....",
  ".....KWWKWWKK...",
  "..KK.KWWKWWKWK..",
  ".KWWKKWWKWWKWWK.",
  ".KWWWKWWWWWWWWK.",
  "..KWWWWWWWWWWgK.",
  "..KWWWWWWWWWWgK.",
  "...KWWWWWWWWWgK.",
  "....KWWWWWWWgK..",
  ".....KWWWWWWgK..",
  ".....KKKKKKKK...",
  "................",
}
local finger = L.rows(FINGER)
-- Нажатие: рука на 1 px ниже, у кончика пальца — штрихи касания
local pressed = L.shift(finger, 0, 1)
for _, p in ipairs({ {3, 0}, {2, 1}, {9, 0}, {10, 1}, {6, 0} }) do
  if not L.get(pressed, p[1], p[2]) then L.set(pressed, p[1], p[2], "A") end
end
L.save("UI/HintFinger", { finger, pressed }, { 0.2 })

-- Мышь: L — левая кнопка (в кадре нажатия — акцентный цвет)
local MOUSE = {
  "......K.........",
  "......K.........",
  "...KKKKKKKK.....",
  "..KLLLLKWWWK....",
  ".KLLLLLKWWWWK...",
  ".KLLLLLKWWWWK...",
  ".KLLLLLKWWWWK...",
  ".KKKKKKKKKKKK...",
  ".KWWWWWWWWWgK...",
  ".KWWWWWWWWWgK...",
  ".KWWWWWWWWWgK...",
  "..KWWWWWWWgK....",
  "..KWWWWWWWgK....",
  "...KKKKKKKK.....",
  "................",
  "................",
}
local mouse = L.rows(MOUSE)
L.save("UI/HintMouse", { L.recolor(mouse, { L = "W" }), L.recolor(mouse, { L = "A" }) }, { 0.2 })

-- Красный крестик «нельзя резать»
L.save("UI/HintCross", { L.rows({
  "KK.....KK",
  "KDK...KDK",
  ".KDK.KDK.",
  "..KDKDK..",
  "...KDK...",
  "..KDKDK..",
  ".KDK.KDK.",
  "KDK...KDK",
  "KK.....KK",
}) })
