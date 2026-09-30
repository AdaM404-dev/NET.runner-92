#!/usr/bin/env python3
"""Draw the warehouse floor plan as an SVG from the positions Unity reports.

Usage (the Unity editor must be open on this project, with MainTest loaded):

    bin/unity-inspect map > /tmp/map.txt
    python3 tools/inspect/floorplan.py /tmp/map.txt docs/img/warehouse-floorplan.svg

The picture uses Unity world coordinates as seen in the editor's top view:
+X to the right, +Z up. Two panels are drawn, one per level, because the
upper floors sit directly above ground-floor rooms.

Input lines look like `KIND|name|x|y|z|sizeX|sizeZ|...` (see tools/inspect/map.cs).
"""
import re
import sys
from html import escape

X0, X1, Z0, Z1 = -70.0, 70.0, -48.0, 48.0   # world extent of each panel, metres
K = 6.0                                      # pixels per metre
LEFT, RIGHT, TOP, GAP, BOTTOM = 56, 24, 96, 84, 60
PANEL_W, PANEL_H = (X1 - X0) * K, (Z1 - Z0) * K
WIDTH = LEFT + PANEL_W + RIGHT
HEIGHT = TOP + PANEL_H + GAP + PANEL_H + BOTTOM
UPPER_FROM = 3.0                             # objects centred above this height belong to the upper panel

# Colours: neutral chrome plus the first two categorical slots of the palette
# (blue, orange). Checked for colour-blind separation on both surfaces.
STYLE = """
text { font-family: system-ui, -apple-system, "Segoe UI", sans-serif; }
.surface { fill: #fcfcfb; }
.ink { fill: #0b0b0b; }
.ink2 { fill: #52514e; }
.muted { fill: #898781; }
.grid { stroke: #e1e0d9; stroke-width: 1; }
.frame { fill: none; stroke: #c3c2b7; stroke-width: 1; }
.zone { fill: #f0efec; stroke: #c3c2b7; stroke-width: 1; }
.deck { fill: #e1e0d9; stroke: #c3c2b7; stroke-width: 1; }
.ghost { fill: none; stroke: #e1e0d9; stroke-width: 1; }
.col { fill: #898781; }
.door { fill: #2a78d6; stroke: #fcfcfb; stroke-width: 2; }
.load { fill: #eb6834; stroke: #fcfcfb; stroke-width: 2; }
.spawn { fill: none; stroke: #0b0b0b; stroke-width: 2; stroke-linecap: round; }
.leader { fill: none; stroke: #898781; stroke-width: 1; }
.halo { paint-order: stroke; stroke: #fcfcfb; stroke-width: 3px; stroke-linejoin: round; }
.halo-zone { paint-order: stroke; stroke: #f0efec; stroke-width: 3px; stroke-linejoin: round; }
@media (prefers-color-scheme: dark) {
  .surface { fill: #1a1a19; }
  .ink { fill: #ffffff; }
  .ink2 { fill: #c3c2b7; }
  .muted { fill: #898781; }
  .grid { stroke: #2c2c2a; }
  .frame { stroke: #383835; }
  .zone { fill: #2c2c2a; stroke: #4a4a46; }
  .deck { fill: #383835; stroke: #4a4a46; }
  .ghost { stroke: #2c2c2a; }
  .col { fill: #898781; }
  .door { fill: #3987e5; stroke: #1a1a19; }
  .load { fill: #d95926; stroke: #1a1a19; }
  .spawn { stroke: #ffffff; }
  .leader { stroke: #898781; }
  .halo { stroke: #1a1a19; }
  .halo-zone { stroke: #2c2c2a; }
}
"""


def read(path):
    rows = []
    with open(path, encoding="utf-8") as handle:
        for line in handle:
            parts = line.rstrip("\n").split("|")
            if len(parts) < 7 or parts[0].startswith("="):
                continue
            rows.append({
                "kind": parts[0], "name": parts[1],
                "x": float(parts[2]), "y": float(parts[3]), "z": float(parts[4]),
                "sx": float(parts[5]), "sz": float(parts[6]),
                "loading": parts[0] == "DOOR" and parts[7] == "1",
            })
    return rows


