param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

Add-Type -AssemblyName System.Drawing

function Ensure-Dir([string]$Path) {
    if (-not (Test-Path $Path)) { New-Item -ItemType Directory -Force -Path $Path | Out-Null }
}

function New-Bitmap([int]$W, [int]$H) {
    return New-Object System.Drawing.Bitmap $W, $H, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
}

function Get-G([System.Drawing.Bitmap]$bmp) {
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
    return $g
}

function Save-Bmp([System.Drawing.Bitmap]$bmp, [string]$Path) {
    Ensure-Dir (Split-Path $Path -Parent)
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function Fill-RoundedRect($g, $rect, $radius, $fill, $outline) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = [Math]::Max(1, $radius * 2)
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $brush = New-Object System.Drawing.SolidBrush $fill
    $g.FillPath($brush, $path)
    $brush.Dispose()
    if ($outline.A -gt 0) {
        $pen = New-Object System.Drawing.Pen $outline, 3
        $g.DrawPath($pen, $path)
        $pen.Dispose()
    }
    $path.Dispose()
}

function Draw-Character($g, [int]$W, [int]$H, $clothes, $accent, [int]$frame, [bool]$attack) {
    $bob = [Math]::Sin($frame * [Math]::PI * 0.5) * 4
    $cx = $W / 2.0
    $cy = $H * 0.42 + $bob
    $skin = [System.Drawing.Color]::FromArgb(255, 240, 210, 180)
    $outline = [System.Drawing.Color]::FromArgb(255, 11, 18, 32)
    $brushSkin = New-Object System.Drawing.SolidBrush $skin
    $pen = New-Object System.Drawing.Pen $outline, 3
    $g.FillEllipse($brushSkin, [int]($cx - 34), [int]($cy + 4), 68, 68)
    $g.DrawEllipse($pen, [int]($cx - 34), [int]($cy + 4), 68, 68)
    Fill-RoundedRect $g ([System.Drawing.Rectangle]::FromLTRB([int]($cx - 38), [int]($cy - 10), [int]($cx + 38), [int]($cy + 60))) 16 $clothes $outline
    $armX = if ($attack) { [int]($cx + 24 + $frame * 4) } else { [int]($cx + 10 + $frame * 3) }
    $armY = if ($attack) { [int]($cy + 10) } else { [int]($cy + 20) }
    $brushAccent = New-Object System.Drawing.SolidBrush $accent
    $g.FillEllipse($brushAccent, $armX - 8, $armY - 8, 16, 16)
    $brushAccent.Dispose(); $brushSkin.Dispose(); $pen.Dispose()
}

function Save-CharacterFrame([string]$Path, $clothes, $accent, [int]$frame, [bool]$attack) {
    $bmp = New-Bitmap 256 256
    $g = Get-G $bmp
    Draw-Character $g 256 256 $clothes $accent $frame $attack
    $g.Dispose()
    Save-Bmp $bmp $Path
}

function Save-CharacterSheet([string]$Path, $clothes, $accent, [string]$state) {
    $sheet = New-Bitmap 1024 256
    $sg = Get-G $sheet
    for ($i = 0; $i -lt 4; $i++) {
        $frame = New-Bitmap 256 256
        $fg = Get-G $frame
        $attack = ($state -eq "attack") -or ($state -eq "skill" -and $i -gt 1)
        Draw-Character $fg 256 256 $clothes $accent $i $attack
        $fg.Dispose()
        $sg.DrawImage($frame, $i * 256, 0)
        $frame.Dispose()
    }
    $sg.Dispose()
    Save-Bmp $sheet $Path
}

