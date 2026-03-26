import struct
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from xml.sax.saxutils import escape


BASE_DIR = Path(r"D:\Codex work")
OUTPUT_PATH = BASE_DIR / "多角度测色仪上位机系统_V1.0_详细功能清单_页面原型说明_含UI示意图.docx"


TEXT_BLOCKS = [
    ("title", "多角度测色仪上位机系统 V1.0 详细功能清单与页面原型说明"),
    ("p", "生成时间：" + datetime.now().strftime("%Y-%m-%d %H:%M:%S")),
    ("h1", "一、详细功能清单"),
    ("h2", "1. 仪器连接管理"),
    ("p", "功能目标：完成设备发现、连接、状态监控和异常恢复。"),
    ("p", "功能点："),
    ("p", "1. 自动扫描可用串口。"),
    ("p", "2. 显示端口号、设备名、连接状态、驱动状态。"),
    ("p", "3. 支持手动选择端口连接。"),
    ("p", "4. 支持自动连接上次成功设备。"),
    ("p", "5. 支持断开连接。"),
    ("p", "6. 支持连接超时和错误提示。"),
    ("p", "7. 支持设备信息读取，包括设备型号、序列号、固件版本、电量或供电状态。"),
    ("p", "8. 支持连接日志查看。"),
    ("p", "9. 支持端口占用检测。"),
    ("p", "10. 支持断线自动重连。"),
    ("p", "11. 预留蓝牙搜索与连接入口。"),
    ("h2", "2. 校准管理"),
    ("p", "功能目标：保证测量前仪器处于可用状态。"),
    ("p", "功能点：显示当前校准状态，支持白板校准、黑腔校准、校准超期提醒、校准失败提示、校准记录追溯。"),
    ("h2", "3. 测量中心"),
    ("p", "功能目标：完成标准样和试样的主测量流程。"),
    ("p", "功能点：支持单次测量、平均测量、连续测量、标准样建立、试样测量、自动命名、状态提示、结果预览和重新测量。"),
    ("h2", "4. 多角度结果分析"),
    ("p", "功能目标：对多角度颜色和效果数据进行可视化和判定。"),
    ("p", "功能点：显示各角度测量结果表，展示 XYZ、Lab、LCh、ΔL、Δa、Δb、ΔC、ΔH、ΔE，支持切换 ΔE76、ΔE94、ΔE00，显示综合判定、图表和高亮超差项。"),
    ("h2", "5. 效果指标分析"),
    ("p", "功能目标：分析金属漆、珠光漆等效果色外观差异。"),
    ("p", "功能点：显示 sparkle、graininess 或 coarseness，支持与标准样对比、角度差异显示、效果指标容差设置和效果总判定。"),
    ("h2", "6. 容差与判定规则管理"),
    ("p", "功能目标：实现可配置的质控判定。"),
    ("p", "功能点：支持色差公式选择、按角度设置容差、综合容差设置、效果容差设置、模板保存、复制、编辑、启停和默认模板。"),
    ("h2", "7. 样品与颜色库管理"),
    ("p", "功能目标：统一管理标准样、试样和颜色主数据。"),
    ("p", "功能点：支持颜色库创建、标准样录入、试样录入、按编号或批次检索、查看历史测量记录、导入导出、复制标准样版本和状态管理。"),
    ("h2", "8. 报告中心"),
    ("p", "功能目标：输出用于质量判定和归档的正式报告。"),
    ("p", "功能点：支持报告预览、模板选择、Excel 导出、PDF 导出、打印、签名栏、审核栏和历史报告查询。"),
    ("h2", "9. 历史记录与追溯"),
    ("p", "功能目标：支持后续复查和问题定位。"),
    ("p", "功能点：支持按时间、样品、批次查询历史记录，查看单条测量详情、使用的标准样与容差模板、操作日志、通信异常日志和导出诊断包。"),
    ("h2", "10. 系统设置"),
    ("p", "功能目标：提供全局配置能力。"),
    ("p", "功能点：主题切换、语言切换、默认命名规则设置、默认测量模式设置、数据库路径设置、自动备份设置、日志级别设置、驱动检查入口和软件版本展示。"),
    ("h1", "二、页面原型说明"),
    ("p", "本版文档将原来的纯文字线框图替换为核心页面的初步 UI 示意图，并保留页面目标与关键交互说明，便于评审与后续视觉深化。"),
]


