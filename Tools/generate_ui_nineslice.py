"""Generate 9-slice friendly UI chrome PNGs (rounded square, stretchable center)."""
from __future__ import annotations

import struct
import zlib
from pathlib import Path

SIZE = 96
RADIUS = 20
OUTLINE = 3
BORDER = 24

COLORS = {
    "button_normal": ((20, 184, 166, 255), (11, 18, 32, 255)),
    "button_pressed": ((45, 212, 191, 255), (11, 18, 32, 255)),
    "button_disabled": ((51, 65, 85, 255), (71, 85, 105, 255)),
    "panel": ((51, 65, 85, 255), (11, 18, 32, 255)),
    "card_frame": ((15, 23, 42, 255), (249, 115, 22, 255)),
}


def inside_rounded(x: float, y: float, x0: float, y0: float, x1: float, y1: float, radius: float) -> bool:
    cx = min(max(x, x0 + radius), x1 - radius)
    cy = min(max(y, y0 + radius), y1 - radius)
    dx = x - cx
    dy = y - cy
    return (dx * dx) + (dy * dy) <= radius * radius


def draw_rounded(fill: tuple[int, int, int, int], outline: tuple[int, int, int, int]) -> bytearray:
    pixels = bytearray(SIZE * SIZE * 4)
    x0, y0, x1, y1 = 0.5, 0.5, SIZE - 0.5, SIZE - 0.5
    inner0 = x0 + OUTLINE
    inner1 = x1 - OUTLINE
    for y in range(SIZE):
        py = y + 0.5
        for x in range(SIZE):
            px = x + 0.5
            idx = (y * SIZE + x) * 4
            if not inside_rounded(px, py, x0, y0, x1, y1, RADIUS):
                pixels[idx : idx + 4] = b"\x00\x00\x00\x00"
                continue
            edge = not inside_rounded(px, py, inner0, inner0, inner1, inner1, max(0.0, RADIUS - OUTLINE))
            color = outline if edge else fill
            pixels[idx : idx + 4] = bytes(color)
    return pixels


def write_png(path: Path, pixels: bytearray) -> None:
    raw = b"".join(b"\x00" + pixels[y * SIZE * 4 : (y + 1) * SIZE * 4] for y in range(SIZE))

    def chunk(tag: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")
    path.write_bytes(png)


def main() -> None:
    out_dir = Path(__file__).resolve().parents[1] / "Assets" / "Art" / "Sprites" / "UI"
    out_dir.mkdir(parents=True, exist_ok=True)
    for name, colors in COLORS.items():
        write_png(out_dir / f"{name}.png", draw_rounded(*colors))
        print(f"wrote {name}.png border={BORDER}")


if __name__ == "__main__":
    main()
