# LINGUAFLOW DESIGN LANGUAGE CONTRACT (DESIGN.md)
*Official Living Design Specification & Style Governance for AI Agents & Engineers.*

---

## 1. Core Philosophy: "Bespoke Cyber-Academic Aesthetic"
- **Avoid Generic AI Slop**: Never apply the exact same layout or card structure across different labs.
- **Deep Space Immersion**: Ultra-dark, premium slate backdrop with glowing neon energy accents (`Teal`, `Amber`, `Indigo`, `Emerald`).
- **5 Mandatory UI States**: Every component and view MUST explicitly define and handle:
  1. `Default / Rest`: Clean visual balance, crisp contrast.
  2. `Loading / Skeleton`: Shimmering placeholder with matching layout height.
  3. `Empty State`: Meaningful illustration or mascot tip with actionable CTA.
  4. `Error / Retry`: Gentle feedback, clear recovery button.
  5. `Success / Active`: Micro-interaction, celebration fanfare or pulse glow.

---

## 2. Color System & Design Tokens
```css
/* Backgrounds */
--bg-cosmos: #020617;        /* slate-950 - Main App Backdrop */
--bg-surface: #0f172a;       /* slate-900 - Card Surface */
--bg-surface-glass: rgba(15, 23, 42, 0.85); /* backdrop-blur-2xl */
--bg-surface-elevated: #1e293b; /* slate-800 - Popover & Interactive Items */

/* Brand & Accent Matrix */
--accent-teal: #14b8a6;      /* Primary Action & Brand Glow */
--accent-emerald: #10b981;   /* Success, Mastery & A1/A2 Badges */
--accent-amber: #f59e0b;     /* Streak, XP, High Alert & B1 Badges */
--accent-coral: #f43f5e;     /* Error, Buzzer, Again rating & B2 Badges */
--accent-indigo: #6366f1;    /* Deep Focus, AI Tutor & Grammar */
--accent-purple: #a855f7;    /* Vocabulary Folders & SRS Mastery */

/* Text & Contrast Hierarchy */
--text-primary: #ffffff;     /* High-emphasis Headings */
--text-secondary: #cbd5e1;   /* slate-300 - Body & Content */
--text-muted: #94a3b8;       /* slate-400 - Subtitles & Metadata */
--text-faint: #64748b;       /* slate-500 - Inactive / Disabled */

/* Border & Dividers */
--border-subtle: rgba(51, 65, 85, 0.6);   /* slate-700/60 */
--border-highlight: rgba(20, 184, 166, 0.3); /* teal-500/30 */
--border-glow: rgba(245, 158, 11, 0.4);      /* amber-500/40 */
```

---

## 3. Typography & Hierarchy Scale
* **Display & Brand Headings**: `Cal Sans`, `Outfit`, or `Inter` Font-family, extra-bold (`font-extrabold`), tight tracking (`tracking-tight`).
* **Body Text**: `Inter` or system sans-serif, high readability line-height (`leading-relaxed`), text-sm (14px) to text-base (16px).
* **Phonetics & Code**: `JetBrains Mono` or `font-mono`, bold for IPA symbols (`/ˈvəʊkæb/`).

---

## 4. Spacing, Geometry & Glassmorphism
* **Border Radius System**:
  * Action Buttons: `rounded-2xl` (16px) or `rounded-xl` (12px).
  * Feature Cards & Modals: `rounded-3xl` (24px).
  * Mini Tags & Badges: `rounded-full` or `rounded-lg` (8px).
* **Glassmorphism Rule**:
  * Cards use `bg-slate-900/80 backdrop-blur-xl border border-slate-800/80`.
  * Active / Glowing elements use `shadow-xl shadow-teal-500/10 hover:border-teal-400/40`.

---

## 5. Micro-Animations & Sound Governance
* **Motion Springs**:
  * Subtle hover lift: `whileHover={{ scale: 1.02, y: -2 }}`.
  * Tactile tap response: `whileTap={{ scale: 0.96 }}`.
  * Transitions use spring physics (`stiffness: 400, damping: 25`).
* **Procedural Sound Synthesizer (`arcadeAudio.ts`)**:
  * Correct / Success: `playCoin()` or `playLaser()`.
  * Combo Milestones: `playCombo(multiplier)`.
  * Incorrect / Buzzer: `playBuzzer()`.
  * Stage Victory: `playVictoryFanfare()`.
  * Game Over / Retry: `playGameOver()`.

---

## 6. Anti-AI Slop Checklist (Mandatory Prior to Code Commit)
- [ ] Is this layout distinct from other labs? (Vocabulary $\ne$ Listening $\ne$ Speaking).
- [ ] Are navbar and navigation elements grouped logically without overcrowding?
- [ ] Is there proper visual breathing room (no cramped text, no cards with >7 conflicting focal points)?
- [ ] Are mobile touch targets at least 44x44px?
- [ ] Is dark mode contrast compliant with WCAG 2.2 AA (minimum 4.5:1 ratio for body text)?
