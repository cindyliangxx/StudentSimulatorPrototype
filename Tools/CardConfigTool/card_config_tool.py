# -*- coding: utf-8 -*-
import os
import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from tkinter import BOTH, END, LEFT, RIGHT, X, filedialog, messagebox
import tkinter as tk
from tkinter import ttk
from xml.sax.saxutils import escape


APP_TITLE = "学生模拟器卡牌快速配表工具"
VALUE_CHOICES = [15, 10, 5, -15, -10, -5]
STAT_NAMES = ["身心", "学业", "人际", "经济"]
OUTPUT_HEADERS = [
    "GroupName",
    "CardName",
    "StyleName",
    "EventScript.titleText",
    "EventScript.questionText",
    "EventScript.answerLeft",
    "EventScript.answerRight",
    "ImageFile",
    "EventScript.cardPropability",
    "EventScript.redrawBlockCnt",
    "EventScript.maxDraws",
    "StudentCard.unique",
    "Left_身心",
    "Left_学业",
    "Left_人际",
    "Left_经济",
    "Right_身心",
    "Right_学业",
    "Right_人际",
    "Right_经济",
]


def column_index(cell_ref):
    letters = "".join(ch for ch in cell_ref if ch.isalpha())
    total = 0
    for ch in letters.upper():
        total = total * 26 + ord(ch) - ord("A") + 1
    return max(total - 1, 0)


def column_name(index):
    index += 1
    out = ""
    while index:
        index, rem = divmod(index - 1, 26)
        out = chr(ord("A") + rem) + out
    return out


