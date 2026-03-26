import zipfile
from datetime import datetime, timezone
from xml.sax.saxutils import escape


OUTPUT_PATH = r"D:\Codex work\多角度测色仪上位机系统_V1.0_详细功能清单_页面原型说明.docx"
TITLE = "多角度测色仪上位机系统 V1.0 详细功能清单与页面原型说明"

PARAGRAPHS = [
    ("title", TITLE),
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
    ("p", "7. 支持设备信息读取。内容包括：设备型号、序列号、固件版本、电量或供电状态。"),
    ("p", "8. 支持连接日志查看。"),
    ("p", "9. 支持端口占用检测。"),
    ("p", "10. 支持断线自动重连。"),
    ("p", "11. 预留蓝牙搜索与连接入口。"),
    ("h2", "2. 校准管理"),
    ("p", "功能目标：保证测量前仪器处于可用状态。"),
    ("p", "功能点："),
    ("p", "1. 显示当前校准状态。"),
    ("p", "2. 支持白板校准。"),
    ("p", "3. 支持黑腔校准。"),
    ("p", "4. 支持校准超期提醒。"),
    ("p", "5. 支持校准失败原因提示。"),
    ("p", "6. 记录校准时间、操作人和结果。"),
    ("p", "7. 支持查看最近一次校准记录。"),
    ("h2", "3. 测量中心"),
    ("p", "功能目标：完成标准样和试样的主测量流程。"),
    ("p", "功能点："),
    ("p", "1. 支持选择测量模式。内容包括：单次测量、平均测量、连续测量。"),
    ("p", "2. 支持设置平均次数和测量间隔。"),
    ("p", "3. 支持建立标准样。"),
    ("p", "4. 支持引用已有标准样。"),
    ("p", "5. 支持试样测量。"),
    ("p", "6. 支持自动命名标准样和试样。"),
    ("p", "7. 支持测量中状态提示。内容包括：等待放样、测量中、成功、失败。"),
    ("p", "8. 支持测量完成后自动计算结果。"),
    ("p", "9. 支持重新测量。"),
    ("p", "10. 支持保存前预览当前结果。"),
    ("h2", "4. 多角度结果分析"),
    ("p", "功能目标：对多角度颜色和效果数据进行可视化和判定。"),
    ("p", "功能点："),
    ("p", "1. 显示各角度测量结果表。"),
    ("p", "2. 显示 XYZ / Lab / LCh。"),
    ("p", "3. 显示 ΔL / Δa / Δb / ΔC / ΔH / ΔE。"),
    ("p", "4. 支持切换色差公式。内容包括：ΔE76、ΔE94、ΔE00。"),
    ("p", "5. 支持按角度显示判定结果。"),
    ("p", "6. 支持综合判定结果。"),
    ("p", "7. 支持图表展示。内容包括：角度对比图、趋势图、综合色差图。"),
    ("p", "8. 支持原始数据与计算结果切换。"),
    ("p", "9. 支持异常角度高亮显示。"),
    ("p", "10. 支持复制结果和导出截图。"),
    ("h2", "5. 效果指标分析"),
    ("p", "功能目标：分析金属漆、珠光漆等效果色外观差异。"),
    ("p", "功能点："),
    ("p", "1. 显示 sparkle 指标。"),
    ("p", "2. 显示 graininess 或 coarseness 指标。"),
    ("p", "3. 支持与标准样对比。"),
    ("p", "4. 支持按角度显示效果差异。"),
    ("p", "5. 支持效果指标容差设置。"),
    ("p", "6. 支持效果总判定。"),
    ("h2", "6. 容差与判定规则管理"),
    ("p", "功能目标：实现可配置的质控判定。"),
    ("p", "功能点："),
    ("p", "1. 支持选择色差公式。"),
    ("p", "2. 支持按角度设置容差上下限。"),
    ("p", "3. 支持综合容差设置。"),
    ("p", "4. 支持效果容差设置。"),
    ("p", "5. 支持容差模板保存。"),
    ("p", "6. 支持模板复制、编辑、启停。"),
    ("p", "7. 支持默认模板设置。"),
    ("p", "8. 支持判定结果分级。内容包括：通过、警告、不通过。"),
    ("p", "9. 支持针对不同产品类型套用不同模板。"),
    ("h2", "7. 样品与颜色库管理"),
    ("p", "功能目标：统一管理标准样、试样和颜色主数据。"),
    ("p", "功能点："),
    ("p", "1. 支持颜色库创建。"),
    ("p", "2. 支持标准样录入。"),
    ("p", "3. 支持试样录入。"),
    ("p", "4. 支持按编号、名称、批次、材质检索。"),
    ("p", "5. 支持查看历史测量记录。"),
    ("p", "6. 支持样品备注和附件说明。"),
    ("p", "7. 支持导入导出颜色库。"),
    ("p", "8. 支持复制标准样生成新版本。"),
    ("p", "9. 支持作废和启用状态管理。"),
    ("h2", "8. 报告中心"),
    ("p", "功能目标：输出用于质量判定和归档的正式报告。"),
    ("p", "功能点："),
    ("p", "1. 支持报告预览。"),
    ("p", "2. 支持选择报告模板。"),
    ("p", "3. 支持导出 Excel。"),
    ("p", "4. 支持导出 PDF。"),
    ("p", "5. 支持打印。"),
    ("p", "6. 支持报告内展示样品信息、角度数据、图表和结论。"),
    ("p", "7. 支持签名栏和审核栏。"),
    ("p", "8. 支持企业 Logo 和页眉页脚配置。"),
    ("p", "9. 支持历史报告查询。"),
    ("h2", "9. 历史记录与追溯"),
    ("p", "功能目标：支持后续复查和问题定位。"),
    ("p", "功能点："),
    ("p", "1. 支持按时间、样品、批次查询历史记录。"),
    ("p", "2. 支持查看单条测量详情。"),
    ("p", "3. 支持查看使用的标准样与容差模板。"),
    ("p", "4. 支持查看操作日志。"),
    ("p", "5. 支持查看通信异常日志。"),
    ("p", "6. 支持导出诊断包。"),
    ("h2", "10. 系统设置"),
    ("p", "功能目标：提供全局配置能力。"),
    ("p", "功能点："),
    ("p", "1. 主题切换。"),
    ("p", "2. 语言切换。"),
    ("p", "3. 默认命名规则设置。"),
    ("p", "4. 默认测量模式设置。"),
    ("p", "5. 数据库存储路径设置。"),
    ("p", "6. 自动备份设置。"),
    ("p", "7. 日志级别设置。"),
    ("p", "8. 驱动安装检查入口。"),
    ("p", "9. 软件版本信息展示。"),
    ("h1", "二、页面原型说明"),
    ("h2", "1. 启动首页"),
    ("p", "页面目标：作为软件总入口，展示设备状态和快捷操作。"),
    ("p", "页面布局：顶部区域、左侧导航栏、中间主区域、底部状态栏。"),
    ("p", "主要控件：设备状态卡片、快捷按钮、最近记录列表。"),
    ("p", "交互说明：点击连接设备进入仪器连接页；点击最近记录可直接打开详情；如果设备未连接，首页顶部显示明显告警。"),
    ("h2", "2. 仪器连接页"),
    ("p", "页面目标：管理设备接入和通信状态。"),
    ("p", "页面布局：左侧设备列表区、右侧设备详情区、底部操作区。"),
    ("p", "主要控件：串口列表表格、连接参数区、设备信息卡、通信日志框。"),
    ("p", "交互说明：点击扫描刷新设备列表；点击连接后进入连接中状态；连接成功后显示绿色状态标识；端口被占用时弹出明确错误提示；点击校准可进入校准流程弹窗。"),
    ("h2", "3. 校准弹窗"),
    ("p", "页面目标：引导用户完成白板或黑腔校准。"),
    ("p", "页面布局：顶部步骤提示、中间操作说明图文、底部操作按钮。"),
    ("p", "交互说明：分步骤提示放置白板或黑腔；校准成功显示时间与结果；校准失败时显示原因和重试建议。"),
    ("h2", "4. 测量中心页"),
    ("p", "页面目标：完成标准样与试样的完整测量。"),
    ("p", "页面布局：左侧任务信息区、中间测量操作区、右侧实时结果区、底部快捷操作区。"),
    ("p", "主要控件：样品基本信息表单、标准样区域、试样区域、参数设置区域、实时数据表。"),
    ("p", "交互说明：未连接仪器时禁止测量按钮；未建立标准样时试样测量给出提醒；测量完成后自动跳出合格判定摘要；点击进入分析页带入本次记录。"),
    ("h2", "5. 结果分析页"),
    ("p", "页面目标：展示本次或历史记录的详细分析结果。"),
    ("p", "页面布局：顶部信息栏、左侧结果表区、右侧图表区、底部摘要区。"),
    ("p", "主要控件：角度结果表、公式切换下拉框、图表切换标签、结论摘要卡。"),
    ("p", "交互说明：切换色差公式时自动刷新结果；点击某个角度可高亮对应图表数据；超差项用红色显示；支持导出当前分析截图和数据表。"),
    ("h2", "6. 样品管理页"),
    ("p", "页面目标：管理试样和标准样主数据。"),
    ("p", "页面布局：顶部检索区、中间列表区、右侧详情区、底部操作区。"),
    ("p", "主要控件：样品列表表格、详情面板、历史记录子表。"),
    ("p", "交互说明：点击样品后右侧自动加载详情；支持从样品详情直接发起重新测量；作废样品默认不删除，只标记状态。"),
    ("h2", "7. 颜色库页"),
    ("p", "页面目标：管理标准颜色与标准样集合。"),
    ("p", "页面布局：左侧颜色库树、中间标准样列表、右侧标准样详情和测量记录。"),
    ("p", "主要控件：颜色库树形结构、标准样列表、操作按钮。"),
    ("p", "交互说明：选择颜色库后刷新标准样列表；支持对标准样版本做比对；支持导出整个颜色库。"),
    ("h2", "8. 容差设置页"),
    ("p", "页面目标：配置和维护判定规则。"),
    ("p", "页面布局：左侧模板列表、中间角度容差配置表、右侧公式与综合判定配置。"),
    ("p", "主要控件：模板列表、角度容差表、综合容差配置、公式参数区、操作按钮。"),
    ("p", "交互说明：修改后需显式保存；模板启用前需二次确认；支持恢复默认参数。"),
    ("h2", "9. 报告中心页"),
    ("p", "页面目标：统一输出和管理报告。"),
    ("p", "页面布局：左侧记录筛选区、中间报告预览区、右侧模板参数区。"),
    ("p", "主要控件：记录列表、模板选择器、预览面板、导出按钮。"),
    ("p", "交互说明：选择记录后自动生成预览；切换模板时即时刷新；导出成功后记录导出历史。"),
    ("h2", "10. 系统设置页"),
    ("p", "页面目标：统一配置系统行为。"),
    ("p", "页面布局：左侧设置分类、右侧配置表单。"),
    ("p", "设置分类：常规设置、测量设置、数据设置、日志设置、关于系统。"),
    ("p", "交互说明：修改关键设置时提示重启生效；数据库路径变更前需校验权限；支持一键备份和一键恢复。"),
    ("h1", "三、主流程原型说明"),
    ("p", "流程 A：首次使用。打开软件，进入仪器连接页，扫描并连接设备，进行校准，到系统设置页确认默认参数，返回测量中心开始业务。"),
    ("p", "流程 B：建立标准样。在测量中心填写样品信息，点击新建标准样，放置标准样并测量，保存到颜色库，绑定容差模板。"),
    ("p", "流程 C：试样判定。选择已有标准样，测量试样，自动生成各角度结果，进入结果分析页查看是否超差，保存记录并导出报告。"),
    ("p", "流程 D：历史追溯。在样品管理或报告中心筛选记录，打开历史测量详情，查看原始数据、判定模板和报告，必要时重新测量或再次导出。"),
    ("h1", "四、页面间跳转关系"),
    ("p", "1. 首页 -> 仪器连接页"),
    ("p", "2. 首页 -> 测量中心页"),
    ("p", "3. 仪器连接页 -> 校准弹窗"),
    ("p", "4. 测量中心页 -> 结果分析页"),
    ("p", "5. 测量中心页 -> 样品管理页"),
    ("p", "6. 样品管理页 -> 测量中心页"),
    ("p", "7. 结果分析页 -> 报告中心页"),
    ("p", "8. 颜色库页 -> 测量中心页"),
    ("p", "9. 容差设置页 -> 测量中心页"),
    ("h1", "五、建议下一步产出"),
    ("p", "1. 数据库表设计、字段定义和表关系图。"),
    ("p", "2. 页面线框图说明和开发任务拆分清单。"),
    ("p", "建议优先继续做页面线框图说明和开发任务拆分清单，便于直接进入研发拆解。"),
]


