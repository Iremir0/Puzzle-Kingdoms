# Decision Log

Short records of decisions that shape the project. Newest at the bottom.
Format: decision — why — alternatives rejected.

## D-001 Puzzle and battle are separate phases
Why: readable on a phone, simpler to build, each phase gets full attention.
Rejected: simultaneous puzzle + battle (split attention, high complexity).

## D-002 Four gem types: Warrior, Archer, Mage, Banner
Why: three colors on 6×6 cascade almost constantly and make the army random.
Banner adds a "more units vs. more Rally" trade-off.
Rejected: 3 colors; 5 colors with Defender (scope).

## D-003 Recruitment meter (3 gems = 1 unit), cascades count fully
Why: smooth reward for bigger matches, predictable value, rewarding cascades.
Rejected: 1/2/3 units for match 3/4/5 (overflows 9 slots fast); cascades → gold
only (less exciting).

## D-004 Unlimited bench, 9 deployed, leftovers dismissed for gold
Rejected: stopping recruitment at 9; auto-merging overflow.

## D-005 No army carry-over in the campaign
Why: prevents snowballing, keeps stages balanceable. Carry-over is reserved for
the post-MVP Expedition mode.

## D-006 Manual merge, tier multipliers ×1.8 / ×3.2
Why: keeps "wide vs. tall" as a player decision. Values tuned by headless sims.

## D-007 Formation rows = depth, columns = lanes; same-lane targeting tie-break
Why: makes placement matter without pathfinding.

## D-008 Enemies follow the same rules as the player
Special behaviors (divers, healers) arrive later as taught mechanics.

## D-009 Deterministic combat, no RNG
Why: honest losses, easy testing, enables replays and seeded Daily Challenges.

## D-010 Rally: player-timed army buff, charged by Banner gems
Why: core differentiator; gives the auto battle a moment of command and links
puzzle decisions directly to battle.

## D-011 Do not fork the Kenney Match-3 Starter Kit
Why: its logic is coupled to nodes and lacks group detection, move limits and
deadlock handling. We write our own tested `BoardModel` and may reuse its CC0
art/audio as placeholders. (Code: MIT — keep the notice if any is copied.)

## D-012 Godot 4.6, Mobile renderer, GDScript, portrait 1080×1920 base
Why: Mobile renderer targets Android Vulkan devices; fall back to Compatibility
if low-end device tests fail.

## D-013 Minimal in-house test runner instead of GUT for now
Why: zero third-party code to vendor, enough for pure-logic tests. Revisit if
we need mocking, parameterized tests or scene tests.

## D-014 Art direction split
Semi-realistic quality is concentrated in 2D (portraits, cards, UI,
backgrounds). Battlefield units are mid-poly 3D with strong silhouettes, because
they render at roughly 80–120 px on a phone. Prototype uses CC0 placeholders.
