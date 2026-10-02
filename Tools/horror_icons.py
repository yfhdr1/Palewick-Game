#!/usr/bin/env python3
"""Palewick horror icon generator.
Generates dark, grunge horror-styled UI icons for the HUD and the Shop.
Usage: python3 Tools/horror_icons.py  (run from repo root)
"""
import math
import os
import random
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HUD_DIR = os.path.join(ROOT, "Assets", "UI_Icons")
SHOP_DIR = os.path.join(ROOT, "Assets", "UI_Lobby", "Shop")

BONE = (232, 221, 208, 255)
BONE_DIM = (196, 186, 174, 255)
BLOOD = (150, 18, 22, 255)
BLOOD_DARK = (84, 10, 14, 255)
EMBER = (255, 120, 60, 255)

random.seed(13)


def radial_gradient(size, inner, outer):
    img = Image.new("RGBA", (size, size))
    px = img.load()
    c = (size - 1) / 2.0
    maxd = c * math.sqrt(2)
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - c, y - c) / maxd
            d = min(1.0, d * 1.25)
            r = int(inner[0] + (outer[0] - inner[0]) * d)
            g = int(inner[1] + (outer[1] - inner[1]) * d)
            b = int(inner[2] + (outer[2] - inner[2]) * d)
            px[x, y] = (r, g, b, 255)
    return img


