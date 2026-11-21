# Analytics: what we measure and how we read it

The closed test (12+ testers for 14 days, Google Play) and the soft launch are our only data before release. This
page says which events the game records, which questions they answer, and the targets we compare against. Events are
built and tested in `Logic/Analytics/GameEvents.cs`; what each one contains is listed for players in
[PRIVACY.md](PRIVACY.md). Nothing is sent before the player says yes (D-047, `Consent`).

## Where the events go today

`LocalEventLog` writes one JSON line per event to `analytics-log.jsonl` in the save folder (tester and dev builds). No
online service is connected yet: that is an owner decision (below). Until then, testers can send the file by hand.

## The events (PB-29, PB-36)

| Event | Answers |
|---|---|
| `app_started` | How many sessions, which build and language |
| `session_paused` | Session length (seconds since the game opened or came back) and how far the player got |
| `tutorial_step` | The tutorial funnel: which prompt each player reached |
| `tutorial_skipped` | Where players give up on the tutorial |
| `stage_started` / `stage_won` / `stage_lost` | The stage funnel, retries, stars, puzzle / camp / battle time, merges, Rally |
| `screen_opened` | Whether players find the Kingdom, Commanders, Quests and Shop |
| `feature_unlocked` | When each system opens for a player (PB-37) |
| `building_upgraded` | Whether gold has a use; how fast buildings go up |
| `quest_claimed` | Whether quests and chests are used |
| `commander_chosen` | Whether commanders matter to players |
| `expedition_ended` | How deep Expedition runs go and how often they are won |
| `ad_requested` / `ad_rewarded` | Rewarded ads per player and per placement (extra moves, extra Rally, shop gold) |
| `setting_changed` | Sound, music, battle speed, language |

## How to read them

1. **Retention:** D1 and D7 from `app_started` per install (the service adds an anonymous install id).
   Targets from the 2026-11-21 research: D1 35-40% or more, D7 12-15% or more, D7 / D1 about 0.35.
   Median games reach D1 about 22%; we want well above that before spending on installs.
2. **Tutorial funnel:** share of new installs that reach each `tutorial_step`, in order. A drop of more than 10
   points between two steps is the first thing to fix.
3. **Stage funnel:** share of installs that win 1-1, 1-2, ... 2-10, and the average attempts per stage. A stage
   where many players stop is either too hard or not fun; `stage_lost` says which (time, merges, Rally used).
4. **Systems:** share of players who open each screen in their first two sessions, and who upgrade a building,
   claim a quest, choose a commander, play the Daily and the Expedition.
5. **Ads:** `ad_rewarded` per daily player; 3-4 a day is a usual starting point. Too few means the offers are not
   worth it, far more means the game leans on them.
6. **Session:** `session_paused` seconds; Turkish players average about 33 minutes a session, puzzle games much less.

## Owner decision: which service

Any of these is free at our size; the choice decides the SDK to add (with its line in CREDITS.md and the data
safety form):
- **Unity Analytics** (Unity Gaming Services): no extra vendor, works with Unity 6.
- **GameAnalytics:** free, made for games, ready funnels and retention screens.
- **Firebase Analytics:** free, pairs with Crashlytics for crash reports and with AdMob.

The code sends events through `IAnalyticsSink`; plugging a service in is one new class plus the SDK.