function Draw-Enemy($g, [int]$Size, $accent, [int]$frame) {
    $sway = [Math]::Sin($frame * [Math]::PI * 0.5) * 5
    $body = [System.Drawing.Color]::FromArgb(255, [Math]::Min(255, $accent.R + 20), [Math]::Min(255, $accent.G + 20), [Math]::Min(255, $accent.B + 20))
    Fill-RoundedRect $g ([System.Drawing.Rectangle]::FromLTRB([int]($Size * 0.2 + $sway), [int]($Size * 0.25), [int]($Size * 0.8 + $sway), [int]($Size * 0.85))) ([int]($Size * 0.12)) $body ([System.Drawing.Color]::FromArgb(255, 11, 18, 32))
    $brush = New-Object System.Drawing.SolidBrush $accent
    $g.FillEllipse($brush, [int]($Size / 2 - 20 + $sway), [int]($Size * 0.15), 40, 40)
    $brush.Dispose()
}

function Save-EnemySheet([string]$Path, $accent, [int]$Size, [int]$Count) {
    $sheet = New-Bitmap ($Size * $Count) $Size
    $sg = Get-G $sheet
    for ($i = 0; $i -lt $Count; $i++) {
        $frame = New-Bitmap $Size $Size
        $fg = Get-G $frame
        Draw-Enemy $fg $Size $accent $i
        $fg.Dispose()
        $sg.DrawImage($frame, $i * $Size, 0)
        $frame.Dispose()
    }
    $sg.Dispose()
    Save-Bmp $sheet $Path
}

function Save-BurstSheet([string]$Path, $color, [int]$Size) {
    $sheet = New-Bitmap ($Size * 4) $Size
    $sg = Get-G $sheet
    for ($i = 0; $i -lt 4; $i++) {
        $frame = New-Bitmap $Size $Size
        $fg = Get-G $frame
        $alpha = 255 - $i * 40
        $c = [System.Drawing.Color]::FromArgb($alpha, $color.R, $color.G, $color.B)
        $brush = New-Object System.Drawing.SolidBrush $c
        $radius = [int]($Size * (0.15 + $i * 0.12))
        $fg.FillEllipse($brush, $Size / 2 - $radius, $Size / 2 - $radius, $radius * 2, $radius * 2)
        $brush.Dispose(); $fg.Dispose()
        $sg.DrawImage($frame, $i * $Size, 0)
        $frame.Dispose()
    }
    $sg.Dispose()
    Save-Bmp $sheet $Path
}

$artRoot = Join-Path $ProjectRoot "Assets/Art/Sprites"
Ensure-Dir (Join-Path $artRoot "UI")
Ensure-Dir (Join-Path $artRoot "Environment")
Ensure-Dir (Join-Path $artRoot "Characters")
Ensure-Dir (Join-Path $artRoot "Enemies")
Ensure-Dir (Join-Path $artRoot "Projectiles")
Ensure-Dir (Join-Path $artRoot "VFX")

$navy = [System.Drawing.Color]::FromArgb(255, 26, 39, 68)
$teal = [System.Drawing.Color]::FromArgb(255, 45, 212, 191)
$orange = [System.Drawing.Color]::FromArgb(255, 249, 115, 22)
$gold = [System.Drawing.Color]::FromArgb(255, 251, 191, 36)
$green = [System.Drawing.Color]::FromArgb(255, 34, 197, 94)
$red = [System.Drawing.Color]::FromArgb(255, 239, 68, 68)
$slate = [System.Drawing.Color]::FromArgb(255, 51, 65, 85)
$outline = [System.Drawing.Color]::FromArgb(255, 11, 18, 32)

$bg = New-Bitmap 540 960
$bgG = Get-G $bg
Fill-RoundedRect $bgG ([System.Drawing.Rectangle]::FromLTRB(0, 0, 540, 960)) 0 $navy ([System.Drawing.Color]::FromArgb(0,0,0,0))
Fill-RoundedRect $bgG ([System.Drawing.Rectangle]::FromLTRB(0, 880, 540, 900)) 0 $teal ([System.Drawing.Color]::FromArgb(0,0,0,0))
$bgG.Dispose(); Save-Bmp $bg (Join-Path $artRoot "Environment/subway_background.png")

