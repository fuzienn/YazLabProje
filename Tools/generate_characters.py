"""Karakter sprite sayfalarini uretir (16x16 kare, 3 satir x 3 sutun).

Satirlar: asagi, yukari, yan (sag). Sutunlar: durus, yurume-1, yurume-2.
Sola bakis Unity'de SpriteRenderer.flipX ile elde edilir.
Kullanim: python3 Tools/generate_characters.py <cikti_klasoru>
"""
import struct
import sys
import zlib
from pathlib import Path

TILE = 16

OUTLINE = (26, 20, 35)

PLAYER_PALETTE = {
    "o": OUTLINE, "e": OUTLINE,
    "s": (224, 164, 122), "S": (184, 119, 90),
    "h": (90, 58, 36), "H": (59, 36, 22),
    "t": (201, 180, 138), "T": (156, 134, 96),
    "p": (74, 68, 88), "P": (51, 47, 64),
    "b": (59, 42, 32),
}

GUARD_PALETTE = {
    "o": OUTLINE, "e": OUTLINE,
    "s": (224, 164, 122), "S": (184, 119, 90),
    "m": (138, 147, 168), "M": (92, 99, 120),
    "a": (107, 122, 153), "A": (74, 85, 112),
    "r": (140, 47, 57), "R": (94, 31, 39),
    "b": (59, 42, 32), "k": (140, 47, 57),
}

# Gardiyan: koyu zirh, altin arma, kirmizi sorguc
WARDEN_PALETTE = dict(GUARD_PALETTE)
WARDEN_PALETTE.update({
    "m": (72, 64, 92), "M": (48, 42, 64),
    "a": (61, 53, 80), "A": (40, 34, 54),
    "r": (212, 166, 64), "R": (160, 118, 40),
    "k": (176, 40, 48),
})

RAT_PALETTE = {
    "o": OUTLINE, "e": OUTLINE,
    "g": (138, 127, 134), "G": (95, 85, 96),
    "k": (217, 138, 154),
}

PLAYER = {
    "down": [
        "..........",
        "..oooooo..",
        ".ohhhhhho.",
        "ohhhhhhhho",
        "ohhsssshho",
        "osesssseso",
        "osssssssso",
        ".oSssssSo.",
        "otttttttto",
        "stttttttts",
        "sTttttttTs",
        ".obbbbbbo.",
        ".oppooppo.",
        ".oPPooPPo.",
        ".obboobbo.",
        "..........",
    ],
    "up": [
        "..........",
        "..oooooo..",
        ".ohhhhhho.",
        "ohhhhhhhho",
        "ohhhhhhhho",
        "ohhhhhhhho",
        "oHhhhhhhHo",
        ".oHHHHHHo.",
        "otttttttto",
        "stttttttts",
        "sTttttttTs",
        ".obbbbbbo.",
        ".oppooppo.",
        ".oPPooPPo.",
        ".obboobbo.",
        "..........",
    ],
    "side": [
        "..........",
        "..ooooo...",
        ".ohhhhhho.",
        "ohhhhhhhho",
        "ohhhssssso",
        "ohhsssseso",
        "ohssssssso",
        ".oSSSSSSo.",
        "..ottttto.",
        "..otttsto.",
        "..oTTTsTo.",
        "..obbbbbo.",
        "..oppppo..",
        "..oPPPPo..",
        "..obbbbbo.",
        "..........",
    ],
}