IMAGE_SECTIONS = [
    {
        "title": "1. 启动首页（Dashboard）",
        "desc": "页面目标：展示系统状态、设备状态、最近任务和快捷入口。关键交互：点击连接设备进入仪器连接页，点击最近任务进入结果分析页，设备未连接时高亮告警并禁用测量入口。",
        "image": BASE_DIR / "dashboard_mockup.svg",
    },
    {
        "title": "2. 仪器连接页（Instrument Connection）",
        "desc": "页面目标：完成设备发现、连接、断开、状态监控和校准入口管理。关键交互：扫描端口、识别设备、连接失败提示原因、进入校准向导、查看通信日志。",
        "image": BASE_DIR / "instrument_connection_mockup.svg",
    },
    {
        "title": "3. 测量中心页（Measurement Center）",
        "desc": "页面目标：完成标准样建立、试样测量、实时结果展示和任务保存。关键交互：测量标准样、测量试样、连续测量、自动命名、实时判定和一键进入分析页。",
        "image": BASE_DIR / "measurement_center_mockup.svg",
    },
    {
        "title": "4. 结果分析页（Result Analysis）",
        "desc": "页面目标：完成多角度颜色差异、效果差异和综合结论分析。关键交互：切换色差公式、角度高亮联动、超差原因展开、导出分析快照和生成报告。",
        "image": BASE_DIR / "result_analysis_mockup.svg",
    },
]


TAIL_BLOCKS = [
    ("h2", "5. 其他页面说明"),
    ("p", "样品管理页：用于样品主数据管理、历史记录追溯、从历史记录重新发起测量。"),
    ("p", "颜色库页：用于标准样版本管理、颜色库分类和导入导出。"),
    ("p", "容差设置页：用于维护按角度和综合维度的判定模板。"),
    ("p", "报告中心页：用于报告预览、模板切换、导出和打印。"),
    ("p", "系统设置页：用于维护语言、主题、测量参数、数据库和日志设置。"),
    ("h1", "三、主流程原型说明"),
    ("p", "流程 A：首次使用。打开软件，进入仪器连接页，扫描并连接设备，进行校准，到系统设置页确认默认参数，返回测量中心开始业务。"),
    ("p", "流程 B：建立标准样。在测量中心填写样品信息，点击新建标准样，放置标准样并测量，保存到颜色库，绑定容差模板。"),
    ("p", "流程 C：试样判定。选择已有标准样，测量试样，自动生成各角度结果，进入结果分析页查看是否超差，保存记录并导出报告。"),
    ("p", "流程 D：历史追溯。在样品管理或报告中心筛选记录，打开历史测量详情，查看原始数据、判定模板和报告，必要时重新测量或再次导出。"),
    ("h1", "四、页面间跳转关系"),
    ("p", "首页 -> 仪器连接页 -> 校准向导。"),
    ("p", "首页 -> 测量中心页 -> 结果分析页 -> 报告中心页。"),
    ("p", "样品管理页、颜色库页、容差设置页都可回跳到测量中心页。"),
    ("h1", "五、建议下一步产出"),
    ("p", "1. 数据库表设计、字段定义和表关系图。"),
    ("p", "2. 页面高保真视觉稿与组件规范。"),
    ("p", "3. 可交付研发的开发任务分解与工时估算。"),
]


def emu(px: int) -> int:
    return int(px * 9525)


