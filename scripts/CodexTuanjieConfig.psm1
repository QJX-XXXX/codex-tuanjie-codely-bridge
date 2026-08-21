Set-StrictMode -Version Latest

function ConvertTo-TomlString {
    param([Parameter(Mandatory)][string]$Value)

    return $Value.Replace('\', '\\').Replace('"', '\"')
}

function Get-Newline {
    param([AllowEmptyString()][string]$Text)

    $crlf = [string]([char]13) + [char]10
    if ($Text.Contains($crlf)) {
        return $crlf
    }
    return [string][char]10
}

function Split-LinesPreserveTrailingEmpty {
    param([AllowEmptyString()][string]$Text)

    return $Text -split '\r\n|\n|\r'
}

function Test-ByteEqual {
    param(
        [Parameter(Mandatory)][string]$LeftPath,
        [Parameter(Mandatory)][string]$RightPath
    )

    if ((Get-Item -LiteralPath $LeftPath).Length -ne (Get-Item -LiteralPath $RightPath).Length) {
        return $false
    }
    $left = [IO.File]::ReadAllBytes($LeftPath)
    $right = [IO.File]::ReadAllBytes($RightPath)
    for ($index = 0; $index -lt $left.Length; $index++) {
        if ($left[$index] -ne $right[$index]) {
            return $false
        }
    }
    return $true
}

function Get-TuanjieProjectInfo {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ProjectPath)

    try {
        $root = (Resolve-Path -LiteralPath $ProjectPath -ErrorAction Stop).Path
    }
    catch {
        throw "团结项目路径不存在或不可访问：$ProjectPath"
    }

    foreach ($directory in @('Assets', 'Packages', 'ProjectSettings')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $directory) -PathType Container)) {
            throw "不是有效的团结项目：缺少 $directory 目录。"
        }
    }

    $versionPath = Join-Path $root 'ProjectSettings\ProjectVersion.txt'
    $manifestPath = Join-Path $root 'Packages\manifest.json'
    if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
        throw '不是有效的团结项目：缺少 ProjectSettings/ProjectVersion.txt。'
    }
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw '不是有效的团结项目：缺少 Packages/manifest.json。'
    }

    $versionText = [IO.File]::ReadAllText($versionPath)
    $editorMatch = [regex]::Match($versionText, '(?m)^\s*m_EditorVersion:\s*(?<value>\S.*?)\s*$')
    $tuanjieMatch = [regex]::Match($versionText, '(?m)^\s*m_TuanjieEditorVersion:\s*(?<value>\S.*?)\s*$')
    if (-not $editorMatch.Success -or -not $tuanjieMatch.Success) {
        throw '不是有效的团结项目：ProjectVersion.txt 缺少团结版本标识。'
    }

    try {
        $manifest = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
    }
    catch {
        throw "无法解析 Packages/manifest.json：$($_.Exception.Message)"
    }
    $bridge = $null
    if ($null -ne $manifest.dependencies) {
        $bridgeProperty = $manifest.dependencies.PSObject.Properties['cn.tuanjie.codely.bridge']
        if ($null -ne $bridgeProperty) {
            $bridge = [string]$bridgeProperty.Value
        }
    }
    if ([string]::IsNullOrWhiteSpace($bridge)) {
        throw '不是有效的团结项目：Packages/manifest.json 缺少 cn.tuanjie.codely.bridge。'
    }

    return [pscustomobject]@{
        ProjectRoot = $root
        EditorVersion = $editorMatch.Groups['value'].Value.Trim()
        BridgeVersion = $bridge.Trim()
    }
}

function New-TuanjieMcpSection {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$CodelyCliPath,
        [Parameter(Mandatory)][string]$ProjectPath
    )

    if (-not (Test-Path -LiteralPath $CodelyCliPath -PathType Leaf)) {
        throw "找不到 CodelyCLI：$CodelyCliPath"
    }
    $cli = (Resolve-Path -LiteralPath $CodelyCliPath -ErrorAction Stop).Path
    try {
        if ([IO.Path]::IsPathRooted($ProjectPath)) {
            $project = [IO.Path]::GetFullPath($ProjectPath)
        }
        else {
            $project = [IO.Path]::GetFullPath((Join-Path (Get-Location).Path $ProjectPath))
        }
    }
    catch {
        throw "无法规范化团结项目路径：$ProjectPath"
    }
    $newline = [Environment]::NewLine
    $cliValue = ConvertTo-TomlString -Value $cli
    $projectValue = ConvertTo-TomlString -Value $project
    $lines = @(
        '[mcp_servers.tuanjie]'
        'command = "cmd.exe"'
        'args = ['
        '    "/c",'
        ('    "' + $cliValue + '",')
        '    "serve",'
        '    "unity-mcp",'
        '    "--stdio",'
        '    "--unity-project-path",'
        ('    "' + $projectValue + '"')
        ']'
        'startup_timeout_sec = 30'
        'tool_timeout_sec = 120'
        'enabled = true'
    )
    return (($lines -join $newline) + $newline)
}

