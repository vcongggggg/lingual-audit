import { NextRequest, NextResponse } from 'next/server';

const BACKEND_API_URL = process.env.BACKEND_API_URL || 'http://localhost:5224';

interface ExplainMistakeBody {
  questionPrompt?: string;
  selectedOptionText?: string;
  correctOptionText?: string;
  level?: string;
  existingExplanation?: string;
}

function buildFallbackMistakeAnalysis(body: ExplainMistakeBody) {
  const prompt = (body.questionPrompt || '').trim();
  const selected = (body.selectedOptionText || 'Đáp án đã chọn').trim();
  const correct = (body.correctOptionText || 'Đáp án đúng').trim();
  const level = (body.level || 'A2').trim();
  const baseExp = (body.existingExplanation || '').trim();

  const lowerCorrect = correct.toLowerCase();

  let synonyms = ['accurate expression', 'natural phrasing', 'context-appropriate word'];
  let antonyms = ['incorrect form', 'opposite meaning'];
  let mnemonic = `Mẹo nhớ nhanh: Hãy gắn "${correct}" với từ khóa ngữ cảnh chính trong câu "${prompt}" để phản xạ trong 2 giây!`;
  let example = `In a real ${level} conversation, native speakers naturally use "${correct}" in this exact structure.`;

  if (lowerCorrect === 'goes' || lowerCorrect === 'does' || lowerCorrect === 'has') {
    synonyms = ['attends regularly', 'commutes', 'travels'];
    antonyms = ['stays', 'remains', 'skips'];
    mnemonic = 'He / She / It + V(s/es) ở hiện tại đơn — "Ngôi thứ 3 số ít luôn thích chữ S!"';
    example = 'She goes to the university library every morning before class.';
  } else if (lowerCorrect === 'went' || lowerCorrect === 'bought' || lowerCorrect === 'saw') {
    synonyms = ['traveled to', 'visited', 'attended'];
    antonyms = ['returned from', 'missed'];
    mnemonic = 'Gặp dấu hiệu quá khứ (yesterday, last night, ago) -> Chuyển ngay sang động từ cột 2 (V2/ed)!';
    example = 'They went to Da Nang last weekend for the tech conference.';
  } else if (lowerCorrect === 'lend') {
    synonyms = ['loan', 'advance', 'provide temporarily'];
    antonyms = ['borrow', 'take', 'keep'];
    mnemonic = 'Lend = Cho mượn đi (L = Leave); Borrow = Mượn mang về (B = Bring).';
    example = 'Could you lend me your charger for a few minutes?';
  } else if (lowerCorrect === 'resilient' || lowerCorrect === 'perseverance') {
    synonyms = ['tenacious', 'adaptable', 'strong-willed'];
    antonyms = ['fragile', 'vulnerable', 'easily discouraged'];
    mnemonic = 'Resilient = Bật dậy mạnh mẽ như lò xo sau thử thách.';
    example = 'Our engineering team remained resilient despite tight deadlines.';
  }

  return {
    questionPrompt: prompt,
    selectedOptionText: selected,
    correctOptionText: correct,
    whyWrong: `Lựa chọn "${selected}" chưa phù hợp vì không khớp với dấu hiệu ngữ pháp hoặc ngữ cảnh từ vựng của câu hỏi. Đáp án chuẩn xác phải là "${correct}".`,
    grammarAndContextRule:
      baseExp ||
      `Trong trình độ ${level}, bạn cần chú ý sự hòa hợp giữa chủ ngữ - động từ, dấu hiệu thì (tense markers) và cụm từ cố định (collocations) đi kèm với "${correct}".`,
    synonyms,
    antonyms,
    mnemonicTip: mnemonic,
    exampleSentence: example,
  };
}

export async function POST(req: NextRequest) {
  let body: ExplainMistakeBody = {};
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ error: 'Invalid JSON payload' }, { status: 400 });
  }

  try {
    const res = await fetch(`${BACKEND_API_URL}/api/v1/ai/explain-mistake`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        questionPrompt: body.questionPrompt || '',
        selectedOptionText: body.selectedOptionText || '',
        correctOptionText: body.correctOptionText || '',
        level: body.level || 'A2',
        existingExplanation: body.existingExplanation || null,
      }),
      signal: AbortSignal.timeout(7000),
    });

    if (res.ok) {
      const json = await res.json();
      const data = json?.data ?? json;
      return NextResponse.json(data);
    }
  } catch {
    // Fallback to deterministic analysis when backend is offline
  }

  return NextResponse.json(buildFallbackMistakeAnalysis(body));
}

