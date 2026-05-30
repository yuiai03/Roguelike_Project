from __future__ import annotations

import html
import uuid
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "output" / "diagrams" / "drawio"


@dataclass
class Node:
    id: str
    text: str
    x: int
    y: int
    w: int = 220
    h: int = 80
    kind: str = "box"  # box, terminator, actor, note


@dataclass
class Edge:
    source: str
    target: str
    label: str = ""
    dashed: bool = False


@dataclass
class DiagramSpec:
    filename: str
    report_section: str
    caption: str
    page_name: str
    width: int
    height: int
    nodes: list[Node]
    edges: list[Edge]


BOX_STYLE = (
    "rounded=1;whiteSpace=wrap;html=1;fillColor=#ffffff;strokeColor=#000000;"
    "fontColor=#000000;fontSize=14;fontFamily=Times New Roman;"
)
TERM_STYLE = (
    "rounded=1;arcSize=50;whiteSpace=wrap;html=1;fillColor=#ffffff;strokeColor=#000000;"
    "fontColor=#000000;fontSize=14;fontFamily=Times New Roman;"
)
NOTE_STYLE = (
    "rounded=1;whiteSpace=wrap;html=1;fillColor=#ffffff;strokeColor=#000000;"
    "fontColor=#000000;fontSize=13;fontFamily=Times New Roman;"
)
ACTOR_STYLE = "shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;fontColor=#000000;strokeColor=#000000;fontSize=14;fontFamily=Times New Roman;"
EDGE_STYLE = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=block;endFill=1;strokeColor=#000000;fontColor=#000000;fontSize=12;fontFamily=Times New Roman;"
DASHED_EDGE_STYLE = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=open;dashed=1;strokeColor=#000000;fontColor=#000000;fontSize=12;fontFamily=Times New Roman;"


def esc(text: str) -> str:
    return html.escape(text).replace("\n", "&lt;br&gt;")


def style_for(kind: str) -> str:
    return {
        "box": BOX_STYLE,
        "terminator": TERM_STYLE,
        "actor": ACTOR_STYLE,
        "note": NOTE_STYLE,
    }[kind]


def build_diagram_xml(spec: DiagramSpec) -> str:
    parts = [
        '<mxfile host="app.diagrams.net" modified="2026-05-17T00:00:00.000Z" agent="Codex" version="24.7.17">',
        f'  <diagram id="{uuid.uuid4().hex[:12]}" name="{esc(spec.page_name)}">',
        f'    <mxGraphModel dx="{spec.width}" dy="{spec.height}" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="{spec.width}" pageHeight="{spec.height}" math="0" shadow="0">',
        "      <root>",
        '        <mxCell id="0" />',
        '        <mxCell id="1" parent="0" />',
    ]
    for node in spec.nodes:
        parts.extend(
            [
                f'        <mxCell id="{node.id}" value="{esc(node.text)}" style="{style_for(node.kind)}" vertex="1" parent="1">',
                f'          <mxGeometry x="{node.x}" y="{node.y}" width="{node.w}" height="{node.h}" as="geometry" />',
                "        </mxCell>",
            ]
        )
    edge_id = 1000
    for edge in spec.edges:
        style = DASHED_EDGE_STYLE if edge.dashed else EDGE_STYLE
        parts.extend(
            [
                f'        <mxCell id="e{edge_id}" value="{esc(edge.label)}" style="{style}" edge="1" parent="1" source="{edge.source}" target="{edge.target}">',
                '          <mxGeometry relative="1" as="geometry" />',
                "        </mxCell>",
            ]
        )
        edge_id += 1
    parts.extend(["      </root>", "    </mxGraphModel>", "  </diagram>", "</mxfile>"])
    return "\n".join(parts)


def write_diagram(spec: DiagramSpec) -> None:
    (OUTPUT_DIR / spec.filename).write_text(build_diagram_xml(spec), encoding="utf-8")


def n(id_: str, text: str, x: int, y: int, w: int = 220, h: int = 80, kind: str = "box") -> Node:
    return Node(id=id_, text=text, x=x, y=y, w=w, h=h, kind=kind)


