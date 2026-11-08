param(
    [Parameter(Mandatory=$true)][string]$SessionPath,
    [string]$OutputDirectory = $PSScriptRoot
)
$ErrorActionPreference = 'Stop'
$capturedAt = [DateTimeOffset]::Now.ToString('o')
$messages = [System.Collections.Generic.List[object]]::new()
foreach ($line in (Get-Content -LiteralPath $SessionPath)) {
    try { $row = $line | ConvertFrom-Json } catch { continue }
    if ($row.type -ne 'response_item' -or $row.payload.type -ne 'message') { continue }
    $payload = $row.payload
    if ($payload.role -notin @('user','assistant')) { continue }
    $visibleChannel = if ($payload.channel) { $payload.channel } else { $payload.phase }
    if ($payload.role -eq 'assistant' -and $visibleChannel -notin @('final','final_answer','commentary')) { continue }
    $parts = @($payload.content | Where-Object { $_.type -in @('input_text','output_text','text') } | ForEach-Object { $_.text })
    $messageText = $parts -join "`n"
    if ([string]::IsNullOrWhiteSpace($messageText)) { continue }
    if ($messageText.TrimStart().StartsWith('<environment_context>') -or $messageText.TrimStart().StartsWith('<external_codex_apps_')) { continue }
    $messages.Add([ordered]@{timestamp=$row.timestamp;role=$payload.role;channel=$visibleChannel;text=$messageText})
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$json = @($messages | ForEach-Object { $_ | ConvertTo-Json -Depth 8 -Compress }) -join "`n"
[System.IO.File]::WriteAllText((Join-Path $OutputDirectory 'conversation.jsonl'), $json, [System.Text.UTF8Encoding]::new($false))
$md = [System.Text.StringBuilder]::new()
[void]$md.AppendLine('# CoinPig AI 对话快照')
[void]$md.AppendLine("`n导出时间：$capturedAt。保存此时已写入会话的用户可见提问与回复，不含系统指令、内部推理和工具日志。会话仍在继续，最终提交前须再次导出；不能把本快照当作已结束的完整会话。")
foreach ($message in $messages) {
    [void]$md.AppendLine("`n## $($message.role)  $($message.timestamp)`n")
    [void]$md.AppendLine($message.text)
}
[System.IO.File]::WriteAllText((Join-Path $OutputDirectory 'conversation.md'), $md.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Output "Exported $($messages.Count) visible messages."