def pretty(name):
    """FLOOR_NORTHEAST_ADMIN -> 'Northeast admin', FLOOR_Upper_NEA -> 'Upper NE A'."""
    text = name.split("_", 1)[1]
    text = re.sub(r"NE(?=[A-Z])", "NE ", text)
    text = re.sub(r"(?<=[a-z])(?=[A-Z])", " ", text).replace("_", " ")
    words = [w if w in ("NE", "A", "B") else w.lower() for w in text.split()]
    words[0] = words[0] if words[0] == "NE" else words[0].capitalize()
    return " ".join(words)


def text_width(text, size):
    return 0.56 * size * len(text)


class Panel:
    """One map. Shapes are emitted first, labels last, so text is never covered."""

    def __init__(self, top):
        self.top = top
        self.out = []        # shapes, in paint order
        self.labels = []     # text, painted on top
        self.taken = []      # boxes (x0, y0, x1, y1) that a label should not cover

    def px(self, x):
        return LEFT + (x - X0) * K

    def py(self, z):
        return self.top + (Z1 - z) * K

    def block(self, cx, cy, half_w, half_h):
        self.taken.append((cx - half_w, cy - half_h, cx + half_w, cy + half_h))

    def rect(self, row, css, title=None):
        w, h = max(row["sx"] * K, 2), max(row["sz"] * K, 2)
        x, y = self.px(row["x"]) - w / 2, self.py(row["z"]) - h / 2
        tip = f"<title>{escape(title)}</title>" if title else ""
        self.out.append(f'<rect class="{css}" x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}">{tip}</rect>')
        return x, y, w, h

    def label(self, x, y, text, css="ink2", size=11, anchor="middle", weight=None, halo=None):
        extra = f' font-weight="{weight}"' if weight else ""
        cls = f"{css} {halo}" if halo else css
        self.labels.append(f'<text class="{cls}" x="{x:.1f}" y="{y:.1f}" font-size="{size}" text-anchor="{anchor}"{extra}>{escape(text)}</text>')

    def hits(self, box):
        return sum(1 for t in self.taken if box[0] < t[2] and t[0] < box[2] and box[1] < t[3] and t[1] < box[3])

    def place(self, candidates, text, css, size, weight=None, halo="halo", keep=True):
        """Put centred text at the first candidate (cx, cy) that covers the fewest marks.

        Returns the chosen centre, or None when every candidate covers something
        and keep is False (the label is then left out; the tooltip still names it).
        """
        half_w, half_h = text_width(text, size) / 2 + 2, size / 2 + 2
        best = None
        for order, (cx, cy) in enumerate(candidates):
            score = (self.hits((cx - half_w, cy - half_h, cx + half_w, cy + half_h)), order)
            if best is None or score < best[0]:
                best = (score, cx, cy)
        if best is None or (best[0][0] > 0 and not keep):
            return None
        _, cx, cy = best
        self.label(cx, cy + size * 0.35, text, css, size, "middle", weight, halo)
        self.block(cx, cy, half_w, half_h)
        return cx, cy

    def chrome(self, title, subtitle):
        head = lambda x, y, text, css, size, anchor, weight=None: self.out.append(
            f'<text class="{css}" x="{x:.1f}" y="{y:.1f}" font-size="{size}" text-anchor="{anchor}"'
            + (f' font-weight="{weight}"' if weight else "") + f'>{escape(text)}</text>')
        head(LEFT, self.top - 30, title, "ink", 14, "start", 600)
        head(LEFT, self.top - 13, subtitle, "ink2", 11, "start")
        step = 10
        x = int(X0 // step * step)
        while x <= X1:
            if X0 < x < X1:
                self.out.append(f'<line class="grid" x1="{self.px(x):.1f}" y1="{self.top}" x2="{self.px(x):.1f}" y2="{self.top + PANEL_H}"/>')
            head(self.px(x), self.top + PANEL_H + 14, str(x), "muted", 10, "middle")
            x += step
        z = int(Z0 // step * step)
        while z <= Z1:
            if Z0 <= z <= Z1:
                if Z0 < z < Z1:
                    self.out.append(f'<line class="grid" x1="{LEFT}" y1="{self.py(z):.1f}" x2="{LEFT + PANEL_W}" y2="{self.py(z):.1f}"/>')
                head(LEFT - 8, self.py(z) + 3.5, str(z), "muted", 10, "end")
            z += step
        self.out.append(f'<rect class="frame" x="{LEFT}" y="{self.top}" width="{PANEL_W}" height="{PANEL_H}"/>')
        head(LEFT + PANEL_W, self.top + PANEL_H + 30, "x (m)", "muted", 10, "end")
        head(LEFT - 8, self.top - 6, "z (m)", "muted", 10, "end")

    def zone_shape(self, row, css="zone"):
        return self.rect(row, css, f'{row["name"]}  {row["sx"]:.0f} x {row["sz"]:.0f} m, floor at y = {row["y"] + 0.15:.1f}')

    def zone_label(self, row, box):
        x, y, w, h = box
        name = pretty(row["name"])
        dims = f'{row["name"]} · {row["sx"]:.0f} × {row["sz"]:.0f} m'
        fractions = [0.5, 0.4, 0.6, 0.3, 0.7, 0.2, 0.8, 0.12, 0.88]
        for size in (12, 10):
            tw = text_width(name, size)
            if tw <= w - 10 and h >= size + 8:
                # Candidate centres on a grid inside the zone, nearest the middle first.
                inset_x, inset_y = tw / 2 + 5, size
                cands = [(min(max(x + w * fx, x + inset_x), x + w - inset_x),
                          min(max(y + h * fy, y + inset_y), y + h - inset_y))
                         for fy in fractions for fx in fractions[:5]]
                spot = self.place(cands, name, "ink2", size, 600, "halo-zone")
                if spot and size == 12 and text_width(dims, 9) < w - 10 and spot[1] + 26 < y + h:
                    self.place([(spot[0], spot[1] + 16)], dims, "muted", 9, None, "halo-zone", keep=False)
                return
        # Tall, narrow zone: write the name bottom-to-top.
        size = 10
        tw = text_width(name, size)
        if w >= size + 8 and tw <= h - 10:
            cx = x + w / 2
            best = None
            for order, fy in enumerate(fractions):
                cy = min(max(y + h * fy, y + tw / 2 + 5), y + h - tw / 2 - 5)
                score = (self.hits((cx - size / 2 - 2, cy - tw / 2 - 2, cx + size / 2 + 2, cy + tw / 2 + 2)), order)
                if best is None or score < best[0]:
                    best = (score, cy)
            cy = best[1]
            self.labels.append(f'<text class="ink2 halo-zone" x="{cx:.1f}" y="{cy:.1f}" font-size="{size}" font-weight="600" text-anchor="middle" '
                               f'transform="rotate(-90 {cx:.1f} {cy:.1f})" dy="3.5">{escape(name)}</text>')
            self.block(cx, cy, size / 2 + 2, tw / 2 + 2)

    def door(self, row):
        tip = f'{row["name"]}  x = {row["x"]:.1f}, z = {row["z"]:.1f}, height {row["y"]:.1f}'
        cx, cy = self.px(row["x"]), self.py(row["z"])
        if row["loading"]:
            w, h = max(row["sx"] * K, 8), max(row["sz"] * K, 8)
            self.out.append(f'<rect class="load" x="{cx - w / 2:.1f}" y="{cy - h / 2:.1f}" width="{w:.1f}" height="{h:.1f}" rx="3"><title>{escape(tip)}</title></rect>')
            self.block(cx, cy, w / 2 + 1, h / 2 + 1)
        else:
            self.out.append(f'<circle class="door" cx="{cx:.1f}" cy="{cy:.1f}" r="4.5"><title>{escape(tip)}</title></circle>')
            self.block(cx, cy, 5.5, 5.5)


def build(rows):
    ground, upper = Panel(TOP), Panel(TOP + PANEL_H + GAP)
    kinds = lambda *names: [r for r in rows if r["kind"] in names]
    doors = kinds("DOOR")
    low_doors = [d for d in doors if d["y"] < UPPER_FROM]
    high_doors = [d for d in doors if d["y"] >= UPPER_FROM]

    ground.chrome("Ground level", f"Floors at y = 0 · {len(low_doors)} doors · player spawn in the main hall")
    upper.chrome("Upper level", f"Office floors at y = 3.5, catwalks and mezzanines at y = 4.4 · {len(high_doors)} doors")

    # --- shapes -----------------------------------------------------------
    floors = sorted(kinds("FLOOR"), key=lambda r: -r["sx"] * r["sz"])
    zone_boxes = []
    for row in floors:
        if row["y"] < UPPER_FROM:
            zone_boxes.append((ground, row, ground.zone_shape(row)))
            upper.rect(row, "ghost")
    for row in floors:
        if row["y"] >= UPPER_FROM:
            zone_boxes.append((upper, row, upper.zone_shape(row)))

    for row in kinds("CATWALK", "MEZZ"):
        upper.rect(row, "deck", f'{row["name"]}  walkway at y = {row["y"]:.1f}')
    for row in kinds("LANDING"):
        (upper if row["y"] >= UPPER_FROM else ground).rect(row, "deck", row["name"])
    for row in kinds("DOCK"):
        ground.rect(row, "deck", row["name"])
    for row in kinds("SHAFT"):
        for panel in (ground, upper):
            panel.rect(row, "deck", row["name"].replace("_Cap", "") + "  vertical shaft")
    for row in kinds("STAIR"):
        panel = upper if row["y"] >= UPPER_FROM else ground
        x, y, w, h = panel.rect(row, "deck", row["name"])
        panel.block(x + w / 2, y + h / 2, w / 2, h / 2)
    for row in kinds("COLUMN"):
        for panel in (ground, upper):
            cx, cy = panel.px(row["x"]), panel.py(row["z"])
            panel.out.append(f'<rect class="col" x="{cx - 2:.1f}" y="{cy - 2:.1f}" width="4" height="4"/>')
            panel.block(cx, cy, 3, 3)

    # --- data marks ---------------------------------------------------------
    for row in low_doors:
        ground.door(row)
    for row in high_doors:
        upper.door(row)

    # Player spawn, read from the scene: NEXUS_Player stands at (0, 12).
    sx, sy = ground.px(0), ground.py(12)
    ground.out.append(f'<path class="spawn" d="M{sx - 6:.1f} {sy:.1f}H{sx + 6:.1f}M{sx:.1f} {sy - 6:.1f}V{sy + 6:.1f}"><title>NEXUS_Player spawn  x = 0, z = 12</title></path>')
    ground.block(sx, sy, 8, 8)

    # --- labels (painted last) ---------------------------------------------
    spawn_text = "Player spawn (0, 12)"
    tx = sx + 24 + text_width(spawn_text, 11) / 2
    ground.out.append(f'<path class="leader" d="M{sx + 8:.1f} {sy + 8:.1f}L{sx + 20:.1f} {sy + 19:.1f}"/>')
    ground.label(sx + 24, sy + 26, spawn_text, "ink", 11, "start", 600, "halo-zone")
    ground.block(tx, sy + 22, text_width(spawn_text, 11) / 2 + 4, 9)

    entry = next((d for d in low_doors if d["name"] == "DOOR_Entry_Main"), None)
    if entry:
        ex, ey = ground.px(entry["x"]), ground.py(entry["z"])
        ground.out.append(f'<path class="leader" d="M{ex + 6:.1f} {ey - 5:.1f}L{ex + 22:.1f} {ey - 16:.1f}"/>')
        ground.label(ex + 25, ey - 15, "DOOR_Entry_Main", "ink2", 10, "start", None, "halo")
        ground.block(ex + 25 + text_width("DOOR_Entry_Main", 10) / 2, ey - 18, text_width("DOOR_Entry_Main", 10) / 2 + 3, 8)

    for panel, row, box in zone_boxes:
        panel.zone_label(row, box)

    def beside(panel, row, text, halo="halo-zone"):
        """A small note next to a shape: try right, left, below, above."""
        cx, cy = panel.px(row["x"]), panel.py(row["z"])
        half_w, half_h = row["sx"] * K / 2, row["sz"] * K / 2
        tw = text_width(text, 10) / 2 + 5
        cands = [(cx + half_w + tw, cy), (cx - half_w - tw, cy), (cx, cy + half_h + 9), (cx, cy - half_h - 9)]
        panel.place(cands, text, "muted", 10, None, halo, keep=False)

    docks = kinds("DOCK")
    for group in (docks[:3], docks[3:]):
        if group:
            gx = sum(d["x"] for d in group) / len(group)
            gz = max(d["z"] + d["sz"] / 2 for d in group)
            ground.place([(ground.px(gx), ground.py(gz) - 9)], "loading docks", "muted", 10, None, "halo")
    for row in kinds("STAIR"):
        if 1.0 < row["y"] < UPPER_FROM:
            beside(ground, row, "stairs up")
    for row in kinds("SHAFT"):
        beside(ground, row, "shaft")
        beside(upper, row, "shaft", "halo")
    for row in kinds("MEZZ"):
        beside(upper, row, "mezzanine", "halo")
    for row in kinds("CATWALK"):
        if row["sx"] > row["sz"] and row["z"] < 0:
            upper.place([(upper.px(row["x"] + dx), upper.py(row["z"]) - 12) for dx in (0, -12, 12, -22, 22)],
                        "catwalk around the main hall", "muted", 10, None, "halo", keep=False)

    swing = sum(1 for d in doors if not d["loading"])
    loading = sum(1 for d in doors if d["loading"])
    head = []
    head.append(f'<text class="ink" x="{LEFT}" y="26" font-size="17" font-weight="600">Warehouse floor plan (scene MainTest)</text>')
    head.append(f'<text class="ink2" x="{LEFT}" y="44" font-size="11">Unity world coordinates in metres, as in the editor\'s top view. '
                'Object names use compass words: north is −Z (down), south +Z (up), east −X (left), west +X (right).</text>')
    lx = WIDTH - RIGHT - 330
    head.append(f'<circle class="door" cx="{lx}" cy="22" r="4.5"/><text class="ink2" x="{lx + 10}" y="26" font-size="11">Swing door ({swing})</text>')
    head.append(f'<rect class="load" x="{lx + 104}" y="18" width="22" height="8" rx="3"/><text class="ink2" x="{lx + 132}" y="26" font-size="11">Loading door ({loading})</text>')
    head.append(f'<rect class="col" x="{lx + 246}" y="20" width="4" height="4"/><text class="ink2" x="{lx + 256}" y="26" font-size="11">Column ({len(kinds("COLUMN"))})</text>')
    foot = (f'<text class="muted" x="{LEFT}" y="{HEIGHT - 14}" font-size="9">Generated by tools/inspect/floorplan.py from `bin/unity-inspect map`. '
            'Open the file in a browser and hover a mark to see its object name. Grid lines every 10 m.</text>')

    body = "\n".join(head + ground.out + upper.out + ground.labels + upper.labels + [foot])
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {WIDTH:.0f} {HEIGHT:.0f}" width="{WIDTH:.0f}" height="{HEIGHT:.0f}" role="img" '
            f'aria-label="Floor plan of the warehouse level on two levels with doors, zones and the player spawn">\n'
            f'<style>{STYLE}</style>\n<rect class="surface" width="100%" height="100%"/>\n{body}\n</svg>\n')


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    rows = read(sys.argv[1])
    if not rows:
        sys.exit("no map rows found in " + sys.argv[1])
    with open(sys.argv[2], "w", encoding="utf-8") as handle:
        handle.write(build(rows))
    print(f"{sys.argv[2]}: {len(rows)} objects drawn")


if __name__ == "__main__":
    main()
