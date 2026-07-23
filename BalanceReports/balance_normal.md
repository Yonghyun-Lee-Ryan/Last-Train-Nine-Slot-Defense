# Balance Report (sim_normal)

- Source: `simulation`
- Difficulty: `normal`
- Samples: 8
- GeneratedUtc: 2026-07-23T07:50:55.7407013Z

## Metrics
| Metric | Difficulty | Subject | Value |
|---|---|---|---:|
| win_rate | normal |  | 1 |
| reach_station_5_rate | normal |  | 1 |
| avg_simulated_seconds | normal |  | 64.012 |
| avg_remaining_hp | normal |  | 75.625 |
| avg_remaining_coins | normal |  | 155.25 |
| passenger_pick_rate | normal | passenger_office_worker | 1 |
| passenger_pick_rate | normal | passenger_delivery | 1 |
| passenger_pick_rate | normal | passenger_trainer | 1 |
| passenger_avg_damage | normal | passenger_delivery | 400.8 |
| passenger_avg_damage | normal | passenger_trainer | 228.75 |
| passenger_avg_damage | normal | passenger_office_worker | 404.7 |
| enemy_train_reach_rate | normal | enemy_normal | 4.75 |
| enemy_train_reach_rate | normal | enemy_fast | 0.125 |

## Survival Curve
- Station 1: 100.0%
- Station 2: 100.0%
- Station 3: 100.0%

## Passenger Pick vs Damage
- passenger_office_worker: pick=1, dmg=404.7
- passenger_delivery: pick=1, dmg=400.8
- passenger_trainer: pick=1, dmg=228.75

## Warnings
- **Critical**: win_rate=1 목표 [0.35,0.5] 이탈
- **Critical**: avg_remaining_hp=75.625 목표 [25,50] 이탈
- **Critical**: passenger_pick_rate=1 목표 [0.1,0.7] 이탈
- **Critical**: passenger_pick_rate=1 목표 [0.1,0.7] 이탈
- **Critical**: passenger_pick_rate=1 목표 [0.1,0.7] 이탈
- **Critical**: enemy_train_reach_rate=4.75 목표 [0,0.35] 이탈
- **Warning**: 승객 'passenger_office_worker' 픽률 100.0% — 과도한 범용성
- **Warning**: 승객 'passenger_delivery' 픽률 100.0% — 과도한 범용성
- **Warning**: 승객 'passenger_trainer' 픽률 100.0% — 과도한 범용성
