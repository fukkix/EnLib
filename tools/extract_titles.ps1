# 一次性：从 idleditor 的 nightTitles.ts 抽取题材标题池 → JSON。
# 用法：从仓库根目录运行
#   pwsh tools\extract_titles.ps1

$src = "G:\idleditor\src\core\lore\nightTitles.ts"
$dst = "$PSScriptRoot\..\data\seed\night_titles.json"

if (-not (Test-Path $src)) { throw "找不到源文件: $src" }
$dstDir = Split-Path $dst -Parent
if (-not (Test-Path $dstDir)) { New-Item -ItemType Directory -Path $dstDir | Out-Null }

# 把 TS 常量名映射到 NIGHT_TITLE_POOLS 里的题材 key
$genreMap = @{
    "NIGHT_SCIFI_TITLES"        = "sci-fi"
    "NIGHT_MYSTERY_TITLES"      = "mystery"
    "NIGHT_SUSPENSE_TITLES"     = "suspense"
    "NIGHT_SOCIAL_TITLES"       = "social-science"
    "NIGHT_HYBRID_TITLES"       = "hybrid"
    "NIGHT_LIGHT_NOVEL_TITLES"  = "light-novel"
}

$content = Get-Content $src -Raw -Encoding UTF8
$pools = [ordered]@{}

foreach ($constName in $genreMap.Keys) {
    $genre = $genreMap[$constName]
    # 抓 export const NAME = [ ... ]
    $pattern = "export const $constName\s*=\s*\[(?<body>[^\]]*)\]"
    $m = [regex]::Match($content, $pattern, "Singleline")
    if (-not $m.Success) {
        Write-Warning "未匹配 $constName"
        continue
    }
    $body = $m.Groups["body"].Value
    # 抓所有单引号字符串
    $titles = [regex]::Matches($body, "'([^']+)'") | ForEach-Object { $_.Groups[1].Value }
    $pools[$genre] = @($titles)
    Write-Output "  $genre : $($titles.Count) 条"
}

$json = $pools | ConvertTo-Json -Depth 4
Set-Content -Path $dst -Value $json -Encoding UTF8
Write-Output ""
Write-Output "已写入 $dst （$($pools.Values | ForEach-Object { $_.Count } | Measure-Object -Sum | Select-Object -ExpandProperty Sum) 条）"