def image_size(path: Path) -> tuple[int, int]:
    if path.suffix.lower() == ".png":
        data = path.read_bytes()
        if data[:8] != b"\x89PNG\r\n\x1a\n":
            raise ValueError(f"Not a PNG file: {path}")
        return struct.unpack(">II", data[16:24])
    if path.suffix.lower() == ".svg":
        text = path.read_text(encoding="utf-8")
        width = 1200
        height = 800
        width_match = None
        height_match = None
        import re
        width_match = re.search(r'width="(\d+)"', text)
        height_match = re.search(r'height="(\d+)"', text)
        if width_match:
            width = int(width_match.group(1))
        if height_match:
            height = int(height_match.group(1))
        return width, height
    raise ValueError(f"Unsupported image type: {path}")


def text_paragraph(text: str, style: str | None = None) -> str:
    style_xml = f'<w:pPr><w:pStyle w:val="{style}"/></w:pPr>' if style else ""
    return f'<w:p>{style_xml}<w:r><w:t xml:space="preserve">{escape(text)}</w:t></w:r></w:p>'


def image_paragraph(rel_id: str, doc_pr_id: int, width_px: int, height_px: int) -> str:
    max_width_px = 620
    if width_px > max_width_px:
        ratio = max_width_px / width_px
        width_px = int(width_px * ratio)
        height_px = int(height_px * ratio)
    cx = emu(width_px)
    cy = emu(height_px)
    return f"""
<w:p>
  <w:r>
    <w:drawing>
      <wp:inline distT="0" distB="0" distL="0" distR="0"
        xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing">
        <wp:extent cx="{cx}" cy="{cy}"/>
        <wp:docPr id="{doc_pr_id}" name="Picture {doc_pr_id}"/>
        <a:graphic xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
          <a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture">
            <pic:pic xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture">
              <pic:nvPicPr>
                <pic:cNvPr id="{doc_pr_id}" name="Picture {doc_pr_id}"/>
                <pic:cNvPicPr/>
              </pic:nvPicPr>
              <pic:blipFill>
                <a:blip r:embed="{rel_id}" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"/>
                <a:stretch><a:fillRect/></a:stretch>
              </pic:blipFill>
              <pic:spPr>
                <a:xfrm><a:off x="0" y="0"/><a:ext cx="{cx}" cy="{cy}"/></a:xfrm>
                <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
              </pic:spPr>
            </pic:pic>
          </a:graphicData>
        </a:graphic>
      </wp:inline>
    </w:drawing>
  </w:r>
</w:p>""".strip()


styles_xml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:default="1" w:styleId="Normal">
    <w:name w:val="Normal"/>
    <w:qFormat/>
    <w:rPr><w:sz w:val="22"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Title">
    <w:name w:val="Title"/>
    <w:basedOn w:val="Normal"/>
    <w:qFormat/>
    <w:pPr><w:jc w:val="center"/></w:pPr>
    <w:rPr><w:b/><w:sz w:val="34"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading1">
    <w:name w:val="heading 1"/>
    <w:basedOn w:val="Normal"/>
    <w:qFormat/>
    <w:rPr><w:b/><w:sz w:val="30"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading2">
    <w:name w:val="heading 2"/>
    <w:basedOn w:val="Normal"/>
    <w:qFormat/>
    <w:rPr><w:b/><w:sz w:val="26"/></w:rPr>
  </w:style>
