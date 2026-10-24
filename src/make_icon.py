"""生成 桌面键盘.ico（需要 Pillow）。"""
from PIL import Image, ImageDraw

S = 256
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

d.rounded_rectangle([10, 10, S - 10, S - 10], radius=52, fill=(21, 25, 33, 255),
                    outline=(56, 189, 248, 150), width=4)

kw, kh, gap = 44, 40, 16
x0 = (S - (3 * kw + 2 * gap)) // 2
y0 = 58

# 上排：三个亮键
for i in range(3):
    x = x0 + i * (kw + gap)
    d.rounded_rectangle([x, y0, x + kw, y0 + kh], radius=11, fill=(46, 158, 214, 255))

# 中排：三个暗键
y1 = y0 + kh + gap
for i in range(3):
    x = x0 + i * (kw + gap)
    d.rounded_rectangle([x, y1, x + kw, y1 + kh], radius=11, fill=(70, 82, 98, 255))

# 底排：空格
y2 = y1 + kh + gap
d.rounded_rectangle([x0, y2, x0 + 3 * kw + 2 * gap, y2 + kh], radius=11,
                    fill=(70, 82, 98, 255))

img.save("桌面键盘.ico", sizes=[(256, 256), (128, 128), (64, 64), (48, 48),
                                (32, 32), (24, 24), (16, 16)])
print("ok")
