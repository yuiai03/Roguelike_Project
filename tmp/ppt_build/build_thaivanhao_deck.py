from __future__ import annotations

from pathlib import Path
from zipfile import ZipFile

from PIL import Image
from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_AUTO_SHAPE_TYPE
from pptx.enum.text import PP_ALIGN
from pptx.util import Inches, Pt


ROOT = Path(r"E:\Github\Roguelike_Project")
DOCX = Path(r"C:\Users\haov8\Downloads\_Temp\ThaiVanHao-2121051075.docx")
TEMPLATE = Path(r"C:\Users\haov8\Downloads\BaoCaoDoAnGame_tren_form_BaoCaoDoAn_final-1.pptx")
OUT_DIR = ROOT / "output" / "ppt"
ASSET_DIR = OUT_DIR / "assets_thaivanhao"
OUT = OUT_DIR / "ThaiVanHao-2121051075_BaoCaoDoAnGame.pptx"

FONT = "Times New Roman"
TITLE_SIZE = 16
BODY_SIZE = 13
FOOTER_SIZE = 9

NAVY = RGBColor(25, 55, 105)
BLUE = RGBColor(55, 96, 146)
ORANGE = RGBColor(230, 120, 35)
GRAY = RGBColor(90, 90, 90)
LIGHT_GRAY = RGBColor(235, 240, 246)
WHITE = RGBColor(255, 255, 255)
BLACK = RGBColor(20, 20, 20)


def extract_assets() -> dict[int, Path]:
    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    assets: dict[int, Path] = {}
    with ZipFile(DOCX) as z:
        media = [n for n in z.namelist() if n.startswith("word/media/")]
        for idx, name in enumerate(media, start=1):
            out = ASSET_DIR / f"doc_image_{idx:02d}{Path(name).suffix.lower()}"
            out.write_bytes(z.read(name))
            assets[idx] = out
    return assets


def clear_slide_shapes(slide) -> None:
    sp_tree = slide.shapes._spTree  # noqa: SLF001 - python-pptx has no public clear API.
    for shape in list(slide.shapes):
        sp_tree.remove(shape._element)  # noqa: SLF001


def set_runs(paragraph, size: int, color=BLACK, bold=False) -> None:
    for run in paragraph.runs:
        run.font.name = FONT
        run.font.size = Pt(size)
        run.font.color.rgb = color
        run.font.bold = bold


def add_text(slide, text: str, x, y, w, h, size=BODY_SIZE, color=BLACK, bold=False, align=None):
    box = slide.shapes.add_textbox(x, y, w, h)
    tf = box.text_frame
    tf.clear()
    tf.word_wrap = True
    tf.margin_left = Inches(0.04)
    tf.margin_right = Inches(0.04)
    tf.margin_top = Inches(0.02)
    tf.margin_bottom = Inches(0.02)
    p = tf.paragraphs[0]
    p.text = text
    p.space_after = Pt(0)
    p.line_spacing = 1.0
    if align is not None:
        p.alignment = align
    set_runs(p, size, color, bold)
    return box


def add_bullets(slide, bullets: list[str], x, y, w, h, size=BODY_SIZE):
    box = slide.shapes.add_textbox(x, y, w, h)
    tf = box.text_frame
    tf.clear()
    tf.word_wrap = True
    tf.margin_left = Inches(0.06)
    tf.margin_right = Inches(0.06)
    tf.margin_top = Inches(0.03)
    tf.margin_bottom = Inches(0.03)
    for idx, bullet in enumerate(bullets):
        p = tf.paragraphs[0] if idx == 0 else tf.add_paragraph()
        p.text = bullet
        p.level = 0
        p.space_after = Pt(5)
        p.line_spacing = 1.05
        set_runs(p, size, BLACK, False)
    return box


