/**
 * Script biên dịch Kế hoạch Phân chia Sprint & Lộ trình (Sprint Plan & Roadmap) của Lingual từ Markdown sang Word (.docx) chuyên nghiệp.
 * Tuân thủ bộ skill docx chuẩn của Claude:
 * - docx-js (npm)
 * - Khổ trang chuẩn A4 (11906 x 16838 dxa)
 * - Dual-width cho mọi ô bảng (columnWidths + cell widths)
 * - ShadingType.CLEAR
 * - HeadingLevel chuẩn
 * - Header & Footer có số trang tự động (Page X of Y)
 * - Thiết kế trang bìa đẳng cấp doanh nghiệp
 * - Nhúng ảnh sơ đồ thực tế chất lượng cao
 */

const fs = require('fs');
const path = require('path');
const {
    Document,
    Packer,
    Paragraph,
    TextRun,
    HeadingLevel,
    Table,
    TableRow,
    TableCell,
    WidthType,
    BorderStyle,
    ShadingType,
    AlignmentType,
    Header,
    Footer,
    PageNumber,
    TabStopType,
    TabStopPosition,
    PageBreak,
    ImageRun
} = require('docx');

// Utility: Xóa sạch HTML tags và Markdown link markup
function cleanRawText(text) {
    if (!text) return '';
    let cleaned = text.replace(/<[^>]+>/g, '');
    cleaned = cleaned.replace(/\[([^\]]+)\]\([^)]+\)/g, '$1');
    cleaned = cleaned.replace(/\\rightarrow|\\longrightarrow/g, '→');
    cleaned = cleaned.replace(/\$\$|\$/g, '');
    return cleaned;
}

// Utility: Parse inline formatting (**bold**, *italic*, `code`, ==highlight==) thành TextRun[]
function parseInlineRuns(rawText, baseProps = {}) {
    const text = cleanRawText(rawText);
    const runs = [];
    const regex = /(==.*?==|\*\*.*?\*\*|\*.*?\*|`.*?`)/g;
    let lastIdx = 0;
    let match;

    while ((match = regex.exec(text)) !== null) {
        if (match.index > lastIdx) {
            const normalText = text.substring(lastIdx, match.index);
            if (normalText) {
                runs.push(new TextRun({ text: normalText, ...baseProps }));
            }
        }
        const m = match[0];
        if (m.startsWith('==') && m.endsWith('==')) {
            runs.push(new TextRun({
                text: m.slice(2, -2),
                shading: { fill: 'FEFCBF', type: ShadingType.CLEAR },
                bold: true,
                color: '744210',
                ...baseProps
            }));
        } else if (m.startsWith('**') && m.endsWith('**')) {
            runs.push(new TextRun({
                text: m.slice(2, -2),
                bold: true,
                ...baseProps
            }));
        } else if (m.startsWith('*') && m.endsWith('*') && m.length > 2) {
            runs.push(new TextRun({
                text: m.slice(1, -1),
                italics: true,
                ...baseProps
            }));
        } else if (m.startsWith('`') && m.endsWith('`')) {
            runs.push(new TextRun({
                text: m.slice(1, -1),
                font: 'Consolas',
                size: (baseProps.size || 22) - 2,
                color: '0D9488',
                ...baseProps
            }));
        }
        lastIdx = regex.lastIndex;
    }

    if (lastIdx < text.length) {
        const remaining = text.substring(lastIdx);
        if (remaining) {
            runs.push(new TextRun({ text: remaining, ...baseProps }));
        }
    }

    if (runs.length === 0) {
        runs.push(new TextRun({ text: '', ...baseProps }));
    }
    return runs;
}