foreach ($pair in @(
    @("panel.png", 256, 128, $slate), @("button_normal.png", 240, 80, $teal),
    @("button_pressed.png", 240, 80, ([System.Drawing.Color]::FromArgb(255,20,184,166))),
    @("button_disabled.png", 240, 80, ([System.Drawing.Color]::FromArgb(255,71,85,105))),
    @("card_frame.png", 280, 360, ([System.Drawing.Color]::FromArgb(255,15,23,42))),
    @("popup_dim.png", 64, 64, ([System.Drawing.Color]::FromArgb(180,11,18,32))),
    @("hp_bar_fill.png", 256, 32, $green), @("hp_bar_bg.png", 256, 32, ([System.Drawing.Color]::FromArgb(255,15,23,42))),
    @("boss_hp_bar_fill.png", 256, 32, $red), @("main_menu_title.png", 640, 180, ([System.Drawing.Color]::FromArgb(255,15,23,42))),
    @("result_victory_banner.png", 720, 160, $green), @("result_defeat_banner.png", 720, 160, ([System.Drawing.Color]::FromArgb(255,220,38,38))),
    @("star_frame_1.png", 220, 160, ([System.Drawing.Color]::FromArgb(255,71,85,105))),
    @("star_frame_2.png", 220, 160, ([System.Drawing.Color]::FromArgb(255,248,250,252))),
    @("star_frame_3.png", 220, 160, $gold)
)) {
    $bmp = New-Bitmap $pair[1] $pair[2]
    $g = Get-G $bmp
    Fill-RoundedRect $g ([System.Drawing.Rectangle]::FromLTRB(0, 0, $pair[1], $pair[2])) 16 $pair[3] $outline
    $g.Dispose(); Save-Bmp $bmp (Join-Path $artRoot "UI/$($pair[0])")
}

$lane = New-Bitmap 540 220
$lg = Get-G $lane
Fill-RoundedRect $lg ([System.Drawing.Rectangle]::FromLTRB(20, 40, 520, 180)) 20 $slate $teal
$lg.Dispose(); Save-Bmp $lane (Join-Path $artRoot "Environment/spawn_lane.png")

foreach ($name in @("seat_frame", "seat_highlight")) {
    $bmp = New-Bitmap 220 160
    $g = Get-G $bmp
    $fill = if ($name -eq "seat_highlight") { [System.Drawing.Color]::FromArgb(90, 45, 212, 191) } else { [System.Drawing.Color]::FromArgb(140, 15, 23, 42) }
    Fill-RoundedRect $g ([System.Drawing.Rectangle]::FromLTRB(8, 8, 212, 152)) 14 $fill $slate
    $g.Dispose(); Save-Bmp $bmp (Join-Path $artRoot "Environment/$name.png")
}

foreach ($icon in @("icon_coin", "icon_station", "icon_wave", "icon_ready", "icon_speed", "icon_pause", "icon_summon", "icon_sell", "icon_reroll", "icon_ad", "icon_ability", "icon_synergy")) {
    $bmp = New-Bitmap 64 64
    $g = Get-G $bmp
    $brush = New-Object System.Drawing.SolidBrush $teal
    $g.FillEllipse($brush, 8, 8, 48, 48)
    $brush.Dispose(); $g.Dispose()
    Save-Bmp $bmp (Join-Path $artRoot "UI/$icon.png")
}