def add_title(slide, title: str, number: int) -> None:
    add_text(slide, title, Inches(0.75), Inches(0.35), Inches(11.65), Inches(0.34), TITLE_SIZE, NAVY, True)
    line = slide.shapes.add_shape(MSO_AUTO_SHAPE_TYPE.RECTANGLE, Inches(0.75), Inches(0.78), Inches(11.65), Inches(0.02))
    line.fill.solid()
    line.fill.fore_color.rgb = LIGHT_GRAY
    line.line.fill.background()
    accent = slide.shapes.add_shape(MSO_AUTO_SHAPE_TYPE.RECTANGLE, Inches(0.75), Inches(0.78), Inches(1.2), Inches(0.03))
    accent.fill.solid()
    accent.fill.fore_color.rgb = ORANGE
    accent.line.fill.background()
    add_footer(slide, number)


def add_footer(slide, number: int) -> None:
    add_text(slide, "Bộ môn Mạng máy tính", Inches(0.78), Inches(7.12), Inches(3.0), Inches(0.2), FOOTER_SIZE, GRAY)
    add_text(slide, str(number), Inches(12.05), Inches(7.12), Inches(0.45), Inches(0.2), FOOTER_SIZE, GRAY, align=PP_ALIGN.RIGHT)


def add_panel(slide, title: str, bullets: list[str], x, y, w, h):
    rect = slide.shapes.add_shape(MSO_AUTO_SHAPE_TYPE.RECTANGLE, x, y, w, h)
    rect.fill.solid()
    rect.fill.fore_color.rgb = WHITE
    rect.line.color.rgb = LIGHT_GRAY
    add_text(slide, title, x + Inches(0.14), y + Inches(0.12), w - Inches(0.28), Inches(0.28), BODY_SIZE, BLUE, True)
    add_bullets(slide, bullets, x + Inches(0.14), y + Inches(0.48), w - Inches(0.28), h - Inches(0.6), BODY_SIZE)


def add_image_fit(slide, image_path: Path, x, y, w, h):
    with Image.open(image_path) as im:
        iw, ih = im.size
    scale = min(w / iw, h / ih)
    pw, ph = iw * scale, ih * scale
    pic = slide.shapes.add_picture(str(image_path), x + (w - pw) / 2, y + (h - ph) / 2, width=int(pw), height=int(ph))
    border = slide.shapes.add_shape(MSO_AUTO_SHAPE_TYPE.RECTANGLE, x, y, w, h)
    border.fill.background()
    border.line.color.rgb = LIGHT_GRAY
    return pic


def add_image_caption(slide, image_path: Path, caption: str, x, y, w, h):
    add_image_fit(slide, image_path, x, y, w, h)
    add_text(slide, caption, x, y + h + Inches(0.05), w, Inches(0.28), FOOTER_SIZE, GRAY, align=PP_ALIGN.CENTER)


def prepare_prs() -> tuple[Presentation, list]:
    prs = Presentation(TEMPLATE)
    base_slides = list(prs.slides)
    for slide in base_slides:
        clear_slide_shapes(slide)
    while len(prs.slides) < 20:
        prs.slides.add_slide(prs.slide_layouts[6])
    for slide in list(prs.slides)[15:]:
        clear_slide_shapes(slide)
    return prs, list(prs.slides)