def add_grunge(img, amount=900, alpha=26):
    d = ImageDraw.Draw(img, "RGBA")
    w, h = img.size
    for _ in range(amount):
        x = random.randint(0, w - 1)
        y = random.randint(0, h - 1)
        s = random.randint(1, max(2, w // 170))
        shade = random.randint(0, 40)
        d.ellipse([x, y, x + s, y + s], fill=(shade, shade, shade, alpha))
    for _ in range(10):
        x0 = random.randint(0, w)
        y0 = random.randint(0, h)
        ang = random.uniform(0, math.pi)
        ln = random.randint(w // 8, w // 3)
        x1 = x0 + int(math.cos(ang) * ln)
        y1 = y0 + int(math.sin(ang) * ln)
        d.line([x0, y0, x1, y1], fill=(0, 0, 0, 18), width=max(1, w // 340))
    return img


def circle_mask(size, radius_frac=1.0):
    m = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(m)
    pad = size * (1 - radius_frac) / 2
    d.ellipse([pad, pad, size - pad, size - pad], fill=255)
    return m


def hud_base(size_ss):
    s = size_ss
    img = radial_gradient(s, (34, 24, 26), (7, 5, 6))
    add_grunge(img, 1200, 22)
    d = ImageDraw.Draw(img, "RGBA")
    # outer steel ring
    rim = s * 0.028
    d.ellipse([rim * 0.5, rim * 0.5, s - rim * 0.5, s - rim * 0.5],
              outline=(14, 10, 11, 255), width=int(rim))
    # blood ring
    r2 = s * 0.055
    d.ellipse([r2, r2, s - r2, s - r2], outline=BLOOD_DARK, width=int(s * 0.012))
    d.ellipse([r2 * 1.35, r2 * 1.35, s - r2 * 1.35, s - r2 * 1.35],
              outline=(120, 16, 20, 160), width=max(2, int(s * 0.004)))
    # top-left dim highlight
    hl = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    hd = ImageDraw.Draw(hl)
    hd.ellipse([s * 0.12, s * 0.08, s * 0.72, s * 0.5], fill=(255, 240, 230, 14))
    hl = hl.filter(ImageFilter.GaussianBlur(s * 0.05))
    img = Image.alpha_composite(img, hl)
    return img


def glyph_layer(size_ss, draw_fn, color=BONE, glow=BLOOD, glow_strength=1.0):
    s = size_ss
    glyph = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    draw_fn(ImageDraw.Draw(glyph, "RGBA"), s, color)
    # red glow behind the glyph
    alpha = glyph.split()[3]
    glow_img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    glow_img.paste(Image.new("RGBA", (s, s), glow), (0, 0), alpha)
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(s * 0.03))
    if glow_strength != 1.0:
        a = glow_img.split()[3].point(lambda v: int(v * glow_strength))
        glow_img.putalpha(a)
    out = Image.alpha_composite(glow_img, glyph)
    return out


def finish_hud(base, glyph, out_path, size=256):
    s = base.size[0]
    img = Image.alpha_composite(base, glyph)
    img.putalpha(Image.composite(img.split()[3], Image.new("L", (s, s), 0), circle_mask(s)))
    img = img.resize((size, size), Image.LANCZOS)
    img.save(out_path)
    print("saved", os.path.relpath(out_path, ROOT))


def P(s, pts):
    return [(x * s, y * s) for x, y in pts]


def rot(pts, cx, cy, ang):
    ca, sa = math.cos(ang), math.sin(ang)
    out = []
    for x, y in pts:
        dx, dy = x - cx, y - cy
        out.append((cx + dx * ca - dy * sa, cy + dx * sa + dy * ca))
    return out


# ---------------- HUD glyphs ----------------

def g_flashlight(d, s, c):
    w = s * 0.004
    body = [(0.46, 0.40), (0.54, 0.40), (0.56, 0.78), (0.44, 0.78)]
    head = [(0.40, 0.26), (0.60, 0.26), (0.56, 0.40), (0.44, 0.40)]
    pts_b = rot(P(s, body), s * 0.5, s * 0.5, math.radians(35))
    pts_h = rot(P(s, head), s * 0.5, s * 0.5, math.radians(35))
    d.polygon(pts_b, fill=c)
    d.polygon(pts_h, fill=c)
    # beam rays
    for i, (x0, y0, x1, y1) in enumerate([
            (0.42, 0.20, 0.34, 0.08), (0.50, 0.17, 0.48, 0.04), (0.58, 0.20, 0.63, 0.07)]):
        a = rot(P(s, [(x0, y0), (x1, y1)]), s * 0.5, s * 0.5, math.radians(35))
        d.line(a, fill=c, width=int(s * 0.018))


def g_jump(d, s, c):
    wdt = int(s * 0.055)
    for off in (0.0, 0.17):
        d.line(P(s, [(0.30, 0.52 + off), (0.50, 0.30 + off)]), fill=c, width=wdt)
        d.line(P(s, [(0.50, 0.30 + off), (0.70, 0.52 + off)]), fill=c, width=wdt)
    d.line(P(s, [(0.30, 0.82), (0.70, 0.82)]), fill=c, width=int(s * 0.04))


def g_sprint(d, s, c):
    # running man silhouette (simplified)
    d.ellipse(P(s, [(0.52, 0.16), (0.66, 0.30)]), fill=c)
    w = int(s * 0.05)
    d.line(P(s, [(0.56, 0.32), (0.46, 0.55)]), fill=c, width=w)          # torso
    d.line(P(s, [(0.55, 0.38), (0.72, 0.46)]), fill=c, width=int(w*0.8)) # arm fwd
    d.line(P(s, [(0.55, 0.38), (0.36, 0.33)]), fill=c, width=int(w*0.8)) # arm back
    d.line(P(s, [(0.46, 0.55), (0.66, 0.66)]), fill=c, width=w)          # leg fwd
    d.line(P(s, [(0.66, 0.66), (0.62, 0.84)]), fill=c, width=int(w*0.85))
    d.line(P(s, [(0.46, 0.55), (0.33, 0.70)]), fill=c, width=w)          # leg back
    d.line(P(s, [(0.33, 0.70), (0.18, 0.72)]), fill=c, width=int(w*0.85))
    for y in (0.36, 0.48, 0.60):
        d.line(P(s, [(0.12, y), (0.26, y)]), fill=(c[0], c[1], c[2], 150), width=int(s * 0.022))


def g_crouch(d, s, c):
    wdt = int(s * 0.055)
    for off in (0.0, 0.17):
        d.line(P(s, [(0.30, 0.30 + off), (0.50, 0.52 + off)]), fill=c, width=wdt)
        d.line(P(s, [(0.50, 0.52 + off), (0.70, 0.30 + off)]), fill=c, width=wdt)
    d.line(P(s, [(0.30, 0.82), (0.70, 0.82)]), fill=c, width=int(s * 0.04))


def g_interact(d, s, c):
    # door slightly open + motion arrow
    w = int(s * 0.03)
    d.rectangle(P(s, [(0.30, 0.22), (0.62, 0.80)]), outline=c, width=w)
    d.polygon(P(s, [(0.34, 0.26), (0.56, 0.20), (0.56, 0.84), (0.34, 0.76)]), fill=c)
    d.ellipse(P(s, [(0.49, 0.48), (0.54, 0.545)]), fill=(20, 14, 15, 255))
    d.line(P(s, [(0.66, 0.50), (0.84, 0.50)]), fill=c, width=w)
    d.polygon(P(s, [(0.82, 0.43), (0.92, 0.50), (0.82, 0.57)]), fill=c)


def g_chat(d, s, c):
    w = int(s * 0.03)
    d.rounded_rectangle(P(s, [(0.22, 0.26), (0.78, 0.62)]), radius=s * 0.09, outline=c, width=w)
    d.polygon(P(s, [(0.36, 0.60), (0.50, 0.60), (0.32, 0.78)]), fill=c)
    for x in (0.34, 0.47, 0.60):
        d.ellipse(P(s, [(x, 0.41), (x + 0.06, 0.47)]), fill=c)


def g_settings(d, s, c):
    cx = cy = 0.5
    r_out, r_in, r_hole = 0.26, 0.19, 0.085
    teeth = 8
    pts = []
    for i in range(teeth * 4):
        ang = i * (2 * math.pi) / (teeth * 4)
        step = i % 4
        r = r_out if step in (0, 1) else r_in
        a2 = ang + 0.06 if step in (1, 3) else ang
        pts.append((cx + math.cos(a2) * r, cy + math.sin(a2) * r))
    d.polygon(P(s, pts), fill=c)
    d.ellipse(P(s, [(cx - r_hole, cy - r_hole), (cx + r_hole, cy + r_hole)]), fill=(16, 11, 12, 255))


def g_view(d, s, c):
    # creepy eye with slit pupil
    w = int(s * 0.03)
    d.arc(P(s, [(0.18, 0.26), (0.82, 0.86)]), 200, 340, fill=c, width=w)
    d.arc(P(s, [(0.18, 0.14), (0.82, 0.74)]), 20, 160, fill=c, width=w)
    d.ellipse(P(s, [(0.40, 0.40), (0.60, 0.60)]), outline=c, width=w)
    d.polygon(P(s, [(0.50, 0.41), (0.545, 0.50), (0.50, 0.59), (0.455, 0.50)]), fill=BLOOD)


def g_autorun(d, s, c):
    for off in (0.0, 0.20):
        d.polygon(P(s, [(0.28 + off, 0.32), (0.52 + off, 0.50), (0.28 + off, 0.68)]), fill=c)
    d.line(P(s, [(0.26, 0.80), (0.74, 0.80)]), fill=(c[0], c[1], c[2], 150), width=int(s * 0.025))


# ---------------- Shop art ----------------

def shop_base(size_ss):
    s = size_ss
    img = radial_gradient(s, (30, 22, 24), (6, 4, 5))
    add_grunge(img, 1600, 20)
    d = ImageDraw.Draw(img, "RGBA")
    w = int(s * 0.015)
    d.rectangle([w, w, s - w, s - w], outline=(10, 7, 8, 255), width=w)
    d.rectangle([w * 2.4, w * 2.4, s - w * 2.4, s - w * 2.4], outline=(110, 16, 20, 140), width=max(2, w // 3))
    return img


def finish_shop(base, glyph, out_path, size=512):
    img = Image.alpha_composite(base, glyph)
    img = img.resize((size, size), Image.LANCZOS)
    img.save(out_path)
    print("saved", os.path.relpath(out_path, ROOT))


def skull(d, s, cx, cy, r, c):
    d.ellipse(P(s, [(cx - r, cy - r), (cx + r, cy + r * 0.92)]), fill=c)
    d.rounded_rectangle(P(s, [(cx - r * 0.55, cy + r * 0.55), (cx + r * 0.55, cy + r * 1.25)]),
                        radius=s * r * 0.2, fill=c)
    er = r * 0.32
    for ex in (cx - r * 0.42, cx + r * 0.42):
        d.ellipse(P(s, [(ex - er, cy - r * 0.25), (ex + er, cy + r * 0.35)]), fill=(12, 8, 9, 255))
    d.polygon(P(s, [(cx, cy + r * 0.38), (cx - r * 0.14, cy + r * 0.68), (cx + r * 0.14, cy + r * 0.68)]),
              fill=(12, 8, 9, 255))
    for i in range(4):
        x = cx - r * 0.42 + i * r * 0.28
        d.line(P(s, [(x, cy + r * 0.8), (x, cy + r * 1.18)]), fill=(12, 8, 9, 255), width=max(2, int(s * 0.008)))


def a_skin_default(d, s, c):
    # pale clown face
    d.ellipse(P(s, [(0.26, 0.18), (0.74, 0.74)]), fill=c)
    for ex in (0.38, 0.56):
        d.line(P(s, [(ex, 0.34), (ex + 0.07, 0.44)]), fill=(14, 10, 11, 255), width=int(s * 0.022))
        d.line(P(s, [(ex + 0.07, 0.34), (ex, 0.44)]), fill=(14, 10, 11, 255), width=int(s * 0.022))
    d.ellipse(P(s, [(0.46, 0.50), (0.54, 0.58)]), fill=BLOOD)
    d.arc(P(s, [(0.34, 0.42), (0.66, 0.70)]), 20, 160, fill=(14, 10, 11, 255), width=int(s * 0.02))
    d.polygon(P(s, [(0.30, 0.72), (0.70, 0.72), (0.62, 0.88), (0.38, 0.88)]), fill=(26, 18, 20, 255))


def a_skin_butcher(d, s, c):
    # cleaver with blood
    blade = [(0.24, 0.30), (0.68, 0.24), (0.72, 0.52), (0.30, 0.56)]
    d.polygon(P(s, blade), fill=c)
    d.ellipse(P(s, [(0.30, 0.34), (0.36, 0.40)]), fill=(12, 8, 9, 255))
    d.polygon(P(s, [(0.66, 0.52), (0.74, 0.50), (0.84, 0.80), (0.76, 0.82)]), fill=(52, 34, 26, 255))
    for x, y in [(0.42, 0.56), (0.52, 0.55), (0.60, 0.54)]:
        d.polygon(P(s, [(x, y), (x + 0.03, y), (x + 0.015, y + 0.10)]), fill=BLOOD)
        d.ellipse(P(s, [(x + 0.002, y + 0.09), (x + 0.028, y + 0.12)]), fill=BLOOD)


def a_skin_wraith(d, s, c):
    pts = [(0.30, 0.78), (0.30, 0.40), (0.34, 0.26), (0.44, 0.18), (0.56, 0.18),
           (0.66, 0.26), (0.70, 0.40), (0.70, 0.78)]
    wave = []
    for i in range(9):
        x = 0.70 - i * 0.05
        y = 0.78 + (0.05 if i % 2 == 0 else -0.01)
        wave.append((x, y))
    d.polygon(P(s, pts + wave), fill=(208, 206, 214, 235))
    for ex in (0.42, 0.56):
        d.ellipse(P(s, [(ex - 0.035, 0.34), (ex + 0.035, 0.44)]), fill=(10, 8, 12, 255))
    d.ellipse(P(s, [(0.465, 0.48), (0.535, 0.58)]), fill=(10, 8, 12, 255))


def a_skin_plague(d, s, c):
    # plague doctor: dark hood + bone beak mask + glass eye
    d.ellipse(P(s, [(0.26, 0.16), (0.74, 0.66)]), fill=(44, 40, 38, 255))
    d.polygon(P(s, [(0.30, 0.52), (0.70, 0.52), (0.64, 0.86), (0.36, 0.86)]), fill=(36, 32, 30, 255))
    d.polygon(P(s, [(0.40, 0.34), (0.62, 0.38), (0.88, 0.66), (0.56, 0.60), (0.38, 0.52)]), fill=c)
    d.line(P(s, [(0.62, 0.44), (0.80, 0.62)]), fill=(150, 140, 128, 255), width=int(s * 0.008))
    d.ellipse(P(s, [(0.44, 0.36), (0.56, 0.48)]), fill=(20, 16, 16, 255))
    d.ellipse(P(s, [(0.455, 0.375), (0.545, 0.465)]), outline=(120, 20, 24, 255), width=int(s * 0.012))
    for i in range(3):
        x = 0.46 + i * 0.07
        d.ellipse(P(s, [(x, 0.53 + i * 0.035), (x + 0.018, 0.548 + i * 0.035)]), fill=(14, 12, 11, 255))


def torch(d, s, beam_rgba, body=(66, 62, 64, 255)):
    # horizontal flashlight, beam cone to the right
    d.polygon(P(s, [(0.52, 0.42), (0.92, 0.24), (0.92, 0.80), (0.52, 0.62)]), fill=beam_rgba)
    core = (beam_rgba[0], beam_rgba[1], beam_rgba[2], min(255, beam_rgba[3] + 70))
    d.polygon(P(s, [(0.52, 0.47), (0.92, 0.38), (0.92, 0.66), (0.52, 0.57)]), fill=core)
    d.rounded_rectangle(P(s, [(0.12, 0.455), (0.42, 0.565)]), radius=s * 0.02, fill=body)
    d.rectangle(P(s, [(0.30, 0.455), (0.335, 0.565)]), fill=(30, 26, 28, 255))
    d.polygon(P(s, [(0.42, 0.455), (0.52, 0.41), (0.52, 0.61), (0.42, 0.565)]), fill=(40, 36, 38, 255))
    d.rectangle(P(s, [(0.50, 0.41), (0.525, 0.61)]), fill=(24, 20, 22, 255))


def a_flash_default(d, s, c):
    torch(d, s, (255, 234, 180, 120))


def a_flash_cursed(d, s, c):
    torch(d, s, (120, 255, 150, 110))
    for x, y in [(0.24, 0.26), (0.34, 0.20), (0.30, 0.38)]:
        d.ellipse(P(s, [(x, y), (x + 0.025, y + 0.025)]), fill=(160, 255, 180, 220))


def a_flash_spectral(d, s, c):
    torch(d, s, (150, 200, 255, 110))
    d.arc(P(s, [(0.12, 0.10), (0.60, 0.58)]), 300, 60, fill=(190, 220, 255, 160), width=int(s * 0.012))


def a_item_talisman(d, s, c):
    d.line(P(s, [(0.50, 0.10), (0.50, 0.26)]), fill=(90, 70, 50, 255), width=int(s * 0.02))
    d.ellipse(P(s, [(0.26, 0.26), (0.74, 0.74)]), outline=(150, 120, 70, 255), width=int(s * 0.03))
    r = 0.195
    cx, cy = 0.5, 0.5
    star = []
    for i in range(5):
        ang = -math.pi / 2 + i * 4 * math.pi / 5
        star.append((cx + math.cos(ang) * r, cy + math.sin(ang) * r))
    d.line(P(s, star + [star[0]]), fill=BLOOD, width=int(s * 0.018), joint="curve")


def a_item_adrenaline(d, s, c):
    pts = [(0.34, 0.60), (0.58, 0.36), (0.70, 0.48), (0.46, 0.72)]
    d.polygon(P(s, pts), fill=(210, 206, 212, 230))
    d.polygon(P(s, [(0.37, 0.57), (0.50, 0.44), (0.60, 0.54), (0.47, 0.67)]), fill=(170, 30, 36, 220))
    d.line(P(s, [(0.70, 0.48), (0.80, 0.38)]), fill=(120, 118, 124, 255), width=int(s * 0.025))
    d.line(P(s, [(0.80, 0.38), (0.88, 0.30)]), fill=(200, 200, 206, 255), width=int(s * 0.012))
    d.line(P(s, [(0.30, 0.52), (0.42, 0.64)]), fill=(120, 118, 124, 255), width=int(s * 0.035))
    d.line(P(s, [(0.26, 0.56), (0.38, 0.68)]), fill=(120, 118, 124, 255), width=int(s * 0.02))


def a_item_charm(d, s, c):
    d.line(P(s, [(0.50, 0.08), (0.50, 0.22)]), fill=(90, 70, 50, 255), width=int(s * 0.02))
    d.ellipse(P(s, [(0.47, 0.20), (0.53, 0.26)]), outline=(150, 120, 70, 255), width=int(s * 0.012))
    skull(d, s, 0.50, 0.48, 0.20, (214, 206, 196, 255))


def a_ad(d, s, c):
    d.line(P(s, [(0.40, 0.28), (0.30, 0.12)]), fill=c, width=int(s * 0.018))
    d.line(P(s, [(0.56, 0.28), (0.68, 0.12)]), fill=c, width=int(s * 0.018))
    d.rounded_rectangle(P(s, [(0.20, 0.28), (0.80, 0.76)]), radius=s * 0.04,
                        fill=(36, 30, 32, 255), outline=c, width=int(s * 0.02))
    d.rectangle(P(s, [(0.26, 0.34), (0.68, 0.70)]), fill=(16, 12, 14, 255))
    d.polygon(P(s, [(0.42, 0.42), (0.58, 0.52), (0.42, 0.62)]), fill=BLOOD)
    d.ellipse(P(s, [(0.715, 0.38), (0.765, 0.43)]), fill=BLOOD)
    d.ellipse(P(s, [(0.715, 0.47), (0.765, 0.52)]), fill=c)
    d.line(P(s, [(0.34, 0.76), (0.30, 0.84)]), fill=c, width=int(s * 0.02))
    d.line(P(s, [(0.66, 0.76), (0.70, 0.84)]), fill=c, width=int(s * 0.02))


def main():
    ss_hud = 1024
    hud = {
        "hud_flashlight.png": g_flashlight,
        "hud_jump.png": g_jump,
        "hud_sprint.png": g_sprint,
        "hud_crouch.png": g_crouch,
        "hud_door.png": g_interact,
        "hud_chat.png": g_chat,
        "hud_settings.png": g_settings,
        "hud_view.png": g_view,
        "hud_autorun.png": g_autorun,
    }
    for name, fn in hud.items():
        base = hud_base(ss_hud)
        gl = glyph_layer(ss_hud, fn)
        finish_hud(base, gl, os.path.join(HUD_DIR, name))

    ss_shop = 1024
    shop = {
        "shop_skin_default.png": a_skin_default,
        "shop_skin_butcher.png": a_skin_butcher,
        "shop_skin_wraith.png": a_skin_wraith,
        "shop_skin_plague.png": a_skin_plague,
        "shop_flashlight.png": a_flash_default,
        "shop_flash_cursed.png": a_flash_cursed,
        "shop_flash_spectral.png": a_flash_spectral,
        "shop_item_talisman.png": a_item_talisman,
        "shop_item_adrenaline.png": a_item_adrenaline,
        "shop_item_charm.png": a_item_charm,
        "shop_ad.png": a_ad,
    }
    for name, fn in shop.items():
        base = shop_base(ss_shop)
        gl = glyph_layer(ss_shop, fn, glow_strength=0.8)
        finish_shop(base, gl, os.path.join(SHOP_DIR, name))


if __name__ == "__main__":
    main()