$passengers = @(
    @("passenger_office_worker", ([System.Drawing.Color]::FromArgb(255,100,116,139)), ([System.Drawing.Color]::FromArgb(255,148,163,184))),
    @("passenger_delivery", ([System.Drawing.Color]::FromArgb(255,249,115,22)), ([System.Drawing.Color]::FromArgb(255,251,146,60))),
    @("passenger_trainer", ([System.Drawing.Color]::FromArgb(255,239,68,68)), ([System.Drawing.Color]::FromArgb(255,248,113,113))),
    @("passenger_nurse", ([System.Drawing.Color]::FromArgb(255,236,72,153)), ([System.Drawing.Color]::FromArgb(255,244,114,182))),
    @("passenger_developer", ([System.Drawing.Color]::FromArgb(255,99,102,241)), ([System.Drawing.Color]::FromArgb(255,129,140,248))),
    @("passenger_graduate", ([System.Drawing.Color]::FromArgb(255,139,92,246)), ([System.Drawing.Color]::FromArgb(255,167,139,250)))
)
foreach ($p in $passengers) {
    Save-CharacterFrame (Join-Path $artRoot "Characters/$($p[0])_portrait.png") $p[1] $p[2] 0 $false
    foreach ($state in @("idle", "attack", "skill")) {
        Save-CharacterSheet (Join-Path $artRoot "Characters/$($p[0])_$state`_sheet.png") $p[1] $p[2] $state
    }
}

Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_normal_move_sheet.png") ([System.Drawing.Color]::FromArgb(255,132,204,22)) 128 4
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_normal_hit_sheet.png") ([System.Drawing.Color]::FromArgb(255,132,204,22)) 128 2
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_normal_death_sheet.png") ([System.Drawing.Color]::FromArgb(255,132,204,22)) 128 4
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_fast_move_sheet.png") ([System.Drawing.Color]::FromArgb(255,6,182,212)) 128 4
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_fast_hit_sheet.png") ([System.Drawing.Color]::FromArgb(255,6,182,212)) 128 2
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_fast_death_sheet.png") ([System.Drawing.Color]::FromArgb(255,6,182,212)) 128 4
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_tank_move_sheet.png") ([System.Drawing.Color]::FromArgb(255,120,113,108)) 128 4
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_tank_hit_sheet.png") ([System.Drawing.Color]::FromArgb(255,120,113,108)) 128 2
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_tank_death_sheet.png") ([System.Drawing.Color]::FromArgb(255,120,113,108)) 128 4
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_boss_drunk_manager_move_sheet.png") ([System.Drawing.Color]::FromArgb(255,185,28,28)) 256 4
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_boss_drunk_manager_hit_sheet.png") ([System.Drawing.Color]::FromArgb(255,185,28,28)) 256 2
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_boss_drunk_manager_death_sheet.png") ([System.Drawing.Color]::FromArgb(255,185,28,28)) 256 4
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_boss_drunk_manager_cast_sheet.png") ([System.Drawing.Color]::FromArgb(255,185,28,28)) 256 4
Save-EnemySheet (Join-Path $artRoot "Enemies/enemy_boss_drunk_manager_enraged_sheet.png") ([System.Drawing.Color]::FromArgb(255,239,68,68)) 256 4

foreach ($proj in @("projectile_default", "projectile_office_worker", "projectile_delivery", "projectile_trainer", "projectile_nurse", "projectile_developer", "projectile_graduate", "projectile_turret")) {
    $bmp = New-Bitmap 32 32
    $g = Get-G $bmp
    Fill-RoundedRect $g ([System.Drawing.Rectangle]::FromLTRB(2, 2, 30, 30)) 8 $gold $outline
    $g.Dispose(); Save-Bmp $bmp (Join-Path $artRoot "Projectiles/$proj.png")
}

foreach ($vfx in @(
    @("vfx_hit", $slate, 48), @("vfx_crit", $orange, 64), @("vfx_death", $red, 56), @("vfx_coin", $gold, 40),
    @("vfx_summon", $teal, 52), @("vfx_merge", $gold, 60), @("vfx_sell", $slate, 44), @("vfx_knockback", ([System.Drawing.Color]::FromArgb(255,56,189,248)), 72),
    @("vfx_heal", $green, 56), @("vfx_turret_spawn", ([System.Drawing.Color]::FromArgb(255,99,102,241)), 48), @("vfx_aoe", ([System.Drawing.Color]::FromArgb(255,139,92,246)), 80),
    @("vfx_boss_enrage", $red, 72), @("vfx_boss_portal", ([System.Drawing.Color]::FromArgb(255,185,28,28)), 64), @("vfx_debuff_pulse", ([System.Drawing.Color]::FromArgb(255,220,38,38)), 96)
)) { Save-BurstSheet (Join-Path $artRoot "VFX/$($vfx[0])_sheet.png") $vfx[1] $vfx[2] }

Write-Output "Generated MVP art PNGs under $artRoot"