def build() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    assets = extract_assets()
    prs, slides = prepare_prs()

    # 1. Cover
    s = slides[0]
    add_footer(s, 1)
    add_text(s, "BÁO CÁO ĐỒ ÁN TỐT NGHIỆP", Inches(1.0), Inches(1.45), Inches(11.25), Inches(0.38), 16, NAVY, True, PP_ALIGN.CENTER)
    add_text(s, "NGHIÊN CỨU VÀ PHÁT TRIỂN TRÒ CHƠI SINH TỒN 3D", Inches(1.2), Inches(2.25), Inches(10.85), Inches(0.55), 16, ORANGE, True, PP_ALIGN.CENTER)
    add_text(
        s,
        "Sinh viên thực hiện: Thái Văn Hào\nMã sinh viên: 2121051075\nGiáo viên hướng dẫn: Phạm Quang Hiển\nHà Nội, 5/2026",
        Inches(3.1),
        Inches(3.55),
        Inches(7.2),
        Inches(1.25),
        13,
        BLACK,
        False,
        PP_ALIGN.CENTER,
    )

    # 2. Agenda
    s = slides[1]
    add_title(s, "Nội dung trình bày", 2)
    add_bullets(
        s,
        [
            "Lý do chọn đề tài, mục tiêu và phạm vi thực hiện.",
            "Công nghệ sử dụng và cách tổ chức project Unity.",
            "Các hệ thống gameplay đã xây dựng: Player, Enemy, Wave, EXP, Buff.",
            "Các giao diện và chức năng hỗ trợ: HUD, Challenge, Pause, Settings, Leaderboard.",
            "Kiểm thử, kết quả đạt được, hạn chế và hướng phát triển.",
        ],
        Inches(1.1),
        Inches(1.25),
        Inches(10.9),
        Inches(4.8),
    )

    # 3
    s = slides[2]
    add_title(s, "Lý do chọn đề tài", 3)
    add_bullets(
        s,
        [
            "Game 3D giúp vận dụng nhiều kiến thức: lập trình, đồ họa, UI, âm thanh và thiết kế hệ thống.",
            "Thể loại sinh tồn kết hợp Roguelike có vòng lặp chơi rõ, dễ demo và có khả năng mở rộng.",
            "Đề tài phù hợp để xây dựng một sản phẩm đồ án có thể chơi thử, không chỉ dừng ở mô phỏng lý thuyết.",
        ],
        Inches(0.95),
        Inches(1.2),
        Inches(6.0),
        Inches(4.8),
    )
    add_image_caption(s, assets[28], "Hình ảnh gameplay trong báo cáo", Inches(7.25), Inches(1.22), Inches(4.85), Inches(3.05))

    # 4
    s = slides[3]
    add_title(s, "Mục tiêu và phạm vi", 4)
    add_panel(
        s,
        "Mục tiêu",
        [
            "Xây dựng game Roguelike sinh tồn 3D trên Unity.",
            "Hoàn thiện vòng lặp chơi: chiến đấu, nhận EXP, chọn buff, vượt wave.",
            "Tổ chức code và dữ liệu để có thể mở rộng sau đồ án.",
        ],
        Inches(0.9),
        Inches(1.25),
        Inches(5.7),
        Inches(4.5),
    )
    add_panel(
        s,
        "Phạm vi",
        [
            "Tập trung prototype gameplay chính.",
            "Backend giới hạn ở định danh người chơi và HighScore.",
            "Chưa đặt mục tiêu hoàn thiện như game thương mại.",
        ],
        Inches(6.85),
        Inches(1.25),
        Inches(5.35),
        Inches(4.5),
    )

    # 5
    s = slides[4]
    add_title(s, "Công nghệ sử dụng", 5)
    add_panel(
        s,
        "Unity và C#",
        ["Xử lý gameplay runtime, input, collision, animation và UI.", "MonoBehaviour dùng cho các hệ thống tương tác trực tiếp trong scene."],
        Inches(0.85),
        Inches(1.2),
        Inches(3.8),
        Inches(2.05),
    )
    add_panel(
        s,
        "Dữ liệu và hiệu năng",
        ["ScriptableObject lưu cấu hình Player, Enemy, Wave, Buff.", "Object Pooling giảm chi phí sinh/hủy projectile và enemy."],
        Inches(4.9),
        Inches(1.2),
        Inches(3.8),
        Inches(2.05),
    )
    add_panel(
        s,
        "Quản lý và backend",
        ["Git/GitHub quản lý phiên bản mã nguồn.", "PlayFab lưu tên hiển thị và bảng xếp hạng HighScore."],
        Inches(8.95),
        Inches(1.2),
        Inches(3.35),
        Inches(2.05),
    )
    add_bullets(s, ["Các công nghệ được chọn phục vụ trực tiếp cho việc làm ra bản demo chơi được và dễ trình bày khi bảo vệ."], Inches(1.0), Inches(4.1), Inches(11.0), Inches(0.8))

    # 6
    s = slides[5]
    add_title(s, "Tổng quan sản phẩm gameplay", 6)
    add_image_caption(s, assets[29], "Màn hình gameplay chính", Inches(0.9), Inches(1.2), Inches(5.7), Inches(3.15))
    add_bullets(
        s,
        [
            "Người chơi điều khiển nhân vật trong môi trường 3D.",
            "Enemy xuất hiện theo wave và tấn công người chơi.",
            "Người chơi tiêu diệt enemy để nhận EXP, lên cấp và chọn buff.",
            "Điểm số cuối lượt chơi được gửi lên leaderboard.",
        ],
        Inches(7.05),
        Inches(1.25),
        Inches(5.0),
        Inches(3.8),
    )

    # 7
    s = slides[6]
    add_title(s, "Những hệ thống chính đã xây dựng", 7)
    add_panel(s, "Player", ["Di chuyển, dash, máu, tự động tấn công.", "Đồng bộ trạng thái với animation và UI."], Inches(0.9), Inches(1.15), Inches(3.55), Inches(1.6))
    add_panel(s, "Enemy và Wave", ["Enemy nhận sát thương, tấn công và phát EXP.", "WaveSpawner sinh quái, boss wave và tăng độ khó."], Inches(4.85), Inches(1.15), Inches(3.55), Inches(1.6))
    add_panel(s, "Progression", ["Cộng EXP, lên cấp, chọn buff.", "Buff tác động lên chỉ số hoặc kỹ năng chiến đấu."], Inches(8.8), Inches(1.15), Inches(3.55), Inches(1.6))
    add_panel(s, "UI và Backend", ["HUD, Challenge, Pause, Settings, Loading.", "PlayFab đăng nhập, nhập tên và HighScore."], Inches(2.85), Inches(3.25), Inches(7.6), Inches(1.75))

    # 8
    s = slides[7]
    add_title(s, "Tổ chức module trong project", 8)
    add_panel(s, "Nhóm gameplay", ["PlayerController, PlayerAttack, PlayerHealth.", "Enemy, MeleeEnemy, RangedEnemy, FlyEnemy, BossEnemy.", "WaveSpawner, EnemyConfig, WaveConfig."], Inches(0.9), Inches(1.2), Inches(5.55), Inches(4.3))
    add_panel(s, "Nhóm hỗ trợ", ["PlayerLevelSystem và BuffCardManager.", "GameUI, các panel chức năng và LoadingUIManager.", "PlayFabLeaderboardManager xử lý định danh và điểm."], Inches(6.85), Inches(1.2), Inches(5.35), Inches(4.3))

    # 9
    s = slides[8]
    add_title(s, "Điều khiển nhân vật và chiến đấu", 9)
    add_image_caption(s, assets[30], "Nhân vật chiến đấu trong gameplay", Inches(0.9), Inches(1.2), Inches(5.75), Inches(3.3))
    add_bullets(
        s,
        [
            "Input System gửi dữ liệu di chuyển và thao tác cho PlayerController.",
            "CharacterController xử lý di chuyển, gravity và dash.",
            "PlayerAttack tự tìm enemy gần nhất và bắn projectile.",
            "Projectile gây sát thương thông qua interface IDamageable.",
        ],
        Inches(7.05),
        Inches(1.25),
        Inches(5.05),
        Inches(3.75),
    )

    # 10
    s = slides[9]
    add_title(s, "Enemy, boss và wave", 10)
    add_panel(s, "Enemy đã làm", ["Melee enemy áp sát người chơi.", "Ranged/Fly enemy tấn công từ xa.", "Boss enemy tạo thử thách ở wave quan trọng."], Inches(0.9), Inches(1.2), Inches(4.8), Inches(3.3))
    add_panel(s, "Wave đã làm", ["WaveSpawner đọc cấu hình wave.", "Theo dõi số enemy còn sống.", "Hoàn tất wave để chuyển sang wave tiếp theo.", "Tăng độ khó bằng autoScale và scalePerWave."], Inches(6.0), Inches(1.2), Inches(4.15), Inches(3.3))
    add_image_caption(s, assets[31], "Enemy/boss trong báo cáo", Inches(10.45), Inches(1.25), Inches(1.75), Inches(2.5))

    # 11
    s = slides[10]
    add_title(s, "EXP, level-up và buff", 11)
    add_image_caption(s, assets[34], "Màn hình chọn buff", Inches(0.9), Inches(1.2), Inches(5.75), Inches(3.1))
    add_bullets(
        s,
        [
            "Enemy chết sẽ cộng EXP cho người chơi.",
            "Khi đủ EXP, hệ thống tăng level và mở giao diện chọn buff.",
            "BuffCardManager chọn các card phù hợp theo rarity và luckBonus.",
            "Buff được áp dụng vào PlayerData, máu hoặc các modifier chiến đấu.",
        ],
        Inches(7.05),
        Inches(1.25),
        Inches(5.05),
        Inches(3.75),
    )

    # 12
    s = slides[11]
    add_title(s, "Theme map và chuyển đổi môi trường", 12)
    add_image_caption(s, assets[32], "Theme map 1", Inches(0.9), Inches(1.2), Inches(5.3), Inches(3.0))
    add_image_caption(s, assets[33], "Theme map 2", Inches(6.85), Inches(1.2), Inches(5.3), Inches(3.0))
    add_bullets(s, ["MapThemeManager thay đổi vật liệu, tường và effectRoot theo tiến trình wave.", "LoadingUIManager dùng hiệu ứng chuyển cảnh để quá trình đổi theme không gây gián đoạn trải nghiệm."], Inches(1.1), Inches(4.8), Inches(10.8), Inches(1.0))

    # 13
    s = slides[12]
    add_title(s, "Giao diện HUD, Challenge, Pause và Settings", 13)
    add_image_caption(s, assets[35], "HUD trong trận", Inches(0.85), Inches(1.2), Inches(3.65), Inches(2.15))
    add_image_caption(s, assets[36], "Challenge panel", Inches(4.85), Inches(1.2), Inches(3.65), Inches(2.15))
    add_image_caption(s, assets[37], "Pause/Settings", Inches(8.85), Inches(1.2), Inches(3.65), Inches(2.15))
    add_bullets(s, ["HUD hiển thị HP, EXP, level và wave.", "ChallengePanel điều khiển luồng bắt đầu trận.", "Pause/Settings cho phép tạm dừng và điều chỉnh âm thanh."], Inches(1.05), Inches(4.2), Inches(10.7), Inches(1.2))

    # 14
    s = slides[13]
    add_title(s, "Nhập tên và bảng xếp hạng PlayFab", 14)
    add_image_caption(s, assets[38], "Giao diện nhập tên", Inches(0.9), Inches(1.2), Inches(5.4), Inches(1.75))
    add_image_caption(s, assets[39], "Bảng xếp hạng", Inches(0.9), Inches(3.35), Inches(5.4), Inches(1.55))
    add_bullets(
        s,
        [
            "Người chơi được định danh bằng LoginWithCustomID.",
            "Nếu chưa có Display Name, hệ thống mở panel nhập tên.",
            "Khi kết thúc lượt chơi, điểm được gửi lên PlayFab HighScore.",
            "Leaderboard hiển thị top người chơi và thứ hạng hiện tại.",
        ],
        Inches(6.85),
        Inches(1.25),
        Inches(5.1),
        Inches(3.8),
    )

    # 15
    s = slides[14]
    add_title(s, "Một số màn hình demo đã hoàn thiện", 15)
    add_image_caption(s, assets[29], "Gameplay", Inches(0.85), Inches(1.15), Inches(3.7), Inches(2.05))
    add_image_caption(s, assets[30], "Combat", Inches(4.85), Inches(1.15), Inches(3.7), Inches(2.05))
    add_image_caption(s, assets[34], "Buff", Inches(8.85), Inches(1.15), Inches(3.7), Inches(2.05))
    add_image_caption(s, assets[41], "Giao diện khác", Inches(4.85), Inches(3.85), Inches(3.7), Inches(2.05))

    # 16
    s = slides[15]
    add_title(s, "Kiểm thử chức năng chính", 16)
    add_panel(s, "Gameplay", ["Di chuyển, dash, camera.", "Tấn công, projectile, nhận damage.", "Enemy death và cộng EXP."], Inches(0.9), Inches(1.2), Inches(3.6), Inches(3.1))
    add_panel(s, "Hệ thống", ["Spawn wave, boss wave, hoàn tất wave.", "Level-up, chọn buff, cập nhật chỉ số.", "Đổi theme map và loading."], Inches(4.9), Inches(1.2), Inches(3.6), Inches(3.1))
    add_panel(s, "Giao diện/backend", ["HUD, challenge, pause, settings.", "Nhập tên, gửi điểm, xem leaderboard."], Inches(8.9), Inches(1.2), Inches(3.4), Inches(3.1))

    # 17
    s = slides[16]
    add_title(s, "Kết quả đạt được", 17)
    add_bullets(
        s,
        [
            "Hoàn thiện khung game Roguelike 3D ở mức đồ án.",
            "Có vòng lặp chơi rõ: chiến đấu, EXP, buff, wave và leaderboard.",
            "Các module chính được tách theo chức năng để dễ bảo trì.",
            "Giao diện cơ bản đã hỗ trợ đầy đủ quá trình chơi và demo.",
        ],
        Inches(0.95),
        Inches(1.25),
        Inches(6.05),
        Inches(4.1),
    )
    add_image_caption(s, assets[41], "Kết quả giao diện/gameplay", Inches(7.35), Inches(1.25), Inches(4.7), Inches(2.65))

    # 18
    s = slides[17]
    add_title(s, "Hạn chế", 18)
    add_bullets(
        s,
        [
            "Số lượng enemy, boss pattern, buff và map variation còn ít.",
            "Cân bằng độ khó giữa các wave và các buff cần tiếp tục tinh chỉnh.",
            "Một số tài nguyên giao diện và hình ảnh chưa đồng đều về mặt mỹ thuật.",
            "Hiệu năng ở wave có mật độ spawn lớn cần kiểm thử và tối ưu thêm.",
        ],
        Inches(1.0),
        Inches(1.25),
        Inches(10.8),
        Inches(4.6),
    )

    # 19
    s = slides[18]
    add_title(s, "Hướng phát triển", 19)
    add_bullets(
        s,
        [
            "Hoàn thiện Main Menu và luồng điều hướng tổng thể.",
            "Mở rộng danh sách buff, enemy, boss và theme map.",
            "Bổ sung thống kê ngoài HighScore như thời gian sống sót và số enemy tiêu diệt.",
            "Cải thiện AI, hiệu ứng, UI/UX và tối ưu hiệu năng.",
            "Đóng gói bản demo ổn định hơn để thử nghiệm người dùng.",
        ],
        Inches(1.0),
        Inches(1.25),
        Inches(10.8),
        Inches(4.8),
    )

    # 20
    s = slides[19]
    add_footer(s, 20)
    add_text(s, "XIN CẢM ƠN THẦY CÔ ĐÃ LẮNG NGHE", Inches(1.0), Inches(2.45), Inches(11.25), Inches(0.45), 16, NAVY, True, PP_ALIGN.CENTER)
    add_text(s, "Sinh viên thực hiện: Thái Văn Hào - 2121051075", Inches(2.1), Inches(3.35), Inches(9.2), Inches(0.35), 13, BLACK, False, PP_ALIGN.CENTER)

    prs.save(OUT)
    print(OUT)


if __name__ == "__main__":
    build()