// Tạo bảng DOCX chuyên nghiệp có dual width
function createDocxTable(tableData, totalWidth = 9026) {
    const numCols = Math.max(...tableData.map(r => r.length));
    if (numCols <= 0) return null;

    // Tính toán độ rộng cột tự động dựa trên độ dài nội dung
    const colMaxLens = new Array(numCols).fill(6);
    tableData.forEach(row => {
        row.forEach((cell, idx) => {
            const cleanCell = cleanRawText(cell).trim();
            colMaxLens[idx] = Math.max(colMaxLens[idx], cleanCell.length);
        });
    });

    const totalLen = colMaxLens.reduce((a, b) => a + b, 0);
    let colWidths = colMaxLens.map(len => Math.round((len / totalLen) * totalWidth));
    
    // Đảm bảo tổng độ rộng đúng bằng totalWidth (dual widths requirement)
    const assignedTotal = colWidths.reduce((a, b) => a + b, 0);
    colWidths[colWidths.length - 1] += (totalWidth - assignedTotal);

    const thinBorder = {
        style: BorderStyle.SINGLE,
        size: 4,
        color: 'CBD5E0'
    };

    const docxRows = tableData.map((row, rIdx) => {
        const isHeader = (rIdx === 0);
        const cells = [];

        for (let cIdx = 0; cIdx < numCols; cIdx++) {
            const rawCellText = (row[cIdx] || '').trim();
            const widthDxa = colWidths[cIdx];

            const cellShading = isHeader
                ? { fill: '1A365D', type: ShadingType.CLEAR } // Navy header
                : { fill: (rIdx % 2 === 1 ? 'F8FAFC' : 'FFFFFF'), type: ShadingType.CLEAR };

            const textProps = isHeader
                ? { font: 'Calibri', size: 19, bold: true, color: 'FFFFFF' }
                : { font: 'Calibri', size: 19, color: '2D3748' };

            const runs = parseInlineRuns(rawCellText, textProps);

            cells.push(new TableCell({
                width: { size: widthDxa, type: WidthType.DXA },
                shading: cellShading,
                margins: {
                    top: isHeader ? 140 : 100,
                    bottom: isHeader ? 140 : 100,
                    left: 140,
                    right: 140
                },
                borders: {
                    top: thinBorder,
                    bottom: thinBorder,
                    left: thinBorder,
                    right: thinBorder
                },
                children: [
                    new Paragraph({
                        spacing: { before: 20, after: 20, line: 240 },
                        children: runs
                    })
                ]
            }));
        }

        return new TableRow({
            tableHeader: isHeader,
            cantSplit: true,
            children: cells
        });
    });

    return new Table({
        width: { size: totalWidth, type: WidthType.DXA },
        columnWidths: colWidths,
        rows: docxRows
    });
}

// Hộp Callout ghi chú / chú ý
function createCalloutTable(lines, totalWidth = 9026) {
    const fullText = cleanRawText(lines.join(' ')).trim();
    let borderHex = '3182CE'; // Blue
    let bgHex = 'EBF8FF';
    let badgeText = '📌 GHI CHÚ QUAN TRỌNG: ';
    let badgeHex = '2B6CB0';
    let cleanText = fullText;

    if (fullText.includes('[!IMPORTANT]')) {
        borderHex = 'E53E3E'; // Red
        bgHex = 'FFF5F5';
        badgeText = '⚠️ QUAN TRỌNG: ';
        badgeHex = 'C53030';
        cleanText = fullText.replace(/\[!IMPORTANT\]/g, '').trim();
    } else if (fullText.includes('[!TIP]')) {
        borderHex = 'D69E2E'; // Gold
        bgHex = 'FFFFF0';
        badgeText = '💡 MẸO TRIỂN KHAI: ';
        badgeHex = 'B7791F';
        cleanText = fullText.replace(/\[!TIP\]/g, '').trim();
    } else if (fullText.includes('[!NOTE]')) {
        borderHex = '3182CE';
        bgHex = 'EBF8FF';
        badgeText = '📌 LƯU Ý KỸ THUẬT: ';
        badgeHex = '2B6CB0';
        cleanText = fullText.replace(/\[!NOTE\]/g, '').trim();
    }

    const badgeRun = new TextRun({
        text: badgeText,
        bold: true,
        font: 'Calibri',
        size: 20,
        color: badgeHex
    });

    const contentRuns = parseInlineRuns(cleanText, {
        font: 'Calibri',
        size: 20,
        color: '2D3748'
    });

    return new Table({
        width: { size: totalWidth, type: WidthType.DXA },
        columnWidths: [totalWidth],
        rows: [
            new TableRow({
                cantSplit: true,
                children: [
                    new TableCell({
                        width: { size: totalWidth, type: WidthType.DXA },
                        shading: { fill: bgHex, type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 180, right: 140 },
                        borders: {
                            left: { style: BorderStyle.SINGLE, size: 24, color: borderHex },
                            top: { style: BorderStyle.NONE },
                            right: { style: BorderStyle.NONE },
                            bottom: { style: BorderStyle.NONE }
                        },
                        children: [
                            new Paragraph({
                                spacing: { before: 40, after: 40, line: 260 },
                                children: [badgeRun, ...contentRuns]
                            })
                        ]
                    })
                ]
            })
        ]
    });
}

