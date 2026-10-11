-- Превью: увеличенные полоски кадров на фоне #0B0B12.
-- Перед dofile задайте PREVIEW_IDS = {"Player/Soul_pulse", ...}, PREVIEW_SCALE, PREVIEW_OUT.
local ART = "/home/lotwy/black_heart/Assets/Resources/Art/"
local scale = PREVIEW_SCALE or 8
local pad = 4

local imgs, w, h = {}, 0, pad
for _, id in ipairs(PREVIEW_IDS) do
  local img = Image{ fromFile = ART .. id .. ".png" }
  imgs[#imgs + 1] = img
  w = math.max(w, img.width * scale + pad * 2)
  h = h + img.height * scale + pad
end

local out = Image(w, h, ColorMode.RGB)
out:clear(app.pixelColor.rgba(11, 11, 18, 255))
local y0 = pad
for _, img in ipairs(imgs) do
  for y = 0, img.height - 1 do
    for x = 0, img.width - 1 do
      local c = img:getPixel(x, y)
      if app.pixelColor.rgbaA(c) > 0 then
        for sy = 0, scale - 1 do
          for sx = 0, scale - 1 do out:putPixel(pad + x * scale + sx, y0 + y * scale + sy, c) end
        end
      end
    end
  end
  y0 = y0 + img.height * scale + pad
end
out:saveAs(PREVIEW_OUT)
print("preview " .. PREVIEW_OUT .. " " .. w .. "x" .. h)
