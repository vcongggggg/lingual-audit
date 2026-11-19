const MATCHES = Number(process.env.ARENA_MATCHES || 10000);
const QUESTIONS = 5;
const startedAt = performance.now();
let processedEvents = 0;

for (let match = 0; match < MATCHES; match += 1) {
  let playerScore = 0;
  let opponentScore = 0;
  let playerProgress = 0;
  let opponentProgress = 0;

  for (let question = 0; question < QUESTIONS; question += 1) {
    const playerCorrect = (match + question) % 3 !== 0;
    const opponentCorrect = (match + question) % 4 !== 0;
    playerProgress += 1;
    opponentProgress += 1;
    playerScore += playerCorrect ? 1 : 0;
    opponentScore += opponentCorrect ? 1 : 0;
    processedEvents += 2;
  }

  if (playerProgress !== QUESTIONS || opponentProgress !== QUESTIONS) {
    throw new Error(`Progress invariant failed for match ${match}`);
  }
  if (playerScore > QUESTIONS || opponentScore > QUESTIONS) {
    throw new Error(`Score invariant failed for match ${match}`);
  }
}

const elapsedMs = performance.now() - startedAt;
const eventsPerSecond = Math.round(processedEvents / Math.max(elapsedMs / 1000, 0.001));
console.log(JSON.stringify({
  matches: MATCHES,
  processedEvents,
  elapsedMs: Number(elapsedMs.toFixed(2)),
  eventsPerSecond,
  status: "passed",
}));