def read_xlsx(path):
    with open(path, "rb") as handle:
        data = handle.read()

    with zipfile.ZipFile(io_bytes(data), "r") as archive:
        shared_strings = read_shared_strings(archive)
        sheet_path = find_first_sheet_path(archive)
        if not sheet_path:
            raise ValueError("没有找到 Excel 工作表。")

        root = ET.fromstring(archive.read(sheet_path))
        ns = {"x": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
        raw_rows = []
        for row_node in root.findall(".//x:sheetData/x:row", ns):
            values = []
            for cell_node in row_node.findall("x:c", ns):
                ref = cell_node.attrib.get("r", "")
                col = column_index(ref)
                while len(values) <= col:
                    values.append("")
                values[col] = read_cell(cell_node, shared_strings, ns)
            raw_rows.append(values)

    while raw_rows and not any(str(value).strip() for value in raw_rows[0]):
        raw_rows.pop(0)
    if not raw_rows:
        return [], []

    headers = [str(value).strip() for value in raw_rows[0]]
    rows = []
    for raw in raw_rows[1:]:
        if not any(str(value).strip() for value in raw):
            continue
        row = {}
        for i, header in enumerate(headers):
            if header:
                row[header] = str(raw[i]).strip() if i < len(raw) else ""
        rows.append(row)
    return headers, rows


def io_bytes(data):
    import io

    return io.BytesIO(data)


def read_shared_strings(archive):
    if "xl/sharedStrings.xml" not in archive.namelist():
        return []
    root = ET.fromstring(archive.read("xl/sharedStrings.xml"))
    ns = {"x": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
    strings = []
    for node in root.findall("x:si", ns):
        strings.append("".join(text.text or "" for text in node.findall(".//x:t", ns)))
    return strings


def find_first_sheet_path(archive):
    names = archive.namelist()
    if "xl/workbook.xml" in names and "xl/_rels/workbook.xml.rels" in names:
        wb = ET.fromstring(archive.read("xl/workbook.xml"))
        rels = ET.fromstring(archive.read("xl/_rels/workbook.xml.rels"))
        wb_ns = {
            "x": "http://schemas.openxmlformats.org/spreadsheetml/2006/main",
            "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
        }
        rel_ns = {"r": "http://schemas.openxmlformats.org/package/2006/relationships"}
        sheet = wb.find(".//x:sheets/x:sheet", wb_ns)
        rel_id = sheet.attrib.get("{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id") if sheet is not None else ""
        for rel in rels.findall("r:Relationship", rel_ns):
            if rel.attrib.get("Id") == rel_id:
                target = rel.attrib.get("Target", "").replace("\\", "/").lstrip("/")
                return target if target.startswith("xl/") else "xl/" + target

    candidates = sorted(name for name in names if name.startswith("xl/worksheets/") and name.endswith(".xml"))
    return candidates[0] if candidates else ""


def read_cell(cell_node, shared_strings, ns):
    cell_type = cell_node.attrib.get("t", "")
    if cell_type == "inlineStr":
        return "".join(text.text or "" for text in cell_node.findall(".//x:t", ns))

    value_node = cell_node.find("x:v", ns)
    if value_node is None:
        return ""
    value = value_node.text or ""
    if cell_type == "s":
        try:
            index = int(value)
            return shared_strings[index] if 0 <= index < len(shared_strings) else ""
        except ValueError:
            return ""
    return value


def write_xlsx(path, rows):
    created = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    sheet_xml = build_sheet_xml(rows)
    files = {
        "[Content_Types].xml": CONTENT_TYPES,
        "_rels/.rels": ROOT_RELS,
        "docProps/app.xml": APP_XML,
        "docProps/core.xml": CORE_XML.format(created=created),
        "xl/workbook.xml": WORKBOOK_XML,
        "xl/_rels/workbook.xml.rels": WORKBOOK_RELS,
        "xl/styles.xml": STYLES_XML,
        "xl/worksheets/sheet1.xml": sheet_xml,
    }

    os.makedirs(os.path.dirname(path), exist_ok=True)
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, content in files.items():
            archive.writestr(name, content)


def build_sheet_xml(rows):
    xml_rows = []
    for r, row in enumerate(rows, start=1):
        cells = []
        for c, value in enumerate(row, start=1):
            ref = f"{column_name(c - 1)}{r}"
            cells.append(build_cell(ref, value, is_header=(r == 1)))
        xml_rows.append(f'<row r="{r}">{"".join(cells)}</row>')

    dimension = f"A1:{column_name(len(rows[0]) - 1)}{len(rows)}" if rows else "A1"
    return (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" '
        'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">'
        f'<dimension ref="{dimension}"/>'
        '<sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/>'
        '<selection pane="bottomLeft" activeCell="A2" sqref="A2"/></sheetView></sheetViews>'
        '<sheetFormatPr defaultRowHeight="15"/>'
        f'<sheetData>{"".join(xml_rows)}</sheetData>'
        '<autoFilter ref="' + dimension + '"/>'
        "</worksheet>"
    )


def build_cell(ref, value, is_header=False):
    style = ' s="1"' if is_header else ""
    if isinstance(value, (int, float)):
        return f'<c r="{ref}"{style}><v>{value}</v></c>'
    text = escape(str(value), {'"': "&quot;"})
    return f'<c r="{ref}" t="inlineStr"{style}><is><t>{text}</t></is></c>'


def sanitize_name(value, fallback):
    value = (value or "").strip()
    if not value:
        return fallback
    return re.sub(r'[\\/:*?"<>|]+', "_", value)


def first_existing(headers, candidates, default=-1):
    for candidate in candidates:
        for i, header in enumerate(headers):
            if header.lower() == candidate.lower():
                return i
    return default


class CardConfigTool(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title(APP_TITLE)
        self.geometry("1060x720")
        self.minsize(920, 620)

        self.source_path = tk.StringVar()
        self.output_path = tk.StringVar(value=os.path.abspath("ConfiguredCards.xlsx"))
        self.group_name = tk.StringVar(value="Student")
        self.style_name = tk.StringVar(value="cs_default")
        self.export_limit = tk.IntVar(value=10)
        self.status = tk.StringVar(value="请选择剧情 Excel。")

        self.headers = []
        self.rows = []
        self.cards = []
        self.current = 0
        self.active_choice = None
        self.active_stat = None
        self.mapping = {}

        self.build_ui()

    def build_ui(self):
        outer = ttk.Frame(self, padding=12)
        outer.pack(fill=BOTH, expand=True)

        file_box = ttk.LabelFrame(outer, text="文件")
        file_box.pack(fill=X)
        self.add_path_row(file_box, "剧情 Excel", self.source_path, self.choose_source)
        self.add_path_row(file_box, "导出 Excel", self.output_path, self.choose_output)

        defaults = ttk.Frame(file_box)
        defaults.pack(fill=X, padx=8, pady=(0, 8))
        ttk.Label(defaults, text="GroupName").pack(side=LEFT)
        ttk.Entry(defaults, textvariable=self.group_name, width=14).pack(side=LEFT, padx=(4, 12))
        ttk.Label(defaults, text="StyleName").pack(side=LEFT)
        ttk.Entry(defaults, textvariable=self.style_name, width=14).pack(side=LEFT, padx=(4, 12))
        ttk.Label(defaults, text="导出数量").pack(side=LEFT)
        ttk.Entry(defaults, textvariable=self.export_limit, width=8).pack(side=LEFT, padx=(4, 12))
        ttk.Button(defaults, text="加载表格", command=self.load_source).pack(side=LEFT)
        ttk.Button(defaults, text="导出 Excel", command=self.export).pack(side=RIGHT)

        self.mapping_box = ttk.LabelFrame(outer, text="字段对应")
        self.mapping_box.pack(fill=X, pady=(10, 0))

        body = ttk.Frame(outer)
        body.pack(fill=BOTH, expand=True, pady=(10, 0))

        self.card_box = ttk.LabelFrame(body, text="当前卡片")
        self.card_box.pack(side=LEFT, fill=BOTH, expand=True)
        self.index_label = ttk.Label(self.card_box, text="未加载")
        self.index_label.pack(anchor="w", padx=10, pady=(8, 0))
        self.title_label = ttk.Label(self.card_box, text="", font=("", 12, "bold"))
        self.title_label.pack(anchor="w", padx=10, pady=(8, 0))
        self.content_text = tk.Text(self.card_box, height=7, wrap="word")
        self.content_text.pack(fill=X, padx=10, pady=8)

        options = ttk.Frame(self.card_box)
        options.pack(fill=BOTH, expand=True, padx=10, pady=(0, 8))
        self.left_panel = self.build_option_panel(options, "左滑选择", 0)
        self.left_panel.pack(side=LEFT, fill=BOTH, expand=True, padx=(0, 6))
        self.right_panel = self.build_option_panel(options, "右滑选择", 1)
        self.right_panel.pack(side=LEFT, fill=BOTH, expand=True, padx=(6, 0))

        nav = ttk.Frame(self.card_box)
        nav.pack(fill=X, padx=10, pady=(0, 10))
        ttk.Button(nav, text="上一张", command=self.prev_card).pack(side=LEFT)
        ttk.Button(nav, text="下一张", command=self.next_card).pack(side=LEFT, padx=8)
        ttk.Label(nav, textvariable=self.status).pack(side=LEFT, padx=12)

        help_box = ttk.LabelFrame(body, text="操作")
        help_box.pack(side=RIGHT, fill="y", padx=(10, 0))
        help_text = (
            "1. 加载剧情 Excel\n"
            "2. 检查字段对应是否正确\n"
            "3. 点左/右选项里的四项数值\n"
            "4. 再点 +15/+10/+5/-15/-10/-5\n"
            "5. 不选择就是 0\n"
            "6. 点导出 Excel 保存结果\n\n"
            "导出的 Excel 可以直接给 Unity 的 Card Excel Importer 使用。"
        )
        ttk.Label(help_box, text=help_text, justify=LEFT, wraplength=220).pack(padx=10, pady=10)

    def add_path_row(self, parent, label, variable, command):
        row = ttk.Frame(parent)
        row.pack(fill=X, padx=8, pady=8)
        ttk.Label(row, text=label, width=10).pack(side=LEFT)
        ttk.Entry(row, textvariable=variable).pack(side=LEFT, fill=X, expand=True)
        ttk.Button(row, text="选择", command=command, width=8).pack(side=RIGHT, padx=(8, 0))

    def build_option_panel(self, parent, title, choice):
        panel = ttk.LabelFrame(parent, text=title)
        text = tk.Text(panel, height=3, wrap="word")
        text.pack(fill=X, padx=8, pady=8)
        setattr(self, f"choice_text_{choice}", text)
        for stat_index, stat in enumerate(STAT_NAMES):
            row = ttk.Frame(panel)
            row.pack(fill=X, padx=8, pady=3)
            button = ttk.Button(row, text=f"{stat}: 0", width=10, command=lambda ch=choice, st=stat_index: self.select_stat(ch, st))
            button.pack(side=LEFT)
            setattr(self, f"value_button_{choice}_{stat_index}", button)
            values_frame = ttk.Frame(row)
            values_frame.pack(side=LEFT, padx=6)
            setattr(self, f"value_frame_{choice}_{stat_index}", values_frame)
        return panel

    def choose_source(self):
        path = filedialog.askopenfilename(title="选择剧情 Excel", filetypes=[("Excel", "*.xlsx")])
        if path:
            self.source_path.set(path)
            base = os.path.splitext(os.path.basename(path))[0]
            self.output_path.set(os.path.join(os.path.dirname(path), base + "_ConfiguredCards.xlsx"))

    def choose_output(self):
        path = filedialog.asksaveasfilename(title="导出 Excel", defaultextension=".xlsx", filetypes=[("Excel", "*.xlsx")])
        if path:
            self.output_path.set(path)

    def load_source(self):
        path = self.source_path.get().strip()
        if not path or not os.path.exists(path):
            messagebox.showerror(APP_TITLE, "请选择存在的剧情 Excel。")
            return
        try:
            self.headers, self.rows = read_xlsx(path)
            if not self.rows:
                raise ValueError("表格中没有可用的事件行。")
            self.cards = [{"left": [0, 0, 0, 0], "right": [0, 0, 0, 0]} for _ in self.rows]
            self.current = 0
            self.guess_mapping()
            self.rebuild_mapping_ui()
            self.refresh_card()
            self.status.set(f"已加载 {len(self.rows)} 条事件。")
        except Exception as exc:
            messagebox.showerror(APP_TITLE, str(exc))

    def guess_mapping(self):
        self.mapping = {
            "id": first_existing(self.headers, ["ID", "编号", "事件ID", "事件 ID"]),
            "title": first_existing(self.headers, ["事件标题", "标题", "Title"]),
            "content": first_existing(self.headers, ["卡面文本", "卡片内容", "内容", "事件内容", "描述"]),
            "left": first_existing(self.headers, ["左滑选择", "左选项", "选项A", "选项 A"]),
            "right": first_existing(self.headers, ["右滑选择", "右选项", "选项B", "选项 B"]),
            "image": first_existing(self.headers, ["ImageFile", "图片", "图片文件", "卡图", "图片名"]),
        }

    def rebuild_mapping_ui(self):
        for child in self.mapping_box.winfo_children():
            child.destroy()
        labels = [
            ("id", "ID / 卡牌名"),
            ("title", "标题"),
            ("content", "卡面文本"),
            ("left", "左滑选择"),
            ("right", "右滑选择"),
            ("image", "图片文件"),
        ]
        options = ["<不使用>"] + self.headers
        for key, label in labels:
            row = ttk.Frame(self.mapping_box)
            row.pack(side=LEFT, padx=8, pady=8)
            ttk.Label(row, text=label).pack(anchor="w")
            combo = ttk.Combobox(row, state="readonly", width=16, values=options)
            current = self.mapping.get(key, -1)
            combo.current(0 if current < 0 else current + 1)
            combo.bind("<<ComboboxSelected>>", lambda _event, k=key, c=combo: self.update_mapping(k, c.current() - 1))
            combo.pack()

    def update_mapping(self, key, value):
        self.mapping[key] = value
        self.refresh_card()

    def mapped(self, row, key):
        index = self.mapping.get(key, -1)
        if index < 0 or index >= len(self.headers):
            return ""
        return row.get(self.headers[index], "")

    def refresh_card(self):
        if not self.rows:
            return
        row = self.rows[self.current]
        self.index_label.config(text=f"第 {self.current + 1} / {len(self.rows)} 张")
        self.title_label.config(text=self.mapped(row, "title") or "(无标题)")
        self.set_text(self.content_text, self.mapped(row, "content"))
        self.set_text(self.choice_text_0, self.mapped(row, "left"))
        self.set_text(self.choice_text_1, self.mapped(row, "right"))
        self.refresh_value_buttons()

    def set_text(self, widget, value):
        widget.config(state="normal")
        widget.delete("1.0", END)
        widget.insert("1.0", value)
        widget.config(state="disabled")

    def select_stat(self, choice, stat):
        self.active_choice = choice
        self.active_stat = stat
        self.refresh_value_buttons()

    def refresh_value_buttons(self):
        for choice in [0, 1]:
            values = self.cards[self.current]["left" if choice == 0 else "right"] if self.cards else [0, 0, 0, 0]
            for stat_index, stat in enumerate(STAT_NAMES):
                button = getattr(self, f"value_button_{choice}_{stat_index}")
                button.config(text=f"{stat}: {values[stat_index]}")
                frame = getattr(self, f"value_frame_{choice}_{stat_index}")
                for child in frame.winfo_children():
                    child.destroy()
                if self.active_choice == choice and self.active_stat == stat_index:
                    for value in VALUE_CHOICES:
                        ttk.Button(frame, text=f"{value:+d}", width=5, command=lambda v=value: self.set_value(v)).pack(side=LEFT, padx=1)
                    ttk.Button(frame, text="0", width=4, command=lambda: self.set_value(0)).pack(side=LEFT, padx=1)

    def set_value(self, value):
        if self.active_choice is None or self.active_stat is None:
            return
        key = "left" if self.active_choice == 0 else "right"
        self.cards[self.current][key][self.active_stat] = value
        self.refresh_value_buttons()

    def prev_card(self):
        if self.current > 0:
            self.current -= 1
            self.active_choice = None
            self.active_stat = None
            self.refresh_card()

    def next_card(self):
        if self.current < len(self.rows) - 1:
            self.current += 1
            self.active_choice = None
            self.active_stat = None
            self.refresh_card()

    def export(self):
        if not self.rows:
            messagebox.showerror(APP_TITLE, "请先加载剧情 Excel。")
            return
        output = self.output_path.get().strip()
        if not output:
            messagebox.showerror(APP_TITLE, "请选择导出 Excel 路径。")
            return
        try:
            limit = max(1, int(self.export_limit.get()))
            rows = [OUTPUT_HEADERS]
            for i, source_row in enumerate(self.rows[:limit]):
                rows.append(self.build_output_row(source_row, i))
            write_xlsx(output, rows)
            self.status.set(f"已导出 {len(rows) - 1} 张卡：{output}")
            messagebox.showinfo(APP_TITLE, f"导出完成：\n{output}")
        except Exception as exc:
            messagebox.showerror(APP_TITLE, str(exc))

    def build_output_row(self, source_row, index):
        source_id = self.mapped(source_row, "id")
        title = self.mapped(source_row, "title")
        card_name = sanitize_name(source_id, f"E{index + 1:03d}")
        if not title:
            title = card_name
        left = self.cards[index]["left"]
        right = self.cards[index]["right"]
        return [
            self.group_name.get().strip() or "Student",
            card_name,
            self.style_name.get().strip() or "cs_default",
            title,
            self.mapped(source_row, "content"),
            self.mapped(source_row, "left"),
            self.mapped(source_row, "right"),
            self.mapped(source_row, "image"),
            1,
            0,
            100,
            "false",
            left[0],
            left[1],
            left[2],
            left[3],
            right[0],
            right[1],
            right[2],
            right[3],
        ]


CONTENT_TYPES = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
<Default Extension="xml" ContentType="application/xml"/>
<Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
<Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>"""

ROOT_RELS = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
<Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>"""

APP_XML = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
<Application>Student Card Config Tool</Application></Properties>"""

CORE_XML = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
<dc:creator>Student Card Config Tool</dc:creator><cp:lastModifiedBy>Student Card Config Tool</cp:lastModifiedBy><dcterms:created xsi:type="dcterms:W3CDTF">{created}</dcterms:created><dcterms:modified xsi:type="dcterms:W3CDTF">{created}</dcterms:modified></cp:coreProperties>"""

WORKBOOK_XML = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
<sheets><sheet name="Cards" sheetId="1" r:id="rId1"/></sheets></workbook>"""

WORKBOOK_RELS = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>"""

STYLES_XML = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
<fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts>
<fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
<borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
<cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0"/></cellXfs>
<cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
</styleSheet>"""


if __name__ == "__main__":
    app = CardConfigTool()
    app.mainloop()
