$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path (Join-Path $root "Assets"))) {
    $root = "C:\Users\donggggas\Last-Train-Nine-Slot-Defense"
}

$srcAudio = Join-Path $root "_tmp_audio\interface_extract\Audio"
$outSfx = Join-Path $root "Assets\Art\Audio\Sfx"
$outBgm = Join-Path $root "Assets\Art\Audio\Bgm"
New-Item -ItemType Directory -Force -Path $outSfx | Out-Null
New-Item -ItemType Directory -Force -Path $outBgm | Out-Null

$map = @{
    "ui_click.ogg" = "click_002.ogg"
    "ui_confirm.ogg" = "confirmation_002.ogg"
    "ui_cancel.ogg" = "back_002.ogg"
    "ui_error.ogg" = "error_003.ogg"
    "ui_open.ogg" = "open_002.ogg"
    "ui_close.ogg" = "close_002.ogg"
    "ui_toggle.ogg" = "toggle_001.ogg"
    "summon_open.ogg" = "maximize_004.ogg"
    "summon_select.ogg" = "select_004.ogg"
    "shop_buy.ogg" = "confirmation_004.ogg"
    "pause.ogg" = "minimize_004.ogg"
    "resume.ogg" = "maximize_003.ogg"
    "reward.ogg" = "pluck_002.ogg"
    "switch.ogg" = "switch_002.ogg"
}

foreach ($k in $map.Keys) {
    $from = Join-Path $srcAudio $map[$k]
    $to = Join-Path $outSfx $k
    if (Test-Path $from) {
        Copy-Item $from $to -Force
        Write-Host "SFX $k"
    }
    else {
        Write-Host "MISSING $($map[$k])"
    }
}

$menuBgm = Join-Path $root "_tmp_audio\bgm_menu.ogg"
if (Test-Path $menuBgm) {
    Copy-Item $menuBgm (Join-Path $outBgm "bgm_menu.ogg") -Force
}

function Write-Wav([string]$path, [float[]]$samples, [int]$sampleRate = 22050) {
    $bytes = New-Object byte[] ($samples.Length * 2)
    for ($i = 0; $i -lt $samples.Length; $i++) {
        $v = [Math]::Max(-1.0, [Math]::Min(1.0, [double]$samples[$i]))
        $s = [int][Math]::Round($v * 32767.0)
        $bytes[2 * $i] = $s -band 0xFF
        $bytes[2 * $i + 1] = ($s -shr 8) -band 0xFF
    }

    $dataSize = $bytes.Length
    $stream = [System.IO.File]::Create($path)
    $bw = New-Object System.IO.BinaryWriter($stream)
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("RIFF"))
    $bw.Write([int](36 + $dataSize))
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("WAVE"))
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("fmt "))
    $bw.Write([int]16)
    $bw.Write([int16]1)
    $bw.Write([int16]1)
    $bw.Write([int]$sampleRate)
    $bw.Write([int]($sampleRate * 2))
    $bw.Write([int16]2)
    $bw.Write([int16]16)
    $bw.Write([System.Text.Encoding]::ASCII.GetBytes("data"))
    $bw.Write([int]$dataSize)
    $bw.Write($bytes)
    $bw.Close()
}

function Get-Tone([float]$freq, [float]$dur, [float]$vol = 0.4, [int]$sr = 22050, [string]$shape = "sine") {
    $n = [int]($dur * $sr)
    $samples = New-Object float[] $n
    for ($i = 0; $i -lt $n; $i++) {
        $t = $i / [double]$sr
        $env = 1.0
        if ($t -lt 0.01) { $env = $t / 0.01 }
        elseif ($t -gt ($dur - 0.05)) { $env = [Math]::Max(0.0, ($dur - $t) / 0.05) }

        $phase = 2.0 * [Math]::PI * $freq * $t
        $wave = 0.0
        if ($shape -eq "square") {
            $wave = if ([Math]::Sin($phase) -ge 0) { 1.0 } else { -1.0 }
        }
        elseif ($shape -eq "noise") {
            $wave = (Get-Random -Minimum -100 -Maximum 100) / 100.0
        }
        else {
            $wave = [Math]::Sin($phase)
        }

        $samples[$i] = [float]($wave * $vol * $env)
    }
    return ,$samples
}

function Get-Mix([object[]]$parts) {
    $len = 0
    foreach ($p in $parts) {
        if ($p.Length -gt $len) { $len = $p.Length }
    }
    $out = New-Object float[] $len
    foreach ($p in $parts) {
        for ($i = 0; $i -lt $p.Length; $i++) {
            $out[$i] = [float]($out[$i] + $p[$i])
        }
    }
    for ($i = 0; $i -lt $len; $i++) {
        $out[$i] = [float]([Math]::Max(-1.0, [Math]::Min(1.0, $out[$i])))
    }
    return ,$out
}

