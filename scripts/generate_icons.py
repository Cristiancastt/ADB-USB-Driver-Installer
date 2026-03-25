#!/usr/bin/env python3
"""Generate app icons for Windows, Linux, and macOS from a single SVG.

Usage:
  python scripts/generate_icons.py --input Logo.svg --output CLI/Assets/icons
"""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path


PNG_SIZES = [16, 24, 32, 48, 64, 128, 256, 512, 1024]
ICO_SIZES = [16, 24, 32, 48, 64, 128, 256]
ICONSET_SIZES = [16, 32, 64, 128, 256, 512]


def run(cmd: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(cmd, check=False, capture_output=True, text=True)


def has(cmd: str) -> bool:
    return shutil.which(cmd) is not None


def render_png(svg: Path, png: Path, size: int) -> bool:
    # 1) CairoSVG Python package
    try:
        import cairosvg  # type: ignore

        cairosvg.svg2png(
            url=str(svg),
            write_to=str(png),
            output_width=size,
            output_height=size,
        )
        print(f"[OK] CairoSVG rendered {png.name}")
        return True
    except Exception:
        import traceback
        print(f"[FAIL] CairoSVG failed for size {size}")
        traceback.print_exc()

    # 2) Inkscape CLI
    if has("inkscape"):
        res = run([
            "inkscape",
            str(svg),
            "--export-type=png",
            f"--export-filename={png}",
            "--export-width",
            str(size),
            "--export-height",
            str(size),
        ])
        if res.returncode == 0:
            print(f"[OK] Inkscape rendered {png.name}")
            return True
        else:
            print(f"[FAIL] Inkscape failed: {res.stderr}")

    # 2.5) svglib + reportlab
    try:
        from svglib.svglib import svg2rlg  # type: ignore
        from reportlab.graphics import renderPM  # type: ignore

        drawing = svg2rlg(str(svg))
        if drawing is not None:
            width = float(getattr(drawing, "width", 0.0) or 0.0)
            height = float(getattr(drawing, "height", 0.0) or 0.0)
            if width > 0 and height > 0:
                sx = size / width
                sy = size / height
                drawing.scale(sx, sy)
                drawing.width = size
                drawing.height = size
            renderPM.drawToFile(drawing, str(png), fmt="PNG")
            print(f"[OK] svglib/reportlab rendered {png.name}")
            return True
    except Exception:
        import traceback
        print(f"[FAIL] svglib/reportlab failed for size {size}")
        traceback.print_exc()

    # 3) rsvg-convert
    if has("rsvg-convert"):
        res = run([
            "rsvg-convert",
            "-w",
            str(size),
            "-h",
            str(size),
            "-o",
            str(png),
            str(svg),
        ])
        if res.returncode == 0:
            print(f"[OK] rsvg-convert rendered {png.name}")
            return True
        else:
            print(f"[FAIL] rsvg-convert failed: {res.stderr}")

    # 4) ImageMagick
    if has("magick"):
        res = run(["magick", str(svg), "-resize", f"{size}x{size}", str(png)])
        if res.returncode == 0:
            print(f"[OK] ImageMagick rendered {png.name}")
            return True
        else:
            print(f"[FAIL] ImageMagick failed: {res.stderr}")

    return False

def generate_png_set(svg: Path, out_dir: Path) -> list[Path]:
    pngs: list[Path] = []
    for size in PNG_SIZES:
        target = out_dir / f"icon_{size}x{size}.png"
        if not render_png(svg, target, size):
            raise RuntimeError(
                "Could not render SVG to PNG. Install one of: "
                "cairosvg, inkscape, librsvg (rsvg-convert), or ImageMagick."
            )
        pngs.append(target)
    return pngs


def generate_ico(out_dir: Path) -> bool:
    # Prefer Pillow for deterministic multi-size ico file
    try:
        from PIL import Image  # type: ignore

        largest = out_dir / "icon_1024x1024.png"
        img = Image.open(largest).convert("RGBA")
        ico = out_dir / "adb-installer.ico"
        img.save(str(ico), sizes=[(s, s) for s in ICO_SIZES])
        return True
    except Exception:
        pass

    # Fallback to ImageMagick
    if has("magick"):
        ico = out_dir / "adb-installer.ico"
        inputs = [str(out_dir / f"icon_{s}x{s}.png") for s in ICO_SIZES]
        res = run(["magick", *inputs, str(ico)])
        return res.returncode == 0

    return False


def generate_icns(out_dir: Path) -> bool:
    # iconutil exists on macOS runners and local macOS machines.
    if not has("iconutil"):
        return False

    with tempfile.TemporaryDirectory() as tmp:
        iconset = Path(tmp) / "adb-installer.iconset"
        iconset.mkdir(parents=True, exist_ok=True)

        for size in ICONSET_SIZES:
            src = out_dir / f"icon_{size}x{size}.png"
            dst = iconset / f"icon_{size}x{size}.png"
            shutil.copy2(src, dst)

            retina_src = out_dir / f"icon_{size * 2}x{size * 2}.png"
            retina_dst = iconset / f"icon_{size}x{size}@2x.png"
            if retina_src.exists():
                shutil.copy2(retina_src, retina_dst)

        icns = out_dir / "adb-installer.icns"
        res = run(["iconutil", "-c", "icns", str(iconset), "-o", str(icns)])
        return res.returncode == 0


def write_readme(out_dir: Path) -> None:
    text = """# Generated Icons

This directory is generated from Logo.svg by scripts/generate_icons.py.

Outputs:
- adb-installer.ico (Windows executable icon)
- adb-installer.icns (macOS app icon, generated when iconutil is available)
- icon_16x16.png .. icon_1024x1024.png (Linux/macOS assets)
"""
    (out_dir / "README.txt").write_text(text, encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate multi-platform icons from an SVG file")
    parser.add_argument("--input", default="Logo.svg", help="Path to source SVG")
    parser.add_argument("--output", default="CLI/Assets/icons", help="Output directory")
    args = parser.parse_args()

    src = Path(args.input).resolve()
    out_dir = Path(args.output).resolve()

    if not src.exists():
        print(f"ERROR: SVG not found: {src}", file=sys.stderr)
        return 2

    out_dir.mkdir(parents=True, exist_ok=True)

    try:
        generate_png_set(src, out_dir)
        ico_ok = generate_ico(out_dir)
        icns_ok = generate_icns(out_dir)
        write_readme(out_dir)
    except Exception as ex:
        print(f"ERROR: {ex}", file=sys.stderr)
        return 3

    print(f"Generated PNG set in: {out_dir}")
    print(f"Windows icon (.ico): {'OK' if ico_ok else 'SKIPPED'}")
    print(f"macOS icon (.icns): {'OK' if icns_ok else 'SKIPPED'}")

    if not ico_ok:
        print(
            "NOTE: Install Pillow (pip install pillow) or ImageMagick to generate .ico automatically.",
            file=sys.stderr,
        )

    if not icns_ok:
        print(
            "NOTE: .icns generation requires macOS iconutil (or manual conversion).",
            file=sys.stderr,
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