</w:styles>"""


def main() -> None:
    media_files: list[tuple[str, Path]] = []
    rel_entries = [
        ('rId1', 'http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles', 'styles.xml')
    ]
    body_parts: list[str] = []
    doc_pr_id = 1
    rel_index = 2

    for kind, text in TEXT_BLOCKS:
        style = "Title" if kind == "title" else "Heading1" if kind == "h1" else "Heading2" if kind == "h2" else None
        body_parts.append(text_paragraph(text, style))

    for section in IMAGE_SECTIONS:
        body_parts.append(text_paragraph(section["title"], "Heading2"))
        body_parts.append(text_paragraph(section["desc"]))
        image_path = section["image"]
        rel_id = f"rId{rel_index}"
        rel_index += 1
        media_name = f"image{len(media_files) + 1}{image_path.suffix.lower()}"
        media_files.append((media_name, image_path))
        rel_entries.append((rel_id, 'http://schemas.openxmlformats.org/officeDocument/2006/relationships/image', f"media/{media_name}"))
        width_px, height_px = image_size(image_path)
        body_parts.append(image_paragraph(rel_id, doc_pr_id, width_px, height_px))
        doc_pr_id += 1
        body_parts.append(text_paragraph(""))

    for kind, text in TAIL_BLOCKS:
        style = "Heading1" if kind == "h1" else "Heading2" if kind == "h2" else None
        body_parts.append(text_paragraph(text, style))

    sect = '<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/></w:sectPr>'
    document_xml = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:wpc="http://schemas.microsoft.com/office/word/2010/wordprocessingCanvas" xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math" xmlns:v="urn:schemas-microsoft-com:vml" xmlns:wp14="http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:w10="urn:schemas-microsoft-com:office:word" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:w14="http://schemas.microsoft.com/office/word/2010/wordml" xmlns:wpg="http://schemas.microsoft.com/office/word/2010/wordprocessingGroup" xmlns:wpi="http://schemas.microsoft.com/office/word/2010/wordprocessingInk" xmlns:wne="http://schemas.microsoft.com/office/word/2006/wordml" xmlns:wps="http://schemas.microsoft.com/office/word/2010/wordprocessingShape" mc:Ignorable="w14 wp14">
  <w:body>{''.join(body_parts)}{sect}</w:body>
</w:document>"""

    relationships_xml = ['<?xml version="1.0" encoding="UTF-8" standalone="yes"?>', '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">']
    for rel_id, rel_type, target in rel_entries:
        relationships_xml.append(f'<Relationship Id="{rel_id}" Type="{rel_type}" Target="{target}"/>')
    relationships_xml.append('</Relationships>')
    document_rels_xml = "".join(relationships_xml)

    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    core_xml = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>多角度测色仪上位机系统 V1.0 详细功能清单与页面原型说明（含 UI 示意图）</dc:title>
  <dc:creator>Codex</dc:creator>
  <cp:lastModifiedBy>Codex</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">{now}</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">{now}</dcterms:modified>
</cp:coreProperties>"""

    app_xml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Codex</Application>
</Properties>"""

    content_types = [
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>',
        '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">',
        '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>',
        '<Default Extension="xml" ContentType="application/xml"/>',
        '<Default Extension="png" ContentType="image/png"/>',
        '<Default Extension="svg" ContentType="image/svg+xml"/>',
        '<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>',
        '<Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>',
        '<Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>',
        '<Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>',
        '</Types>',
    ]
    content_types_xml = "".join(content_types)

    package_rels_xml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>"""

    with zipfile.ZipFile(OUTPUT_PATH, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.writestr("[Content_Types].xml", content_types_xml.encode("utf-8"))
        zf.writestr("_rels/.rels", package_rels_xml.encode("utf-8"))
        zf.writestr("docProps/core.xml", core_xml.encode("utf-8"))
        zf.writestr("docProps/app.xml", app_xml.encode("utf-8"))
        zf.writestr("word/document.xml", document_xml.encode("utf-8"))
        zf.writestr("word/styles.xml", styles_xml.encode("utf-8"))
        zf.writestr("word/_rels/document.xml.rels", document_rels_xml.encode("utf-8"))
        for media_name, image_path in media_files:
            zf.writestr(f"word/media/{media_name}", image_path.read_bytes())

    print(str(OUTPUT_PATH))


if __name__ == "__main__":
    main()