def e(src: str, tgt: str, label: str = "", dashed: bool = False) -> Edge:
    return Edge(source=src, target=tgt, label=label, dashed=dashed)


def section_14_specs() -> list[DiagramSpec]:
    return [
        DiagramSpec(
            "1_4_1_1_dinh_danh_nguoi_choi.drawio",
            "1.4.1.1",
            "Sơ đồ định danh người chơi qua PlayFab",
            "1.4.1.1 Định danh người chơi",
            1400,
            900,
            [
                n("a1", "Player", 80, 260, 120, 140, "actor"),
                n("n1", "PlayFabLeaderboardManager\nLoginWithCustomID", 280, 280),
                n("n2", "GetPlayerProfile\nShowDisplayName", 560, 280),
                n("n3", "NameInputPanel\nnhập Display Name", 840, 180),
                n("n4", "SubmitName\n3..25 ký tự", 1120, 180),
                n("n5", "CurrentDisplayName\nCurrentPlayFabId", 840, 380, kind="note"),
            ],
            [e("a1", "n1", "bắt đầu game"), e("n1", "n2", "đăng nhập"), e("n2", "n3", "chưa có tên"), e("n3", "n4", "xác nhận"), e("n4", "n5", "cập nhật"), e("n2", "n5", "đã có tên", True)],
        ),
        DiagramSpec(
            "1_4_1_2_bat_dau_tran_dau.drawio",
            "1.4.1.2",
            "Sơ đồ bắt đầu trận đấu",
            "1.4.1.2 Bắt đầu trận đấu",
            1400,
            900,
            [
                n("a1", "Player", 80, 260, 120, 140, "actor"),
                n("n1", "ChallengePostNPC", 260, 280),
                n("n2", "InteractPanel\nhiện prompt F", 540, 280),
                n("n3", "ChallengePanel\nShowTutorial", 820, 280),
                n("n4", "onGameStart", 1100, 180, kind="terminator"),
                n("n5", "WaveSpawner\nStartNextWave", 1100, 380),
            ],
            [e("a1", "n1", "vào trigger"), e("n1", "n2", "hiện hướng dẫn"), e("n2", "n3", "nhấn F"), e("n3", "n4", "StartGame"), e("n4", "n5", "khởi tạo trận")],
        ),
        DiagramSpec(
            "1_4_1_3_player_controller.drawio",
            "1.4.1.3",
            "Sơ đồ điều khiển nhân vật",
            "1.4.1.3 Player",
            1600,
            900,
            [
                n("n1", "InputSystem_Actions\nMove / Right Click", 80, 300),
                n("n2", "PlayerController", 360, 300),
                n("n3", "Camera.main\nforward/right", 360, 130, kind="note"),
                n("n4", "CharacterController\nMove + Gravity", 660, 220),
                n("n5", "Dash State\nisDashing / cooldown", 660, 410),
                n("n6", "PlayerAnimationController\nIdle / Run / Dash", 980, 220),
                n("n7", "PlayerData\nmoveSpeed=5\ndashSpeed=15\ndashCooldown=1", 980, 410, kind="note"),
            ],
            [e("n1", "n2", "moveInput"), e("n3", "n2", "camera direction", True), e("n2", "n4", "move"), e("n2", "n5", "dash logic"), e("n4", "n6", "trạng thái"), e("n5", "n6", "dash"), e("n7", "n2", "tham số", True)],
        ),
        DiagramSpec(
            "1_4_1_4_chien_dau_player.drawio",
            "1.4.1.4",
            "Sơ đồ chiến đấu của người chơi",
            "1.4.1.4 Chiến đấu player",
            1600,
            900,
            [
                n("n1", "PlayerAttack", 80, 300),
                n("n2", "FindNearestEnemy\nattackRange=20", 340, 300),
                n("n3", "ObjectPool\nSpawn PlayerProjectile", 620, 300),
                n("n4", "PlayerProjectile\nInitialize damage/speed/lifetime", 920, 220, 260, 90),
                n("n5", "Extra Buff Data\nmultishot / AoE", 920, 410, 260, 80, "note"),
                n("n6", "Enemy / IDamageable", 1240, 300),
            ],
            [e("n1", "n2", "cooldown<=0"), e("n2", "n3", "có mục tiêu"), e("n3", "n4", "spawn"), e("n5", "n4", "tham số", True), e("n4", "n6", "gây sát thương")],
        ),
        DiagramSpec(
            "1_4_1_5_enemy_system.drawio",
            "1.4.1.5",
            "Sơ đồ nhóm enemy",
            "1.4.1.5 Enemy",
            1700,
            1000,
            [
                n("n1", "EnemyConfig", 80, 280),
                n("n2", "EnemyData\nmaxHealth/moveSpeed/\nprojectileDamage/expValue", 340, 260, 240, 100),
                n("n3", "Enemy\nCharacterController + AI", 660, 260, 240, 100),
                n("n4", "MeleeEnemy", 980, 140),
                n("n5", "RangedEnemy", 980, 280),
                n("n6", "FlyEnemy", 980, 420),
                n("n7", "BossEnemy", 980, 560),
                n("n8", "OnDeath\nExpDropper / Wave update", 1280, 280, 260, 100, "terminator"),
            ],
            [e("n1", "n2", "nạp cấu hình"), e("n2", "n3", "runtime state"), e("n3", "n4"), e("n3", "n5"), e("n3", "n6"), e("n3", "n7"), e("n3", "n8", "hp<=0")],
        ),
        DiagramSpec(
            "1_4_1_6_wave_va_do_kho.drawio",
            "1.4.1.6",
            "Sơ đồ quản lý wave và độ khó",
            "1.4.1.6 Wave",
            1700,
            950,
            [
                n("n1", "WaveConfig\nwaves / enemyGroups /\nautoScale=1.1", 80, 300, 250, 100),
                n("n2", "WaveSpawner\ncurrentWave / session", 400, 300, 250, 100),
                n("n3", "Spawn Groups", 740, 180),
                n("n4", "Boss Wave\nbossPoolTypes", 740, 420),
                n("n5", "Active Enemies\nOnEnemyCountChanged", 1060, 180, 250, 100),
                n("n6", "CompleteWave", 1060, 420, kind="terminator"),
                n("n7", "Endless Wave\nre-use base wave", 1380, 300, 240, 100, "note"),
            ],
            [e("n1", "n2", "đọc cấu hình"), e("n2", "n3", "wave thường"), e("n2", "n4", "boss wave"), e("n3", "n5", "spawn"), e("n4", "n5", "spawn boss"), e("n5", "n6", "count==0"), e("n2", "n7", "vượt số wave", True)],
        ),
        DiagramSpec(
            "1_4_1_7_theme_ban_do.drawio",
            "1.4.1.7",
            "Sơ đồ đổi theme bản đồ",
            "1.4.1.7 Theme bản đồ",
            1500,
            900,
            [
                n("n1", "Wave complete", 100, 300, kind="terminator"),
                n("n2", "WaveSpawner\nupcomingWave", 340, 300),
                n("n3", "MapThemeManager\nResolveThemeIndexForWave", 620, 300, 260, 100),
                n("n4", "LoadingUIManager\nblack transition", 960, 180, 240, 90),
                n("n5", "Apply ground / wall /\neffectRoot", 960, 420, 240, 90),
                n("n6", "OnThemeTransitionCompleted", 1260, 300, 240, 90, "terminator"),
            ],
            [e("n1", "n2"), e("n2", "n3", "mốc 10 wave"), e("n3", "n4", "cần đổi"), e("n4", "n5"), e("n5", "n6"), e("n3", "n6", "không đổi", True)],
        ),
        DiagramSpec(
            "1_4_1_8_exp_va_len_cap.drawio",
            "1.4.1.8",
            "Sơ đồ EXP và lên cấp",
            "1.4.1.8 EXP và lên cấp",
            1500,
            900,
            [
                n("n1", "Enemy OnDeath", 100, 300, kind="terminator"),
                n("n2", "ExpDropper\nexpValue", 340, 300),
                n("n3", "PlayerLevelSystem\nAddExp", 620, 300),
                n("n4", "Update EXP HUD\nOnExpChanged", 940, 180, 240, 90),
                n("n5", "LevelUp\ncurrentLevel++", 940, 420, 240, 90),
                n("n6", "OnLevelUp", 1240, 420, kind="terminator"),
                n("n7", "totalExpGained\nHighScore source", 1240, 180, 240, 90, "note"),
            ],
            [e("n1", "n2"), e("n2", "n3", "cộng EXP"), e("n3", "n4", "mọi lần"), e("n3", "n5", "đủ ngưỡng"), e("n5", "n6"), e("n3", "n7", "tích lũy", True)],
        ),
        DiagramSpec(
            "1_4_1_9_buff_va_tang_suc_manh.drawio",
            "1.4.1.9",
            "Sơ đồ chọn buff và tăng sức mạnh",
            "1.4.1.9 Buff",
            1600,
            900,
            [
                n("n1", "OnLevelUp", 80, 300, kind="terminator"),
                n("n2", "BuffCardManager\nGetRandomCards", 320, 300),
                n("n3", "cardsPerSelection=3\nrarity + luckBonus", 320, 140, 240, 90, "note"),
                n("n4", "CardSelectionPanel\nShowCards", 660, 300),
                n("n5", "ApplyCard", 960, 300),
                n("n6", "PlayerData /\nPlayerHealth", 1240, 180),
                n("n7", "Spirit / OrbitingBall /\ncombat modifiers", 1240, 420, 260, 90),
            ],
            [e("n1", "n2"), e("n3", "n2", "lọc card", True), e("n2", "n4"), e("n4", "n5", "người chơi chọn"), e("n5", "n6", "buff chỉ số"), e("n5", "n7", "buff kỹ năng")],
        ),
        DiagramSpec(
            "1_4_1_10_ui_trong_tran.drawio",
            "1.4.1.10",
            "Sơ đồ UI trong trận",
            "1.4.1.10 UI trong trận",
            1800,
            1000,
            [
                n("n1", "GameUI", 760, 300, 260, 100, "terminator"),
                n("n2", "PlayerStatsPanel\nHP / EXP / Lv / Wave", 220, 100),
                n("n3", "InteractPanel", 220, 300),
                n("n4", "CardSelectionPanel", 220, 500),
                n("n5", "NameInputPanel", 1280, 100),
                n("n6", "LeaderboardPanel", 1280, 300),
                n("n7", "PauseMenuPanel", 1280, 500),
                n("n8", "Input ownership\nSetInputActive", 760, 540, 260, 90, "note"),
            ],
            [e("n1", "n2"), e("n1", "n3"), e("n1", "n4"), e("n1", "n5"), e("n1", "n6"), e("n1", "n7"), e("n8", "n1", "panel priority", True)],
        ),
        DiagramSpec(
            "1_4_1_11_pause_va_ket_thuc_tran.drawio",
            "1.4.1.11",
            "Sơ đồ pause và kết thúc trận",
            "1.4.1.11 Pause và kết thúc trận",
            1800,
            1000,
            [
                n("n1", "ESC", 80, 240, kind="terminator"),
                n("n2", "PauseMenuPanel", 320, 240),
                n("n3", "Time.timeScale = 0\nPlayer input off", 620, 160, 260, 90, "note"),
                n("n4", "Resume / Settings /\nLeaderboard / Quit", 620, 340, 260, 90),
                n("n5", "PlayerHealth\nhp<=0", 980, 560),
                n("n6", "Death cleanup\nclear wave / despawn pool", 1260, 470, 280, 100),
                n("n7", "Leaderboard after death", 1260, 650, 280, 100, "terminator"),
            ],
            [e("n1", "n2"), e("n2", "n3", "pause"), e("n2", "n4"), e("n5", "n6", "die"), e("n6", "n7")],
        ),
        DiagramSpec(
            "1_4_1_12_leaderboard.drawio",
            "1.4.1.12",
            "Sơ đồ leaderboard",
            "1.4.1.12 Leaderboard",
            1600,
            900,
            [
                n("n1", "PlayerHealth\nGetCurrentRunScore", 80, 300),
                n("n2", "PlayFabLeaderboardManager\nSubmitScore", 380, 300, 260, 100),
                n("n3", "PlayFab\nStatistic: HighScore", 740, 300, 240, 100),
                n("n4", "GetLeaderboardData\nMaxResultsCount=100", 1080, 180, 260, 100),
                n("n5", "GetPlayerLeaderboardData", 1080, 420, 260, 100),
                n("n6", "LeaderboardPanel\nentry + current player", 1380, 300, 260, 100, "terminator"),
            ],
            [e("n1", "n2", "score=floor(totalExpGained)"), e("n2", "n3"), e("n3", "n4", "top list"), e("n3", "n5", "around player"), e("n4", "n6"), e("n5", "n6")],
        ),
    ]


