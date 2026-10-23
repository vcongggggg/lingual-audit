# LINGUAFLOW UI/UX GOVERNANCE RULES (UI_RULES.md)

### 1. Navigation & Information Architecture
- **No Navigation Overload**: Keep primary top nav to $\le 6$ core destinations. Secondary destinations (such as admin audit logs or sub-games) must live inside dropdowns, drawer sheets, or contextual tabs.
- **Hierarchical Anchoring**: The active route must always be distinctly highlighted with brand gradient tint, not just plain white text.

### 2. Cognitive Load & Density Control
- **One Primary CTA Per Screen**: Never present more than one high-intensity primary action button in the same viewport quadrant.
- **Progressive Disclosure**: Hide complex settings or secondary statistics inside accordion tabs or modal bottom sheets on mobile.

### 3. Typography & Text Formatting
- **No Orphan Words**: Use balanced text wrapping (`text-wrap: balance` / `text-pretty`) on titles.
- **Contrast Ratios**: Body text (`text-slate-300` on `slate-950`) must exceed 7:1 contrast ratio.

### 4. Mascot & Gamification Restraint
- **LingLing Mascot Rules**: Mascot must not obscure critical interactive buttons or form inputs. In mobile viewports ($< 768px$), mascot size must scale down to $\le 80px$ or sit in header bars.

### 5. Multi-Locale Consistency (`/vi` and `/en`)
- Every label, button, placeholder, error message, and tooltip must support dynamic translation based on active locale without hardcoded fallback Vietnamese strings breaking English layout.