def make_paragraph(text: str, style: str | None = None) -> str:
    style_xml = f'<w:pPr><w:pStyle w:val="{style}"/></w:pPr>' if style else ""
    return f'<w:p>{style_xml}<w:r><w:t xml:space="preserve">{escape(text)}</w:t></w:r></w:p>'


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
    <w:rPr><w:b/><w:sz w:val="32"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading1">
    <w:name w:val="heading 1"/>
    <w:basedOn w:val="Normal"/>
    <w:qFormat/>
    <w:rPr><w:b/><w:sz w:val="28"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading2">
    <w:name w:val="heading 2"/>
    <w:basedOn w:val="Normal"/>
    <w:qFormat/>
    <w:rPr><w:b/><w:sz w:val="24"/></w:rPr>
  </w:style>
</w:styles>"""

content_types_xml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>"""

rels_xml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>"""

doc_rels_xml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>"""

now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
core_xml = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>{escape(TITLE)}</dc:title>
  <dc:creator>Codex</dc:creator>
  <cp:lastModifiedBy>Codex</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">{now}</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">{now}</dcterms:modified>
</cp:coreProperties>"""

app_xml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Codex</Application>
</Properties>"""

body = []
for kind, text in PARAGRAPHS:
    if kind == "title":
        body.append(make_paragraph(text, "Title"))
    elif kind == "h1":
        body.append(make_paragraph(text, "Heading1"))
    elif kind == "h2":
        body.append(make_paragraph(text, "Heading2"))
    else:
        body.append(make_paragraph(text))