// Khối code / sơ đồ mô phỏng
function createCodeBlockTable(codeLines, totalWidth = 9026) {
    const thinBorder = {
        style: BorderStyle.SINGLE,
        size: 6,
        color: 'CBD5E0'
    };

    const paragraphs = codeLines.map(line => {
        return new Paragraph({
            spacing: { before: 0, after: 0, line: 220 },
            children: [
                new TextRun({
                    text: line || ' ',
                    font: 'Consolas',
                    size: 17, // 8.5pt
                    color: '1A202C'
                })
            ]
        });
    });

    if (paragraphs.length === 0) {
        paragraphs.push(new Paragraph({ text: '' }));
    }

    return new Table({
        width: { size: totalWidth, type: WidthType.DXA },
        columnWidths: [totalWidth],
        rows: [
            new TableRow({
                cantSplit: true,
                children: [
                    new TableCell({
                        width: { size: totalWidth, type: WidthType.DXA },
                        shading: { fill: 'F8FAFC', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        borders: {
                            top: thinBorder,
                            bottom: thinBorder,
                            left: thinBorder,
                            right: thinBorder
                        },
                        children: paragraphs
                    })
                ]
            })
        ]
    });
}

// Tạo Trang Bìa (Front Cover Page) sang trọng
function createCoverPage(totalWidth = 9026) {
    const coverParagraphs = [];

    coverParagraphs.push(new Paragraph({ spacing: { before: 600 } }));

    coverParagraphs.push(new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 120 },
        children: [
            new TextRun({
                text: 'CHƯƠNG TRÌNH PHÁT TRIỂN DỰ ÁN THỰC CHIẾN',
                font: 'Calibri',
                size: 20,
                bold: true,
                color: '4A5568'
            })
        ]
    }));

    coverParagraphs.push(new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 400 },
        children: [
            new TextRun({
                text: 'MEZON CAMPUS STUDIO 2026 — NCC+ & MEZON PLATFORM',
                font: 'Calibri',
                size: 24,
                bold: true,
                color: '1A365D'
            })
        ]
    }));

    coverParagraphs.push(new Paragraph({
        border: {
            bottom: { style: BorderStyle.DOUBLE, size: 16, color: '1A365D' }
        },
        spacing: { after: 600 }
    }));

    coverParagraphs.push(new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 300, after: 180 },
        children: [
            new TextRun({
                text: 'KẾ HOẠCH PHÂN CHIA SPRINT & LỘ TRÌNH 10 TUẦN',
                font: 'Calibri Light',
                size: 38,
                bold: true,
                color: '1A365D'
            })
        ]
    }));

    coverParagraphs.push(new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 360 },
        children: [
            new TextRun({
                text: 'SPRINT PLAN & ROADMAP SPECIFICATION (SPRINT 0 ➔ 4)',
                font: 'Calibri',
                size: 26,
                bold: true,
                color: '2B6CB0'
            })
        ]
    }));

    coverParagraphs.push(new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 200, after: 120 },
        children: [
            new TextRun({
                text: 'DỰ ÁN: LINGUAL — TEAM 05 (ĐỤT CẬN TRĨ)',
                font: 'Calibri',
                size: 32,
                bold: true,
                color: '0D9488'
            })
        ]
    }));

    coverParagraphs.push(new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 800 },
        children: [
            new TextRun({
                text: 'Kế hoạch phát triển phiên bản MVP ban đầu phục vụ mốc nộp Milestone 1 (12/10/2026)',
                font: 'Calibri',
                size: 22,
                italics: true,
                color: '4A5568'
            })
        ]
    }));

    const metaTable = new Table({
        width: { size: 7000, type: WidthType.DXA },
        columnWidths: [2600, 4400],
        alignment: AlignmentType.CENTER,
        rows: [
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Đơn vị thực hiện:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4400, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Team 05 — Đụt Cận Trĩ', bold: true, size: 20, font: 'Calibri', color: '2D3748' })] })]
                    })
                ]
            }),
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Mentor hướng dẫn:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4400, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Mai Hồng Mận (man.maihong)', bold: true, size: 20, font: 'Calibri', color: '2D3748' })] })]
                    })
                ]
            }),
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Nhóm trưởng / Lead:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4400, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Ngô Văn Công (cong.ngovan)', bold: true, size: 20, font: 'Calibri', color: '2D3748' })] })]
                    })
                ]
            }),
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Thành viên cốt lõi:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4400, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Ngô Văn Công (Lead), Nguyễn Công Minh, Phan Phước Trí', bold: true, size: 20, font: 'Calibri', color: '2D3748' })] })]
                    })
                ]
            }),
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Bộ công nghệ chính:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4400, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'ASP.NET Core 8 (Modular Monolith) + Next.js 14', bold: true, size: 20, font: 'Calibri', color: '0D9488' })] })]
                    })
                ]
            }),
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Mốc nộp Milestone 1:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4400, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: '12/10/2026 (Hoàn thành Code: Tuần 9)', bold: true, size: 20, font: 'Calibri', color: 'C53030' })] })]
                    })
                ]
            }),
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Phiên bản tài liệu:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4400, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'v1.0 (Official Milestone 1 Approval)', size: 20, font: 'Calibri', color: '2D3748' })] })]
                    })
                ]
            })
        ]
    });

    coverParagraphs.push(metaTable);

    coverParagraphs.push(new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 800 },
        children: [
            new TextRun({
                text: 'ĐÀ NẴNG, THÁNG 10 NĂM 2026',
                font: 'Calibri',
                size: 20,
                bold: true,
                color: '718096'
            })
        ]
    }));

    coverParagraphs.push(new Paragraph({
        children: [new PageBreak()]
    }));

    return coverParagraphs;
}