def chapter3_specs() -> list[DiagramSpec]:
    return [
        DiagramSpec(
            "3_1_1_use_case_nguoi_choi_truoc_tran.drawio",
            "3.1 Use Case",
            "Use Case người chơi trước trận",
            "3.1.1 Use Case trước trận",
            1600,
            900,
            [
                n("a1", "Người chơi", 90, 220, 120, 140, "actor"),
                n("a2", "PlayFab", 1320, 220, 120, 140, "actor"),
                n("n1", "Đăng nhập\nCustom ID", 420, 120, kind="terminator"),
                n("n2", "Nhập tên\nhiển thị", 420, 320, kind="terminator"),
                n("n3", "Bắt đầu trận", 820, 220, kind="terminator"),
            ],
            [e("a1", "n1"), e("a1", "n2"), e("a1", "n3"), e("a2", "n1", "xác thực"), e("a2", "n2", "Display Name")],
        ),
        DiagramSpec(
            "3_1_2_use_case_gameplay_core_loop.drawio",
            "3.1 Use Case",
            "Use Case gameplay core loop",
            "3.1.2 Use Case gameplay core loop",
            1800,
            1000,
            [
                n("a1", "Người chơi", 80, 260, 120, 140, "actor"),
                n("n1", "Điều khiển nhân vật", 340, 100, kind="terminator"),
                n("n2", "Chiến đấu tự động", 340, 260, kind="terminator"),
                n("n3", "Nhận EXP\nLên cấp", 340, 420, kind="terminator"),
                n("n4", "Chọn buff", 720, 180, kind="terminator"),
                n("n5", "Hoàn thành wave", 720, 340, kind="terminator"),
                n("n6", "Đổi theme /\nBoss wave", 1100, 180, kind="terminator"),
            ],
            [e("a1", "n1"), e("a1", "n2"), e("a1", "n3"), e("n3", "n4"), e("n2", "n5"), e("n5", "n6")],
        ),
        DiagramSpec(
            "3_1_3_use_case_ket_thuc_tran_leaderboard.drawio",
            "3.1 Use Case",
            "Use Case kết thúc trận và leaderboard",
            "3.1.3 Use Case kết thúc trận và leaderboard",
            1700,
            900,
            [
                n("a1", "Người chơi", 80, 240, 120, 140, "actor"),
                n("a2", "PlayFab", 1410, 240, 120, 140, "actor"),
                n("n1", "Tạm dừng", 360, 120, kind="terminator"),
                n("n2", "Kết thúc trận", 360, 320, kind="terminator"),
                n("n3", "Gửi điểm", 760, 220, kind="terminator"),
                n("n4", "Xem leaderboard", 1120, 220, kind="terminator"),
            ],
            [e("a1", "n1"), e("a1", "n2"), e("n2", "n3"), e("n3", "n4"), e("a2", "n3"), e("a2", "n4")],
        ),
        DiagramSpec(
            "3_2_1_sequence_dang_nhap_nhap_ten.drawio",
            "3.2 Sequence",
            "Sequence đăng nhập và nhập tên hiển thị",
            "3.2.1 Sequence đăng nhập - nhập tên",
            1800,
            1000,
            [
                n("n1", "Player", 60, 60, 140, 60, "note"),
                n("n2", "PlayFabLeaderboardManager", 300, 60, 220, 60, "note"),
                n("n3", "PlayFab", 620, 60, 180, 60, "note"),
                n("n4", "NameInputPanel", 940, 60, 180, 60, "note"),
                n("n5", "CurrentDisplayName", 1260, 60, 180, 60, "note"),
            ],
            [
                e("n1", "n2", "start"),
                e("n2", "n3", "LoginWithCustomID"),
                e("n3", "n2", "PlayFabId"),
                e("n2", "n3", "GetPlayerProfile"),
                e("n3", "n2", "DisplayName?"),
                e("n2", "n4", "show if empty"),
                e("n4", "n2", "SubmitName"),
                e("n2", "n3", "UpdateUserTitleDisplayName"),
                e("n2", "n5", "sync"),
            ],
        ),
        DiagramSpec(
            "3_2_2_sequence_chien_dau_exp_buff.drawio",
            "3.2 Sequence",
            "Sequence chiến đấu - nhận EXP - chọn buff",
            "3.2.2 Sequence chiến đấu - EXP - buff",
            2200,
            1200,
            [
                n("n1", "PlayerAttack", 60, 60, 180, 60, "note"),
                n("n2", "ObjectPool", 340, 60, 180, 60, "note"),
                n("n3", "Enemy", 620, 60, 180, 60, "note"),
                n("n4", "ExpDropper", 900, 60, 180, 60, "note"),
                n("n5", "PlayerLevelSystem", 1180, 60, 200, 60, "note"),
                n("n6", "BuffCardManager", 1480, 60, 200, 60, "note"),
                n("n7", "CardSelectionPanel", 1780, 60, 220, 60, "note"),
            ],
            [
                e("n1", "n2", "Spawn projectile"),
                e("n2", "n3", "Projectile hit"),
                e("n3", "n4", "OnDeath"),
                e("n4", "n5", "AddExp"),
                e("n5", "n6", "OnLevelUp"),
                e("n6", "n7", "GetRandomCards"),
                e("n7", "n6", "selected card"),
            ],
        ),
        DiagramSpec(
            "3_2_3_sequence_game_over_gui_diem_tai_leaderboard.drawio",
            "3.2 Sequence",
            "Sequence game over - gửi điểm - tải leaderboard",
            "3.2.3 Sequence game over - leaderboard",
            2100,
            1100,
            [
                n("n1", "PlayerHealth", 60, 60, 180, 60, "note"),
                n("n2", "PlayerLevelSystem", 360, 60, 200, 60, "note"),
                n("n3", "PlayFabLeaderboardManager", 700, 60, 240, 60, "note"),
                n("n4", "PlayFab", 1080, 60, 180, 60, "note"),
                n("n5", "LeaderboardPanel", 1400, 60, 220, 60, "note"),
            ],
            [
                e("n1", "n2", "GetTotalExpGained"),
                e("n1", "n3", "SubmitScore(finalScore)"),
                e("n3", "n4", "UpdatePlayerStatistics"),
                e("n4", "n3", "ok"),
                e("n3", "n4", "GetLeaderboard / AroundPlayer"),
                e("n4", "n5", "entries"),
            ],
        ),
        DiagramSpec(
            "3_3_activity_gameplay_core_loop.drawio",
            "3.3 Activity",
            "Activity gameplay core loop",
            "3.3 Activity gameplay core loop",
            1700,
            1500,
            [
                n("n1", "Start", 680, 60, 180, 60, "terminator"),
                n("n2", "Đăng nhập PlayFab", 680, 170),
                n("n3", "Có Display Name?", 680, 290, 220, 80, "note"),
                n("n4", "Nhập tên", 360, 400),
                n("n5", "Tương tác NPC\nStartGame", 680, 400),
                n("n6", "Spawn wave", 680, 520),
                n("n7", "Di chuyển + chiến đấu", 680, 640),
                n("n8", "Enemy chết?", 680, 760, 220, 80, "note"),
                n("n9", "Cộng EXP", 360, 880),
                n("n10", "Đủ EXP để level up?", 360, 1000, 220, 80, "note"),
                n("n11", "Hiện 3 buff", 360, 1120),
                n("n12", "Wave hoàn tất?", 980, 880, 220, 80, "note"),
                n("n13", "Boss / đổi theme", 980, 1000),
                n("n14", "Player chết?", 980, 1120, 220, 80, "note"),
                n("n15", "Gửi HighScore\nHiện leaderboard", 980, 1240),
                n("n16", "End", 980, 1360, 180, 60, "terminator"),
            ],
            [
                e("n1", "n2"),
                e("n2", "n3"),
                e("n3", "n4", "không"),
                e("n3", "n5", "có"),
                e("n4", "n5"),
                e("n5", "n6"),
                e("n6", "n7"),
                e("n7", "n8"),
                e("n8", "n9", "có"),
                e("n9", "n10"),
                e("n10", "n11", "có"),
                e("n11", "n7"),
                e("n8", "n12", "không", True),
                e("n12", "n13", "có"),
                e("n13", "n6"),
                e("n12", "n14", "không", True),
                e("n14", "n15", "có"),
                e("n15", "n16"),
                e("n14", "n7", "không", True),
            ],
        ),
        DiagramSpec(
            "3_4_1_component_gameplay_runtime.drawio",
            "3.4 Component",
            "Component gameplay runtime",
            "3.4.1 Component gameplay runtime",
            1800,
            1000,
            [
                n("n1", "Input", 100, 300),
                n("n2", "Player", 420, 180),
                n("n3", "Enemy", 420, 420),
                n("n4", "Wave", 760, 300),
                n("n5", "ObjectPool", 1100, 180),
                n("n6", "MapThemeManager", 1100, 420),
                n("n7", "Shared Services", 1440, 300),
            ],
            [e("n1", "n2"), e("n4", "n3", "spawn"), e("n2", "n3", "combat"), e("n5", "n2"), e("n5", "n3"), e("n4", "n6"), e("n7", "n2"), e("n7", "n4")],
        ),
        DiagramSpec(
            "3_4_2_component_progression_ui.drawio",
            "3.4 Component",
            "Component progression và UI",
            "3.4.2 Component progression & UI",
            1800,
            1000,
            [
                n("n1", "PlayerLevelSystem", 220, 300),
                n("n2", "BuffCardManager", 560, 180),
                n("n3", "CardSelectionPanel", 560, 420),
                n("n4", "GameUI", 920, 300),
                n("n5", "PlayerStatsPanel", 1280, 160),
                n("n6", "PauseMenuPanel", 1280, 320),
                n("n7", "LeaderboardPanel", 1280, 480),
            ],
            [e("n1", "n2", "OnLevelUp"), e("n2", "n3", "cards"), e("n3", "n2", "selection"), e("n1", "n4", "events"), e("n4", "n5"), e("n4", "n6"), e("n4", "n7")],
        ),
        DiagramSpec(
            "3_4_3_component_backend_services.drawio",
            "3.4 Component",
            "Component backend và services",
            "3.4.3 Component backend & services",
            1800,
            1000,
            [
                n("n1", "PlayFabLeaderboardManager", 280, 300, 280, 100),
                n("n2", "PlayFab", 700, 300),
                n("n3", "NameInputPanel", 1100, 180),
                n("n4", "LeaderboardPanel", 1100, 420),
                n("n5", "LoadingUIManager", 1460, 180),
                n("n6", "AudioManager", 1460, 420),
            ],
            [e("n1", "n2", "login / score"), e("n3", "n1", "SubmitName"), e("n2", "n4", "leaderboard data"), e("n5", "n4", "transition support", True), e("n6", "n4", "UI SFX", True)],
        ),
        DiagramSpec(
            "3_5_1_class_player_progression.drawio",
            "3.5 Class",
            "Class diagram Player & Progression",
            "3.5.1 Class Player & Progression",
            2000,
            1100,
            [
                n("n1", "Singleton<T>", 120, 300, 220, 100, "note"),
                n("n2", "PlayerController", 420, 120),
                n("n3", "PlayerAttack", 420, 260),
                n("n4", "PlayerHealth", 420, 400),
                n("n5", "PlayerData", 420, 540),
                n("n6", "PlayerLevelSystem", 860, 200),
                n("n7", "BuffCardManager", 860, 380),
                n("n8", "PlayerStatsPanel", 1280, 200),
                n("n9", "CardSelectionPanel", 1280, 380),
            ],
            [e("n1", "n2"), e("n1", "n6"), e("n1", "n7"), e("n2", "n5"), e("n3", "n5"), e("n4", "n5"), e("n6", "n8", "events"), e("n6", "n9", "OnLevelUp"), e("n7", "n9", "cards")],
        ),
        DiagramSpec(
            "3_5_2_class_enemy_projectile.drawio",
            "3.5 Class",
            "Class diagram Enemy & Projectile",
            "3.5.2 Class Enemy & Projectile",
            2100,
            1100,
            [
                n("n1", "IDamageable", 120, 300, 220, 100, "note"),
                n("n2", "Enemy", 440, 300),
                n("n3", "MeleeEnemy", 760, 120),
                n("n4", "RangedEnemy", 760, 260),
                n("n5", "FlyEnemy", 760, 400),
                n("n6", "BossEnemy", 760, 540),
                n("n7", "EnemyData", 1080, 220),
                n("n8", "EnemyConfig", 1080, 420),
                n("n9", "Projectile", 1440, 300),
                n("n10", "PlayerProjectile", 1760, 180),
                n("n11", "EnemyProjectile", 1760, 320),
                n("n12", "SpiritProjectileScript", 1760, 460),
            ],
            [e("n1", "n2"), e("n2", "n3"), e("n2", "n4"), e("n2", "n5"), e("n2", "n6"), e("n7", "n2"), e("n8", "n7"), e("n9", "n10"), e("n9", "n11"), e("n9", "n12")],
        ),
        DiagramSpec(
            "3_5_3_class_ui_backend.drawio",
            "3.5 Class",
            "Class diagram UI & Backend",
            "3.5.3 Class UI & Backend",
            2100,
            1100,
            [
                n("n1", "PanelBase", 120, 300, 220, 100, "note"),
                n("n2", "ChallengePanel", 440, 120),
                n("n3", "NameInputPanel", 440, 260),
                n("n4", "LeaderboardPanel", 440, 400),
                n("n5", "PauseMenuPanel", 440, 540),
                n("n6", "GameUI", 860, 300),
                n("n7", "NPC", 1260, 180),
                n("n8", "ChallengePostNPC", 1580, 120),
                n("n9", "ChestBuffBox", 1580, 260),
                n("n10", "PlayFabLeaderboardManager", 1260, 440, 300, 100),
            ],
            [e("n1", "n2"), e("n1", "n3"), e("n1", "n4"), e("n1", "n5"), e("n6", "n2"), e("n6", "n3"), e("n6", "n4"), e("n6", "n5"), e("n7", "n8"), e("n7", "n9"), e("n10", "n3"), e("n10", "n4")],
        ),
    ]


def build_index(specs: list[DiagramSpec]) -> str:
    lines = [
        "# Draw.io Diagram Index",
        "",
        "File | Mục báo cáo | Caption đề xuất",
        "--- | --- | ---",
    ]
    for spec in specs:
        lines.append(f"{spec.filename} | {spec.report_section} | {spec.caption}")
    lines.append("")
    lines.append("Ghi chú: mỗi file chỉ chứa một sơ đồ, mở trực tiếp bằng draw.io / diagrams.net.")
    return "\n".join(lines)


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    specs = section_14_specs() + chapter3_specs()
    for spec in specs:
        write_diagram(spec)
    (OUTPUT_DIR / "INDEX.md").write_text(build_index(specs), encoding="utf-8")
    print(f"Generated {len(specs)} drawio files in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