body_xml = "".join(body) + '<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/></w:sectPr>'

document_xml = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:wpc="http://schemas.microsoft.com/office/word/2010/wordprocessingCanvas" xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math" xmlns:v="urn:schemas-microsoft-com:vml" xmlns:wp14="http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:w10="urn:schemas-microsoft-com:office:word" xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:w14="http://schemas.microsoft.com/office/word/2010/wordml" xmlns:wpg="http://schemas.microsoft.com/office/word/2010/wordprocessingGroup" xmlns:wpi="http://schemas.microsoft.com/office/word/2010/wordprocessingInk" xmlns:wne="http://schemas.microsoft.com/office/word/2006/wordml" xmlns:wps="http://schemas.microsoft.com/office/word/2010/wordprocessingShape" mc:Ignorable="w14 wp14">
  <w:body>{body_xml}</w:body>
</w:document>"""

with zipfile.ZipFile(OUTPUT_PATH, "w", zipfile.ZIP_DEFLATED) as zf:
    zf.writestr("[Content_Types].xml", content_types_xml.encode("utf-8"))
    zf.writestr("_rels/.rels", rels_xml.encode("utf-8"))
    zf.writestr("docProps/core.xml", core_xml.encode("utf-8"))
    zf.writestr("docProps/app.xml", app_xml.encode("utf-8"))
    zf.writestr("word/document.xml", document_xml.encode("utf-8"))
    zf.writestr("word/styles.xml", styles_xml.encode("utf-8"))
    zf.writestr("word/_rels/document.xml.rels", doc_rels_xml.encode("utf-8"))

print(OUTPUT_PATH)
