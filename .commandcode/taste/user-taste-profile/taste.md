# User Taste Profile
- Communicates in French (informal, casual register). Confidence: 0.95
- Uses very conversational, spoken-style French with slang ("Genre", "eh ben", "très chiant", "c'est quoi ce bordel"-style). Expects responses in the same casual French tone. Confidence: 0.9
- Describes bugs and issues in a narrative, experiential way ("tu vois la sidebar là où...", "je reste plus en bas, mais ça remonte en haut") rather than technical jargon. Confidence: 0.85
- Cares about UI polish and state persistence across page navigations (e.g., sidebar scroll position resetting on navigation was immediately noticed and flagged as annoying). Confidence: 0.85
- Accepts localStorage-based persistence patterns for UI state (scroll position, collapsed state). Confidence: 0.75
- Expects the assistant to investigate, fix, and verify (build/compile) changes autonomously without asking for confirmation at each step. Confidence: 0.8
- Prefers SPA-like fluid navigation over full page reloads — finds page reloads jarring and expects smooth transitions between pages (AJAX content swap, fade transitions). Confidence: 0.85
