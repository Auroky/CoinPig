# Project conventions

All future code must follow the object-oriented architecture taught in `E:/Wendy/Homework/NTU/Semester1/Coding/11`. Read the matching source lesson when a design decision is unclear.

- A class owns a coherent responsibility and its valid state. Separate independently changing rules, presentation, input and storage; do not create one class per method.
- Entry points assemble and connect collaborators. Entities and services own their work and validation.
- Keep game rules independent of Godot nodes, screen coordinates, UI strings and rendering. Presentation maps domain outcomes into text and visuals.
- Model genuine is-a, has-a and uses-a relationships. Prefer composition when there is no truthful subtype contract.
- Use data for new variants with the same behaviour. Use a focused abstraction for genuinely variable behaviour; avoid growing central type switches.
- Interfaces describe capabilities that clients actually need. Every implementation must honour the same inputs, outputs and side effects; no fake or unsupported operations.
- Protect mutable state with private setters and commands. Failed operations must not partially mutate state or charge twice.
- Keep names grounded in the game and the responsibility. Do not name runtime code after requests, prompts, AI, assignment stages, visual reference products, or implementation iterations.
- Runtime UI contains game information only. Coursework evidence, AI logs and design analysis belong in documentation. Test scenarios belong in Tests and must not ship in release builds.
- Preserve unrelated local changes and the user's Git state. Recheck branch, worktree and running editor before editing.
- Run relevant rule checks and rendered scene checks after changes. Explain changes using source location, reason, plausible change impact and the resulting responsibility boundary.

Key sources: SOLID Design Principles pp. 14–25, 29, 36–39, 49–50, 60–61, 65–82; Vending Machine pp. 2, 30, 38, 43–52; Class Basics pp. 39–43 and 51; OODP Relationships pp. 4, 12, 20, 61.
