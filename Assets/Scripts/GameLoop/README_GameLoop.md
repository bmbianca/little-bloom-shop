# Game Loop

## Setup (once)
1. Open `Assets/Scenes/ver2_scene`.
2. Menu **Little Bloom > Build Game Loop**.
3. Save the scene (Ctrl+S). Commit the new scripts **and their .meta files**, the scene and `Assets/Materials/GameLoop`.

Running the menu again rebuilds everything (it removes the old `GameLoop` object and every `LB_` object first).

## How to play one day
1. Flip the **OPEN / CLOSED sign** on the door (grab it) -> the shop opens, the sun rises.
2. After a few seconds a new order appears in the **notebook** on the main desk.
3. Grab the notebook -> the order is stamped **TAKEN**.
4. Read the meanings on the **vase labels** or in the **encyclopedia** on the desk.
5. Make the bouquet on the wrapping table (paper, flowers, fold, ribbon).
6. Put the finished bouquet on the **main desk** and press the **desk bell**.
7. The notebook shows the score (3 / 2 / 1 stars), the board and the progress bar update, the sun moves.
8. After 3 orders: sunset and a day summary. Flip the sign to **CLOSED** -> night, the shop is tidied, vases refilled.
9. Flip the sign to OPEN again for the next day.

## Scripts
| Script | What it does |
|---|---|
| `GameLoop` | The loop: day states, orders, delivery, scoring, end of day |
| `OrderData` | One order (client, request, good flowers as `Name/Color`) |
| `BouquetEvaluator` | Counts how many flowers fit the order -> stars |
| `ScoreBoard` | Board text, shop score, unlock bar (never goes down), unlocks vases |
| `DayCycle` | Moves the Directional Light from morning to sunset, night |
| `FlowerStock` | Refills the vases, removes loose flowers at night |
| `EncyclopediaPage` | Flower meanings on the book, `???` for locked flowers |
| `ShopSign` | Flips the OPEN / CLOSED sign |
| `TouchButton` | Grab to press (notebook, bell, sign) |
| `Editor/GameLoopBuilder` | The menu that sets all of this up in the scene |

Also added: `WrappingStation.ResetStation()` and `PaperFlap.ResetFlap()`, so the table can be used again for the next bouquet.

## Tweaking (GameLoop object in the Inspector)
- **Orders**: add or edit orders. `Good Flowers` uses `Rose/Red`, or just `Tulip` for any color.
  `Required Unlocks` = how many flowers must be unlocked before the order appears.
- **Orders Per Day**, **Delay Before Order**, **Result Time**.
- **Score Board > Points Per Unlock** (3 stars = 100, 2 stars = 50, 1 star = 10).
- **Shop Sign > Shows Open In Scene**: toggle it if the sign shows the wrong side.
- Sounds are optional fields on GameLoop and on each TouchButton.