GUARD = {
    "down": [
        "..........",
        "..oooooo..",
        ".ommmmmmo.",
        "ommmmmmmmo",
        "oMmmmmmmMo",
        "oMsessesMo",
        "oMssssssMo",
        ".oMSSSSMo.",
        "oaarrrraao",
        "AaarrrraaA",
        "sAarRRraAs",
        ".obbbbbbo.",
        ".oAAooAAo.",
        ".oAAooAAo.",
        ".obboobbo.",
        "..........",
    ],
    "up": [
        "..........",
        "..oooooo..",
        ".ommmmmmo.",
        "ommmmmmmmo",
        "oMmmmmmmMo",
        "oMmmmmmmMo",
        "oMmmmmmmMo",
        ".oMMMMMMo.",
        "oaaaaaaaao",
        "AaaaaaaaaA",
        "sAaaaaaaAs",
        ".obbbbbbo.",
        ".oAAooAAo.",
        ".oAAooAAo.",
        ".obboobbo.",
        "..........",
    ],
    "side": [
        "..........",
        "..ooooo...",
        ".ommmmmo..",
        "ommmmmmmo.",
        "oMmmmmmmmo",
        "oMMmssseso",
        "oMMmssssso",
        ".oMMSSSSo.",
        "..oarrrao.",
        "..oarrsao.",
        "..oARRsAo.",
        "..obbbbbo.",
        "..oAAAAo..",
        "..oAAAAo..",
        "..obbbbbo.",
        "..........",
    ],
}

RAT_SIDE = [
    "............",
    "............",
    "............",
    "............",
    "............",
    "............",
    "............",
    "............",
    "............",
    ".......oo...",
    "..ooooogko..",
    ".oggggggggeo",
    "oggggggggggk",
    "kGggggggggGo",
    "k.oGo.oGo...",
    ".k..........",
]


def pad(rows, width=TILE):
    """Satirlari ortalayarak 16 genislige tamamlar."""
    out = []
    for row in rows:
        left = (width - len(row)) // 2
        out.append("." * left + row + "." * (width - len(row) - left))
    return out


def lift_leg(rows, cols):
    """Yurume karesi: verilen sutunlardaki bacak bir piksel yukari kalkar."""
    grid = [list(r) for r in rows]
    for c in cols:
        for r in range(12, 15):
            grid[r][c] = grid[r + 1][c]
    return ["".join(r) for r in grid]


def side_stride(rows):
    """Yandan yurume karesi: bacaklar acilir."""
    rows = list(rows)
    rows[13] = ".oPPooPPo." if "P" in rows[13] else ".oAAooAAo."
    rows[14] = ".obbo.obbo"
    return rows


def humanoid_frames(sheet):
    frames = {}
    for direction in ("down", "up"):
        base = sheet[direction]
        frames[direction] = [base, lift_leg(base, range(1, 5)), lift_leg(base, range(5, 9))]
    side = sheet["side"]
    frames["side"] = [side, side_stride(side), side]
    return frames


def rat_frames():
    walk = list(RAT_SIDE)
    walk[14] = "k..oGooGo..."
    return {d: [RAT_SIDE, walk, RAT_SIDE] for d in ("down", "up", "side")}


def write_png(path, pixels, width, height):
    raw = b"".join(
        b"\x00" + b"".join(bytes(pixels[y][x]) for x in range(width)) for y in range(height)
    )

    def chunk(tag, data):
        body = tag + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    header = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    path.write_bytes(png)


def render_sheet(frames, palette, extra=None):
    size = TILE * 3
    pixels = [[(0, 0, 0, 0)] * size for _ in range(size)]
    for row_index, direction in enumerate(("down", "up", "side")):
        for col_index, frame in enumerate(frames[direction]):
            rows = pad(frame)
            if extra:
                rows = extra(rows, direction)
            for y, line in enumerate(rows):
                for x, ch in enumerate(line):
                    if ch == ".":
                        continue
                    r, g, b = palette[ch]
                    pixels[row_index * TILE + y][col_index * TILE + x] = (r, g, b, 255)
    return pixels


def warden_plume(rows, direction):
    rows = list(rows)
    rows[0] = rows[0][:7] + "kk" + rows[0][9:]
    return rows


def main():
    out = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    out.mkdir(parents=True, exist_ok=True)
    sheets = {
        "player": render_sheet(humanoid_frames(PLAYER), PLAYER_PALETTE),
        "guard": render_sheet(humanoid_frames(GUARD), GUARD_PALETTE),
        "warden": render_sheet(humanoid_frames(GUARD), WARDEN_PALETTE, warden_plume),
        "rat": render_sheet(rat_frames(), RAT_PALETTE),
    }
    for name, pixels in sheets.items():
        write_png(out / f"{name}.png", pixels, TILE * 3, TILE * 3)
        print(f"{name}.png")


if __name__ == "__main__":
    main()
