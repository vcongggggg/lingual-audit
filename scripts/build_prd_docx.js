/**
 * Script biên dịch Tài liệu Yêu cầu Sản phẩm (PRD/SRS) của Lingual từ Markdown sang Word (.docx) chuyên nghiệp.
 * Tuân thủ bộ skill docx chuẩn của Claude:
 * - docx-js (npm)
 * - Khổ trang chuẩn A4 (11906 x 16838 dxa)
 * - Dual-width cho mọi ô bảng (columnWidths + cell widths)
 * - ShadingType.CLEAR
 * - HeadingLevel chuẩn
 * - Header & Footer có số trang tự động (Page X of Y)
 * - Thiết kế trang bìa đẳng cấp doanh nghiệp
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
    LevelFormat
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
        badgeText = '📌 LƯU Ý NGHIỆP VỤ: ';
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

    // Khoảng trống trên cùng
    coverParagraphs.push(new Paragraph({ spacing: { before: 600 } }));

    // Banner đơn vị
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

    // Đường kẻ trang trí đôi
    coverParagraphs.push(new Paragraph({
        border: {
            bottom: { style: BorderStyle.DOUBLE, size: 16, color: '1A365D' }
        },
        spacing: { after: 600 }
    }));

    // Tiêu đề tài liệu chính
    coverParagraphs.push(new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 300, after: 180 },
        children: [
            new TextRun({
                text: 'TÀI LIỆU YÊU CẦU SẢN PHẨM & ĐẶC TẢ NGHIỆP VỤ',
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
                text: 'PRODUCT REQUIREMENTS DOCUMENT (PRD / SRS v1.0)',
                font: 'Calibri',
                size: 26,
                bold: true,
                color: '2B6CB0'
            })
        ]
    }));

    // Tên dự án nổi bật
    coverParagraphs.push(new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 200, after: 120 },
        children: [
            new TextRun({
                text: 'DỰ ÁN: LINGUAL (LINGUAMEZON)',
                font: 'Calibri',
                size: 32,
                bold: true,
                color: '0D9488' // Teal
            })
        ]
    }));

    coverParagraphs.push(new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { after: 800 },
        children: [
            new TextRun({
                text: 'Nền tảng học ngoại ngữ tương tác cộng đồng trên hệ sinh thái Mezon Platform',
                font: 'Calibri',
                size: 22,
                italics: true,
                color: '4A5568'
            })
        ]
    }));

    // Bảng thông tin tác giả & phê duyệt
    const metaTable = new Table({
        width: { size: 6800, type: WidthType.DXA },
        columnWidths: [2600, 4200],
        alignment: AlignmentType.CENTER,
        rows: [
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Mentor hướng dẫn:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4200, type: WidthType.DXA },
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
                        children: [new Paragraph({ children: [new TextRun({ text: 'Chủ nhiệm đề tài / PO:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4200, type: WidthType.DXA },
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
                        children: [new Paragraph({ children: [new TextRun({ text: 'Đội ngũ thực hiện:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4200, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Team 05 — Đụt Cận Trĩ (Công, Minh, Trí)', size: 20, font: 'Calibri', color: '2D3748' })] })]
                    })
                ]
            }),
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Đơn vị đào tạo:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4200, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Khoa CNTT — Trường ĐH Bách Khoa, ĐHĐN', size: 20, font: 'Calibri', color: '2D3748' })] })]
                    })
                ]
            }),
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Đơn vị bảo trợ:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4200, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'NCC+ & Nền tảng Mezon (mezon.ai)', size: 20, font: 'Calibri', color: '2D3748' })] })]
                    })
                ]
            }),
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Trạng thái tài liệu:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4200, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Bản chính thức v1.1 — Chuẩn hóa Single-Clan & CSDL 22 Bảng Core MVP', size: 20, font: 'Calibri', bold: true, color: 'D97706' })] })]
                    })
                ]
            }),
            new TableRow({
                children: [
                    new TableCell({
                        width: { size: 2600, type: WidthType.DXA },
                        shading: { fill: 'EDF2F7', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Thời gian phát hành:', bold: true, size: 20, font: 'Calibri', color: '1A365D' })] })]
                    }),
                    new TableCell({
                        width: { size: 4200, type: WidthType.DXA },
                        shading: { fill: 'FFFFFF', type: ShadingType.CLEAR },
                        margins: { top: 120, bottom: 120, left: 160, right: 160 },
                        children: [new Paragraph({ children: [new TextRun({ text: 'Tháng 10 / Năm 2026', size: 20, font: 'Calibri', color: '2D3748' })] })]
                    })
                ]
            })
        ]
    });

    coverParagraphs.push(metaTable);
    coverParagraphs.push(new Paragraph({ spacing: { before: 800 } }));

    // Ngắt trang sau bìa
    coverParagraphs.push(new Paragraph({
        children: [new PageBreak()]
    }));

    return coverParagraphs;
}

function buildDocument() {
    const mdPath = path.resolve('c:/Study/HocKy6/MezonCampusStudio/docs/product/PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.md');
    const docxPath = path.resolve('c:/Study/HocKy6/MezonCampusStudio/docs/product/PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.docx');

    console.log(`Đang đọc file markdown từ: ${mdPath}`);
    const content = fs.readFileSync(mdPath, 'utf-8');
    const lines = content.split(/\r?\n/);

    const docChildren = [];
    const totalContentWidth = 9026; // Chuẩn A4 (11906 - 1440*2)

    // Thêm trang bìa
    docChildren.push(...createCoverPage(totalContentWidth));

    let inCode = false;
    let codeBuffer = [];

    let inTable = false;
    let tableBuffer = [];

    let inCallout = false;
    let calloutBuffer = [];

    function flushTable() {
        if (tableBuffer.length > 0) {
            const cleanRows = [];
            tableBuffer.forEach((row, idx) => {
                if (idx === 1 && row.every(c => /^[:\-\s]+$/.test(c))) {
                    return; // bỏ qua hàng separator
                }
                cleanRows.push(row);
            });
            if (cleanRows.length > 0) {
                const docxTable = createDocxTable(cleanRows, totalContentWidth);
                if (docxTable) {
                    docChildren.push(docxTable);
                    docChildren.push(new Paragraph({ spacing: { before: 0, after: 140 } }));
                }
            }
            tableBuffer = [];
        }
        inTable = false;
    }

    function flushCode() {
        if (codeBuffer.length > 0) {
            docChildren.push(createCodeBlockTable(codeBuffer, totalContentWidth));
            docChildren.push(new Paragraph({ spacing: { before: 0, after: 140 } }));
            codeBuffer = [];
        }
        inCode = false;
    }

    function flushCallout() {
        if (calloutBuffer.length > 0) {
            docChildren.push(createCalloutTable(calloutBuffer, totalContentWidth));
            docChildren.push(new Paragraph({ spacing: { before: 0, after: 140 } }));
            calloutBuffer = [];
        }
        inCallout = false;
    }

    for (let i = 0; i < lines.length; i++) {
        const rawLine = lines[i];
        const trimmed = rawLine.trim();

        // 1. Xử lý Code block / Mermaid block
        if (trimmed.startsWith('```')) {
            if (inCode) {
                flushCode();
            } else {
                flushTable();
                flushCallout();
                inCode = true;
                codeBuffer = [];
            }
            continue;
        }

        if (inCode) {
            codeBuffer.push(rawLine);
            continue;
        }

        // 2. Xử lý Bảng Markdown
        if (trimmed.startsWith('|') && trimmed.endsWith('|')) {
            flushCallout();
            inTable = true;
            const cells = trimmed.split('|').slice(1, -1).map(c => c.trim());
            tableBuffer.push(cells);
            continue;
        } else if (inTable) {
            flushTable();
        }

        // 3. Xử lý Blockquote / Callout
        if (trimmed.startsWith('>')) {
            inCallout = true;
            calloutBuffer.push(trimmed.replace(/^>\s*/, ''));
            continue;
        } else if (inCallout) {
            flushCallout();
        }

        // 4. Xử lý Dòng ngang phân cách (---) -> chuyển thành ngắt đoạn nhẹ hoặc viền
        if (trimmed === '---' || trimmed === '***') {
            docChildren.push(new Paragraph({
                border: {
                    bottom: { style: BorderStyle.SINGLE, size: 6, color: 'E2E8F0' }
                },
                spacing: { before: 180, after: 220 }
            }));
            continue;
        }

        // 5. Bỏ qua dòng trống
        if (!trimmed) {
            continue;
        }

        // 6. Xử lý Tiêu đề Heading
        if (trimmed.startsWith('# ')) {
            // Tiêu đề cấp 1 đã có ở trang bìa, ở các trang sau chuyển thành tiêu đề section lớn
            const titleText = cleanRawText(trimmed.replace(/^#\s+/, ''));
            docChildren.push(new Paragraph({
                heading: HeadingLevel.HEADING_1,
                spacing: { before: 360, after: 180 },
                children: [
                    new TextRun({
                        text: titleText,
                        bold: true,
                        font: 'Calibri',
                        size: 32,
                        color: '1A365D'
                    })
                ]
            }));
            continue;
        }

        if (trimmed.startsWith('## ')) {
            const titleText = cleanRawText(trimmed.replace(/^##\s+/, ''));
            docChildren.push(new Paragraph({
                heading: HeadingLevel.HEADING_2,
                spacing: { before: 300, after: 140 },
                border: {
                    bottom: { style: BorderStyle.SINGLE, size: 4, color: 'E2E8F0' }
                },
                children: [
                    new TextRun({
                        text: titleText,
                        bold: true,
                        font: 'Calibri',
                        size: 26,
                        color: '2B6CB0'
                    })
                ]
            }));
            continue;
        }

        if (trimmed.startsWith('### ')) {
            const titleText = cleanRawText(trimmed.replace(/^###\s+/, ''));
            docChildren.push(new Paragraph({
                heading: HeadingLevel.HEADING_3,
                spacing: { before: 220, after: 100 },
                children: [
                    new TextRun({
                        text: titleText,
                        bold: true,
                        font: 'Calibri',
                        size: 22,
                        color: '1A202C'
                    })
                ]
            }));
            continue;
        }

        if (trimmed.startsWith('#### ')) {
            const titleText = cleanRawText(trimmed.replace(/^####\s+/, ''));
            docChildren.push(new Paragraph({
                spacing: { before: 160, after: 80 },
                children: [
                    new TextRun({
                        text: titleText,
                        bold: true,
                        font: 'Calibri',
                        size: 20,
                        color: '2D3748'
                    })
                ]
            }));
            continue;
        }

        // 7. Xử lý Danh sách Bullet (* hoặc -)
        if (trimmed.startsWith('* ') || trimmed.startsWith('- ')) {
            const listText = trimmed.replace(/^[*\-]\s+/, '');
            const runs = parseInlineRuns(listText, {
                font: 'Calibri',
                size: 21,
                color: '2D3748'
            });

            docChildren.push(new Paragraph({
                spacing: { before: 40, after: 40, line: 260 },
                bullet: { level: 0 },
                indent: { left: 360, hanging: 240 },
                children: runs
            }));
            continue;
        }

        // Danh sách lồng cấp 2
        if (rawLine.startsWith('    - ') || rawLine.startsWith('  - ') || rawLine.startsWith('    * ')) {
            const listText = trimmed.replace(/^[*\-]\s+/, '');
            const runs = parseInlineRuns(listText, {
                font: 'Calibri',
                size: 20,
                color: '4A5568'
            });

            docChildren.push(new Paragraph({
                spacing: { before: 30, after: 30, line: 250 },
                bullet: { level: 1 },
                indent: { left: 720, hanging: 240 },
                children: runs
            }));
            continue;
        }

        // 8. Danh sách đánh số (1., 2., ...)
        const numMatch = trimmed.match(/^(\d+)\.\s+(.*)/);
        if (numMatch) {
            const numIndex = numMatch[1];
            const numText = numMatch[2];
            const runs = parseInlineRuns(numText, {
                font: 'Calibri',
                size: 21,
                color: '2D3748'
            });

            docChildren.push(new Paragraph({
                spacing: { before: 50, after: 50, line: 260 },
                indent: { left: 360, hanging: 240 },
                children: [
                    new TextRun({
                        text: `${numIndex}. `,
                        bold: true,
                        font: 'Calibri',
                        size: 21,
                        color: '1A365D'
                    }),
                    ...runs
                ]
            }));
            continue;
        }

        // 9. Đoạn văn bản thông thường
        const runs = parseInlineRuns(trimmed, {
            font: 'Calibri',
            size: 21,
            color: '2D3748'
        });

        docChildren.push(new Paragraph({
            spacing: { before: 60, after: 80, line: 270 },
            children: runs
        }));
    }

    // Flush any remaining buffers
    flushTable();
    flushCode();
    flushCallout();

    // Khởi tạo đối tượng Document DOCX
    const doc = new Document({
        styles: {
            default: {
                document: {
                    run: {
                        font: 'Calibri',
                        size: 21,
                        color: '2D3748'
                    },
                    paragraph: {
                        spacing: { line: 270 }
                    }
                }
            }
        },
        sections: [
            {
                properties: {
                    page: {
                        size: {
                            width: 11906,  // A4 Width
                            height: 16838  // A4 Height
                        },
                        margin: {
                            top: 1440,     // 1 inch
                            bottom: 1440,
                            left: 1440,
                            right: 1440
                        }
                    }
                },
                headers: {
                    default: new Header({
                        children: [
                            new Paragraph({
                                alignment: AlignmentType.RIGHT,
                                border: {
                                    bottom: { style: BorderStyle.SINGLE, size: 4, color: 'CBD5E0' }
                                },
                                spacing: { after: 120 },
                                children: [
                                    new TextRun({
                                        text: 'LINGUAL — PRODUCT REQUIREMENTS DOCUMENT (PRD v1.1) | MEZON CAMPUS STUDIO 2026',
                                        size: 15,
                                        font: 'Calibri',
                                        color: '718096',
                                        bold: true
                                    })
                                ]
                            })
                        ]
                    })
                },
                footers: {
                    default: new Footer({
                        children: [
                            new Paragraph({
                                border: {
                                    top: { style: BorderStyle.SINGLE, size: 4, color: 'E2E8F0' }
                                },
                                spacing: { before: 100 },
                                tabStops: [
                                    { type: TabStopType.RIGHT, position: TabStopPosition.MAX }
                                ],
                                children: [
                                    new TextRun({
                                        text: 'Tác giả: Ngô Văn Công — Khoa CNTT, ĐH Bách Khoa ĐN',
                                        size: 16,
                                        font: 'Calibri',
                                        color: '718096'
                                    }),
                                    new TextRun({
                                        text: '\tTrang ',
                                        size: 16,
                                        font: 'Calibri',
                                        color: '718096'
                                    }),
                                    new TextRun({
                                        children: [PageNumber.CURRENT],
                                        size: 16,
                                        font: 'Calibri',
                                        color: '1A365D',
                                        bold: true
                                    }),
                                    new TextRun({
                                        text: ' / ',
                                        size: 16,
                                        font: 'Calibri',
                                        color: '718096'
                                    }),
                                    new TextRun({
                                        children: [PageNumber.TOTAL_PAGES],
                                        size: 16,
                                        font: 'Calibri',
                                        color: '1A365D',
                                        bold: true
                                    })
                                ]
                            })
                        ]
                    })
                },
                children: docChildren
            }
        ]
    });

    console.log('Đang đóng gói file .docx với thư viện docx-js...');
    return Packer.toBuffer(doc).then(buffer => {
        fs.writeFileSync(docxPath, buffer);
        console.log(`✅ THÀNH CÔNG: Đã xuất file Word chuyên nghiệp tại: ${docxPath} (${buffer.length} bytes)`);
    });
}

buildDocument().catch(err => {
    console.error('❌ LỖI khi tạo file DOCX:', err);
    process.exit(1);
});
