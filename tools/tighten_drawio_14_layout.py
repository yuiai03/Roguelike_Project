from __future__ import annotations

import shutil
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DRAWIO_DIR = ROOT / "output" / "diagrams" / "drawio"
BACKUP_DIR = ROOT / "tmp" / "drawio_layout_before_compact_1_4"


LAYOUTS: dict[str, dict[str, tuple[int, int, int, int]]] = {
    "1_4_1_2_bat_dau_tran_dau.drawio": {
        "a1": (80, 250, 120, 140),
        "n1": (250, 280, 170, 80),
        "n2": (500, 280, 165, 80),
        "n3": (750, 280, 170, 80),
        "n4": (995, 180, 160, 80),
        "n5": (990, 320, 180, 80),
    },
    "1_4_1_3_player_controller.drawio": {
        "n1": (60, 300, 230, 80),
        "n2": (340, 300, 180, 80),
        "n3": (350, 150, 180, 80),
        "n4": (620, 210, 220, 80),
        "n5": (620, 390, 220, 80),
        "n6": (970, 210, 300, 80),
        "n7": (340, 490, 240, 90),
    },
    "1_4_1_4_chien_dau_player.drawio": {
        "n1": (80, 300, 160, 80),
        "n2": (300, 300, 180, 80),
        "n3": (540, 300, 190, 80),
        "n4": (790, 210, 220, 90),
        "n5": (800, 400, 180, 80),
        "n6": (1080, 300, 180, 80),
    },
    "1_4_1_5_enemy_system.drawio": {
        "n1": (80, 290, 160, 80),
        "n2": (290, 270, 190, 90),
        "n3": (540, 270, 190, 90),
        "n4": (800, 110, 160, 80),
        "n5": (800, 240, 160, 80),
        "n6": (800, 370, 160, 80),
        "n7": (800, 500, 160, 80),
        "n8": (1030, 270, 200, 90),
    },
    "1_4_1_6_wave_va_do_kho.drawio": {
        "n1": (80, 290, 190, 100),
        "n2": (330, 290, 170, 100),
        "n3": (580, 180, 150, 80),
        "n4": (580, 390, 150, 80),
        "n5": (840, 180, 190, 90),
        "n6": (1080, 290, 160, 80),
        "n7": (840, 400, 180, 90),
    },
    "1_4_1_7_theme_ban_do.drawio": {
        "n1": (70, 300, 150, 80),
        "n2": (280, 300, 170, 80),
        "n3": (510, 290, 200, 90),
        "n4": (780, 180, 190, 80),
        "n5": (780, 390, 190, 80),
        "n6": (1040, 290, 210, 80),
    },
    "1_4_1_8_exp_va_len_cap.drawio": {
        "n1": (70, 300, 160, 80),
        "n2": (290, 300, 165, 80),
        "n3": (515, 300, 180, 80),
        "n4": (770, 180, 180, 80),
        "n5": (770, 390, 170, 80),
        "n6": (1000, 390, 160, 80),
        "n7": (995, 180, 180, 80),
    },
    "1_4_1_9_buff_va_tang_suc_manh.drawio": {
        "n1": (80, 300, 160, 80),
        "n2": (300, 300, 170, 80),
        "n3": (300, 160, 190, 80),
        "n4": (560, 300, 180, 80),
        "n5": (810, 300, 150, 80),
        "n6": (1030, 190, 180, 80),
        "n7": (1030, 390, 210, 80),
    },
    "1_4_1_10_ui_trong_tran.drawio": {
        "n1": (520, 250, 180, 80),
        "n2": (150, 120, 190, 80),
        "n3": (150, 250, 170, 80),
        "n4": (150, 380, 190, 80),
        "n5": (860, 120, 180, 80),
        "n6": (860, 250, 180, 80),
        "n7": (860, 380, 180, 80),
        "n8": (520, 430, 190, 80),
    },
    "1_4_1_11_pause_va_ket_thuc_tran.drawio": {
        "n1": (80, 190, 140, 80),
        "n2": (260, 190, 170, 80),
        "n3": (510, 110, 200, 80),
        "n4": (510, 250, 200, 90),
        "n5": (790, 190, 170, 80),
        "n6": (1030, 110, 210, 90),
        "n7": (1030, 250, 210, 90),
    },
    "1_4_1_12_leaderboard.drawio": {
        "n1": (80, 300, 220, 80),
        "n2": (340, 290, 350, 90),
        "n3": (740, 290, 220, 90),
        "n4": (1020, 180, 300, 90),
        "n5": (1020, 390, 300, 90),
        "n6": (1380, 290, 320, 90),
    },
}


def backup_current_files() -> None:
    BACKUP_DIR.mkdir(parents=True, exist_ok=True)
    for filename in LAYOUTS:
        shutil.copy2(DRAWIO_DIR / filename, BACKUP_DIR / filename)


def tighten_file(path: Path, layout: dict[str, tuple[int, int, int, int]]) -> None:
    tree = ET.parse(path)
    root = tree.getroot()
    graph = root.find(".//mxGraphModel")
    if graph is None:
        raise ValueError(f"mxGraphModel not found in {path.name}")

    max_right = 0
    max_bottom = 0

    for cell in root.findall(".//mxCell"):
        if cell.attrib.get("vertex") != "1":
            continue
        node_id = cell.attrib.get("id")
        if node_id not in layout:
            continue
        x, y, w, h = layout[node_id]
        geom = cell.find("mxGeometry")
        if geom is None:
            continue
        geom.attrib["x"] = str(x)
        geom.attrib["y"] = str(y)
        geom.attrib["width"] = str(w)
        geom.attrib["height"] = str(h)
        max_right = max(max_right, x + w)
        max_bottom = max(max_bottom, y + h)

    page_width = max_right + 120
    page_height = max_bottom + 100
    graph.attrib["pageWidth"] = str(page_width)
    graph.attrib["pageHeight"] = str(page_height)
    graph.attrib["dx"] = str(page_width)
    graph.attrib["dy"] = str(page_height)

    tree.write(path, encoding="utf-8", xml_declaration=False)


def main() -> None:
    backup_current_files()
    for filename, layout in LAYOUTS.items():
        tighten_file(DRAWIO_DIR / filename, layout)
    print(f"Updated {len(LAYOUTS)} drawio files")
    print(f"Backup saved to {BACKUP_DIR}")


if __name__ == "__main__":
    main()