function Merge-TuanjieMcpSection {
    [CmdletBinding()]
    param(
        [AllowEmptyString()][string]$Original,
        [Parameter(Mandatory)][string]$Section
    )

    if ($null -eq $Original) {
        $Original = ''
    }
    $newline = Get-Newline -Text $Original
    $normalizedSection = [regex]::Replace($Section, '\r\n|\r|\n', $newline)
    $normalizedSection = $normalizedSection.TrimEnd([char]13, [char]10)
    $targetPattern = '^[ \t]*\[mcp_servers\.tuanjie\][ \t]*(?:#.*)?$'
    $lines = @(Split-LinesPreserveTrailingEmpty -Text $Original)
    $targetIndices = @()
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match $targetPattern) {
            $targetIndices += $index
        }
    }
    if ($targetIndices.Count -gt 1) {
        return [pscustomobject]@{
            Success = $false
            Changed = $false
            Content = $Original
            Error = 'config.toml 包含重复的 [mcp_servers.tuanjie] table。'
        }
    }

    $sectionLines = @($normalizedSection -split [regex]::Escape($newline))
    if ($sectionLines.Count -gt 0 -and $sectionLines[$sectionLines.Count - 1] -eq '') {
        $sectionLines = $sectionLines[0..($sectionLines.Count - 2)]
    }

    if ($targetIndices.Count -eq 0) {
        if ([string]::IsNullOrEmpty($Original)) {
            $content = ($sectionLines -join $newline) + $newline
        }
        elseif ($Original.EndsWith($newline)) {
            $content = $Original + (($sectionLines -join $newline) + $newline)
        }
        else {
            $content = $Original + $newline + (($sectionLines -join $newline) + $newline)
        }
    }
    else {
        $start = [int]$targetIndices[0]
        $end = $lines.Count
        for ($index = $start + 1; $index -lt $lines.Count; $index++) {
            if ($lines[$index] -match '^[ \t]*\[{1,2}[^\]\r\n]+\]{1,2}[ \t]*(?:#.*)?$') {
                $end = $index
                break
            }
        }
        $before = if ($start -gt 0) { @($lines[0..($start - 1)]) } else { @() }
        $after = if ($end -lt $lines.Count) { @($lines[$end..($lines.Count - 1)]) } else { @() }
        $content = (@($before + $sectionLines + $after) -join $newline)
        if ($Original.EndsWith($newline) -and -not $content.EndsWith($newline)) {
            $content += $newline
        }
    }

    [pscustomobject]@{
        Success = $true
        Changed = $content -cne $Original
        Content = $content
        Error = $null
    }
}

function Set-TuanjieCodexConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$CodelyCliPath,
        [switch]$Force
    )

    $info = Get-TuanjieProjectInfo -ProjectPath $ProjectPath
    $section = New-TuanjieMcpSection -CodelyCliPath $CodelyCliPath -ProjectPath $info.ProjectRoot
    $codexDirectory = Join-Path $info.ProjectRoot '.codex'
    $configPath = Join-Path $codexDirectory 'config.toml'
    $backupPath = Join-Path $codexDirectory 'config.toml.bak'
    $tempPath = Join-Path $codexDirectory 'config.toml.tmp'
    New-Item -ItemType Directory -Path $codexDirectory -Force | Out-Null

    $exists = Test-Path -LiteralPath $configPath -PathType Leaf
    $original = if ($exists) { [IO.File]::ReadAllText($configPath) } else { '' }
    $merge = Merge-TuanjieMcpSection -Original $original -Section $section
    if (-not $merge.Success) {
        throw $merge.Error
    }

    $utf8 = New-Object System.Text.UTF8Encoding($false)
    try {
        [IO.File]::WriteAllText($tempPath, $merge.Content, $utf8)
        if ($exists -and (Test-ByteEqual -LeftPath $configPath -RightPath $tempPath)) {
            Remove-Item -LiteralPath $tempPath -Force
            return [pscustomobject]@{
                ConfigPath = $configPath
                Changed = $false
                BackupPath = $null
            }
        }
        if ($exists -and -not $Force) {
            Remove-Item -LiteralPath $tempPath -Force
            throw 'config.toml 已存在且内容需要更新；如确认覆盖，请使用 -Force。'
        }
        if ($exists) {
            Copy-Item -LiteralPath $configPath -Destination $backupPath -Force
        }
        Move-Item -LiteralPath $tempPath -Destination $configPath -Force
        $written = [IO.File]::ReadAllText($configPath)
        if ($written -cne $merge.Content) {
            throw 'config.toml 写入后校验失败。'
        }
        return [pscustomobject]@{
            ConfigPath = $configPath
            Changed = $true
            BackupPath = if ($exists) { $backupPath } else { $null }
        }
    }
    catch {
        if (Test-Path -LiteralPath $tempPath -PathType Leaf) {
            Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
        }
        throw
    }
}

Export-ModuleMember -Function Get-TuanjieProjectInfo, New-TuanjieMcpSection, Merge-TuanjieMcpSection, Set-TuanjieCodexConfig