Write-Wav (Join-Path $outSfx "combat_hit.wav") (Get-Mix @((Get-Tone 420 0.08 0.35), (Get-Tone 180 0.12 0.25 -shape "square")))
Write-Wav (Join-Path $outSfx "combat_crit.wav") (Get-Mix @((Get-Tone 660 0.1 0.4), (Get-Tone 990 0.12 0.3), (Get-Tone 220 0.15 0.2)))
Write-Wav (Join-Path $outSfx "enemy_death.wav") (Get-Mix @((Get-Tone 160 0.25 0.35 -shape "square"), (Get-Tone 90 0.3 0.25)))
Write-Wav (Join-Path $outSfx "train_damage.wav") (Get-Mix @((Get-Tone 70 0.35 0.45 -shape "square"), (Get-Tone 40 0.4 0.3 -shape "noise")))
Write-Wav (Join-Path $outSfx "coin.wav") (Get-Mix @((Get-Tone 880 0.08 0.35), (Get-Tone 1320 0.12 0.3)))
Write-Wav (Join-Path $outSfx "merge.wav") (Get-Mix @((Get-Tone 523 0.1 0.3), (Get-Tone 659 0.12 0.3), (Get-Tone 784 0.18 0.35)))
Write-Wav (Join-Path $outSfx "wave_start.wav") (Get-Mix @((Get-Tone 220 0.15 0.3), (Get-Tone 330 0.2 0.28), (Get-Tone 440 0.25 0.25)))
Write-Wav (Join-Path $outSfx "station_clear.wav") (Get-Mix @((Get-Tone 392 0.15 0.3), (Get-Tone 523 0.18 0.3), (Get-Tone 659 0.25 0.35)))
Write-Wav (Join-Path $outSfx "victory.wav") (Get-Mix @((Get-Tone 523 0.2 0.3), (Get-Tone 659 0.2 0.3), (Get-Tone 784 0.25 0.35), (Get-Tone 1046 0.35 0.3)))
Write-Wav (Join-Path $outSfx "defeat.wav") (Get-Mix @((Get-Tone 220 0.3 0.35), (Get-Tone 185 0.35 0.3), (Get-Tone 146 0.45 0.35)))
Write-Wav (Join-Path $outSfx "boss_spawn.wav") (Get-Mix @((Get-Tone 55 0.5 0.4 -shape "square"), (Get-Tone 110 0.4 0.25), (Get-Tone 40 0.55 0.2 -shape "noise")))

$sr = 22050
$dur = 16.0
$n = [int]($dur * $sr)
$bgm = New-Object float[] $n
for ($i = 0; $i -lt $n; $i++) {
    $t = $i / [double]$sr
    $drone = 0.12 * [Math]::Sin(2 * [Math]::PI * 55 * $t) + 0.08 * [Math]::Sin(2 * [Math]::PI * 82.5 * $t)
    $pulse = 0.05 * [Math]::Sin(2 * [Math]::PI * 110 * $t) * (0.5 + 0.5 * [Math]::Sin(2 * [Math]::PI * 0.25 * $t))
    $tick = 0.0
    if (($i % [int]($sr * 0.5)) -lt 800) {
        $tick = 0.03 * [Math]::Sin(2 * [Math]::PI * 220 * $t)
    }
    $bgm[$i] = [float]([Math]::Max(-1.0, [Math]::Min(1.0, $drone + $pulse + $tick)))
}
Write-Wav (Join-Path $outBgm "bgm_battle.wav") $bgm $sr

$n2 = [int](12 * $sr)
$res = New-Object float[] $n2
for ($i = 0; $i -lt $n2; $i++) {
    $t = $i / [double]$sr
    $res[$i] = [float](0.1 * [Math]::Sin(2 * [Math]::PI * 196 * $t) + 0.08 * [Math]::Sin(2 * [Math]::PI * 247 * $t) + 0.06 * [Math]::Sin(2 * [Math]::PI * 294 * $t))
}
Write-Wav (Join-Path $outBgm "bgm_result.wav") $res $sr

$credits = @"
Audio Credits (Last Train Nine Slot Defense)
===========================================

1) Kenney Interface Sounds
   Author: Kenney (www.kenney.nl)
   License: CC0 1.0 Universal (public domain dedication)
   Source: https://opengameart.org/content/interface-sounds
   Files used (renamed under Assets/Art/Audio/Sfx):
   - ui_*.ogg, summon_*.ogg, shop_buy.ogg, pause.ogg, resume.ogg, reward.ogg, switch.ogg

2) Background Music 1 (menu)
   Author: Tozan
   License: CC0 1.0
   Source: https://opengameart.org/content/background-music-1
   File: Assets/Art/Audio/Bgm/bgm_menu.ogg

3) Original procedural audio (authored for this project)
   License: CC0 / public domain equivalent for this repository
   Files: combat_*.wav, enemy_death.wav, train_damage.wav, coin.wav, merge.wav,
          wave_start.wav, station_clear.wav, victory.wav, defeat.wav, boss_spawn.wav,
          bgm_battle.wav, bgm_result.wav
"@
Set-Content -Path (Join-Path $root "Assets\Art\Audio\CREDITS.txt") -Value $credits -Encoding UTF8

Write-Host "SFX count: $((Get-ChildItem $outSfx).Count)"
Write-Host "BGM count: $((Get-ChildItem $outBgm).Count)"
Write-Host "DONE"