// Hàm chính chuyển đổi file Markdown sang Document
async function buildSprintDocx() {
    const baseDir = path.resolve(__dirname, '..');
    const mdPath = path.join(baseDir, 'docs', 'product', 'SPRINT_PLAN_LINGUAL_MCS2026.md');
    const outPath = path.join(baseDir, 'docs', 'product', 'SPRINT_PLAN_LINGUAL_MCS2026.docx');
    const assetsDir = path.join(baseDir, 'docs', 'assets');
    const timelineImgPath = path.join(assetsDir, 'diagram_sprint_timeline.png');

    console.log(`Đang đọc file Markdown: ${mdPath}`);
    const mdContent = fs.readFileSync(mdPath, 'utf-8');
    const lines = mdContent.split(/\r?\n/);

    const bodyChildren = [];
    const totalWidth = 9026;

    // 1. Thêm trang bìa
    bodyChildren.push(...createCoverPage(totalWidth));

    let inTable = false;
    let tableBuffer = [];
    let inCallout = false;
    let calloutBuffer = [];
    let inCodeBlock = false;
    let codeBuffer = [];

    function flushTable() {
        if (tableBuffer.length > 0) {
            const tbl = createDocxTable(tableBuffer, totalWidth);
            if (tbl) {
                bodyChildren.push(tbl);
                bodyChildren.push(new Paragraph({ spacing: { before: 80, after: 120 } }));
            }
            tableBuffer = [];
        }
        inTable = false;
    }

    function flushCallout() {
        if (calloutBuffer.length > 0) {
            const callout = createCalloutTable(calloutBuffer, totalWidth);
            if (callout) {
                bodyChildren.push(callout);
                bodyChildren.push(new Paragraph({ spacing: { before: 80, after: 120 } }));
            }
            calloutBuffer = [];
        }
        inCallout = false;
    }

    function flushCodeBlock() {
        if (codeBuffer.length > 0) {
            const codeTbl = createCodeBlockTable(codeBuffer, totalWidth);
            if (codeTbl) {
                bodyChildren.push(codeTbl);
                bodyChildren.push(new Paragraph({ spacing: { before: 80, after: 120 } }));
            }
            codeBuffer = [];
        }
        inCodeBlock = false;
    }

    // Bỏ qua tiêu đề H1 và block trích dẫn đầu file (vì đã có trên trang bìa)
    let passedInitialHeader = false;

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        const trimmed = line.trim();

        // Bỏ qua khối metadata ở đầu file
        if (!passedInitialHeader) {
            if (trimmed.startsWith('## CHƯƠNG 1:') || trimmed.startsWith('## 🧭 CHƯƠNG 1:')) {
                passedInitialHeader = true;
            } else {
                continue;
            }
        }

        // 1. Xử lý khối code (```)
        if (trimmed.startsWith('```')) {
            if (inCodeBlock) {
                flushCodeBlock();
            } else {
                flushTable();
                flushCallout();
                inCodeBlock = true;
                codeBuffer = [];
            }
            continue;
        }

        if (inCodeBlock) {
            codeBuffer.push(line);
            continue;
        }

        // 2. Xử lý khối bảng (| ... |)
        if (trimmed.startsWith('|') && trimmed.endsWith('|')) {
            // Bỏ qua dòng phân cách | :--- | :---: |
            if (/^\|[\s\-:]+(\|[\s\-:]+)+\|$/.test(trimmed)) {
                continue;
            }
            flushCallout();
            inTable = true;
            const cells = trimmed
                .slice(1, -1)
                .split('|')
                .map(c => c.trim().replace(/<br\s*\/?>/gi, '\n'));
            tableBuffer.push(cells);
            continue;
        } else if (inTable) {
            flushTable();
        }

        // 3. Xử lý Callout (> [!NOTE], > [!IMPORTANT], > [!TIP])
        if (trimmed.startsWith('>')) {
            flushTable();
            inCallout = true;
            calloutBuffer.push(trimmed.slice(1).trim());
            continue;
        } else if (inCallout) {
            flushCallout();
        }

        // 4. Đường phân cách ngang (---)
        if (trimmed === '---') {
            bodyChildren.push(new Paragraph({
                border: { bottom: { style: BorderStyle.SINGLE, size: 8, color: 'E2E8F0' } },
                spacing: { before: 160, after: 200 }
            }));
            continue;
        }

        // 5. Tiêu đề H2 (Chương)
        if (trimmed.startsWith('## ')) {
            const titleText = cleanRawText(trimmed.slice(3).trim());
            bodyChildren.push(new Paragraph({
                heading: HeadingLevel.HEADING_1,
                spacing: { before: 400, after: 160 },
                children: [
                    new TextRun({
                        text: titleText,
                        bold: true,
                        size: 28, // 14pt
                        font: 'Calibri',
                        color: '1A365D'
                    })
                ],
                border: {
                    bottom: { style: BorderStyle.SINGLE, size: 12, color: '2B6CB0' }
                }
            }));

            // Nếu là Chương 2, nhúng ảnh sơ đồ Timeline ngay bên dưới tiêu đề Chương 2
            if (titleText.includes('CHƯƠNG 2') || titleText.includes('LỘ TRÌNH TỔNG THỂ')) {
                if (fs.existsSync(timelineImgPath)) {
                    console.log(`Nhúng ảnh sơ đồ: ${timelineImgPath}`);
                    bodyChildren.push(new Paragraph({
                        alignment: AlignmentType.CENTER,
                        spacing: { before: 160, after: 160 },
                        children: [
                            new ImageRun({
                                data: fs.readFileSync(timelineImgPath),
                                transformation: { width: 580, height: 180 },
                                type: 'png'
                            })
                        ]
                    }));
                    bodyChildren.push(new Paragraph({
                        alignment: AlignmentType.CENTER,
                        spacing: { after: 200 },
                        children: [
                            new TextRun({
                                text: 'Hình: Sơ đồ Lộ trình 10 Tuần & 4 Sprint Phát triển Dự án LINGUAL (MCS 2026)',
                                italics: true,
                                size: 18,
                                font: 'Calibri',
                                color: '718096'
                            })
                        ]
                    }));
                }
            }
            continue;
        }

        // 6. Tiêu đề H3 (Mục con / Sprint)
        if (trimmed.startsWith('### ')) {
            const titleText = cleanRawText(trimmed.slice(4).trim());
            bodyChildren.push(new Paragraph({
                heading: HeadingLevel.HEADING_2,
                spacing: { before: 260, after: 120 },
                children: [
                    new TextRun({
                        text: titleText,
                        bold: true,
                        size: 24, // 12pt
                        font: 'Calibri',
                        color: '2B6CB0'
                    })
                ]
            }));

            // Nếu là Chương 2 hoặc Lộ trình, nhúng ảnh sơ đồ Timeline ngay bên dưới
            if (titleText.includes('LỘ TRÌNH TỔNG THỂ') || titleText.includes('10-WEEK ROADMAP') || titleText.includes('Bảng Tổng hợp Tiến độ')) {
                if (fs.existsSync(timelineImgPath)) {
                    console.log(`Nhúng ảnh sơ đồ: ${timelineImgPath}`);
                    bodyChildren.push(new Paragraph({
                        alignment: AlignmentType.CENTER,
                        spacing: { before: 160, after: 160 },
                        children: [
                            new ImageRun({
                                data: fs.readFileSync(timelineImgPath),
                                transformation: { width: 580, height: 180 },
                                type: 'png'
                            })
                        ]
                    }));
                    bodyChildren.push(new Paragraph({
                        alignment: AlignmentType.CENTER,
                        spacing: { after: 200 },
                        children: [
                            new TextRun({
                                text: 'Hình: Sơ đồ Lộ trình 10 Tuần & 4 Sprint Phát triển Dự án LINGUAL (MCS 2026)',
                                italics: true,
                                size: 18,
                                font: 'Calibri',
                                color: '718096'
                            })
                        ]
                    }));
                }
            }
            continue;
        }

        // 7. Tiêu đề H4
        if (trimmed.startsWith('#### ')) {
            const titleText = cleanRawText(trimmed.slice(5).trim());
            bodyChildren.push(new Paragraph({
                heading: HeadingLevel.HEADING_3,
                spacing: { before: 200, after: 80 },
                children: [
                    new TextRun({
                        text: titleText,
                        bold: true,
                        size: 21, // 10.5pt
                        font: 'Calibri',
                        color: '2D3748'
                    })
                ]
            }));
            continue;
        }

        // 7.1. Xử lý ảnh Markdown: ![alt](path)
        const imgMatch = trimmed.match(/^!\[(.*?)\]\((.*?)\)$/);
        if (imgMatch) {
            const altText = imgMatch[1];
            const relPath = imgMatch[2];
            const resolvedImgPath = path.resolve(path.dirname(mdPath), relPath);
            if (fs.existsSync(resolvedImgPath)) {
                console.log(`Nhúng ảnh từ Markdown: ${resolvedImgPath} (${altText})`);
                let imgHeight = 170;
                if (resolvedImgPath.includes('matrix')) {
                    imgHeight = 162;
                }
                bodyChildren.push(new Paragraph({
                    alignment: AlignmentType.CENTER,
                    spacing: { before: 180, after: 100 },
                    children: [
                        new ImageRun({
                            data: fs.readFileSync(resolvedImgPath),
                            transformation: { width: 580, height: imgHeight },
                            type: 'png'
                        })
                    ]
                }));
            }
            continue;
        }

        // 7.2. Xử lý Chú thích ảnh (Caption dạng *Hình: ...* hoặc *Figure: ...*)
        if ((trimmed.startsWith('*Hình:') || trimmed.startsWith('*Figure:')) && trimmed.endsWith('*')) {
            const capText = cleanRawText(trimmed.replace(/^\*|\*$/g, '').trim());
            bodyChildren.push(new Paragraph({
                alignment: AlignmentType.CENTER,
                spacing: { before: 0, after: 200 },
                children: [
                    new TextRun({
                        text: capText,
                        italics: true,
                        size: 18, // 9pt
                        font: 'Calibri',
                        color: '718096'
                    })
                ]
            }));
            continue;
        }

        // 8. Danh sách dấu đầu dòng (- hoặc *)
        if (trimmed.startsWith('- ') || trimmed.startsWith('* ')) {
            const bulletText = trimmed.slice(2).trim();
            const runs = parseInlineRuns(bulletText, {
                font: 'Calibri',
                size: 21,
                color: '2D3748'
            });
            bodyChildren.push(new Paragraph({
                bullet: { level: 0 },
                spacing: { before: 30, after: 30, line: 240 },
                children: runs
            }));
            continue;
        }

        // 9. Danh sách đánh số (1., 2., ...)
        const numMatch = trimmed.match(/^(\d+)\.\s+(.*)/);
        if (numMatch) {
            const numPrefix = numMatch[1];
            const itemText = numMatch[2];
            const runs = parseInlineRuns(itemText, {
                font: 'Calibri',
                size: 21,
                color: '2D3748'
            });
            bodyChildren.push(new Paragraph({
                spacing: { before: 40, after: 40, line: 240 },
                indent: { left: 360 },
                children: [
                    new TextRun({ text: `${numPrefix}.  `, bold: true, font: 'Calibri', size: 21, color: '1A365D' }),
                    ...runs
                ]
            }));
            continue;
        }

        // 10. Đoạn văn thường
        if (trimmed.length > 0) {
            const runs = parseInlineRuns(trimmed, {
                font: 'Calibri',
                size: 21,
                color: '2D3748'
            });
            bodyChildren.push(new Paragraph({
                spacing: { before: 60, after: 60, line: 260 },
                children: runs
            }));
        }
    }

    // Xả hết buffer còn sót
    flushTable();
    flushCallout();
    flushCodeBlock();

    // 11. Cấu hình Header và Footer chuyên nghiệp
    const docHeader = new Header({
        children: [
            new Paragraph({
                alignment: AlignmentType.RIGHT,
                spacing: { after: 120 },
                border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: 'CBD5E0' } },
                children: [
                    new TextRun({
                        text: 'LINGUAL — Kế hoạch Phân chia Sprint & Lộ trình 10 Tuần (MCS 2026)',
                        font: 'Calibri',
                        size: 16,
                        color: '718096'
                    })
                ]
            })
        ]
    });

    const docFooter = new Footer({
        children: [
            new Paragraph({
                tabStops: [
                    { type: TabStopType.RIGHT, position: TabStopPosition.MAX }
                ],
                border: { top: { style: BorderStyle.SINGLE, size: 6, color: 'CBD5E0' } },
                spacing: { before: 120 },
                children: [
                    new TextRun({
                        text: 'Team 05 — Đụt Cận Trĩ | Tài liệu Mật nội bộ',
                        font: 'Calibri',
                        size: 16,
                        color: '718096'
                    }),
                    new TextRun({
                        text: '\tTrang ',
                        font: 'Calibri',
                        size: 16,
                        color: '718096'
                    }),
                    new TextRun({
                        children: [PageNumber.CURRENT],
                        font: 'Calibri',
                        size: 16,
                        bold: true,
                        color: '1A365D'
                    }),
                    new TextRun({
                        text: ' / ',
                        font: 'Calibri',
                        size: 16,
                        color: '718096'
                    }),
                    new TextRun({
                        children: [PageNumber.TOTAL_PAGES],
                        font: 'Calibri',
                        size: 16,
                        bold: true,
                        color: '1A365D'
                    })
                ]
            })
        ]
    });

    // 12. Tạo Document với cấu hình Section A4
    const doc = new Document({
        styles: {
            default: {
                document: {
                    run: {
                        font: 'Calibri',
                        size: 21,
                        color: '2D3748'
                    }
                }
            }
        },
        sections: [
            {
                properties: {
                    page: {
                        size: { width: 11906, height: 16838 }, // A4
                        margin: { top: 1440, bottom: 1440, left: 1440, right: 1440 }
                    }
                },
                headers: { default: docHeader },
                footers: { default: docFooter },
                children: bodyChildren
            }
        ]
    });

    console.log(`Đang đóng gói file Word: ${outPath}...`);
    const buffer = await Packer.toBuffer(doc);
    try {
        fs.writeFileSync(outPath, buffer);
        console.log(`🎉 XUẤT FILE WORD THÀNH CÔNG! Kích thước: ${buffer.length} bytes`);
    } catch (e) {
        if (e.code === 'EBUSY' || e.code === 'EPERM') {
            const tempOut = outPath.replace('.docx', '_updated.docx');
            fs.writeFileSync(tempOut, buffer);
            console.log(`⚠️ File đang bị mở trong Word/WPS Office! Đã lưu bản cập nhật tại: ${tempOut}`);
            console.log(`👉 Hãy đóng file ${path.basename(outPath)} trong Word/WPS rồi chạy lại để ghi đè.`);
        } else {
            throw e;
        }
    }
    return outPath;
}

buildSprintDocx().catch(err => {
    console.error('❌ LỖI TRONG QUÁ TRÌNH BIÊN DỊCH:', err);
    process.exit(1);
});
