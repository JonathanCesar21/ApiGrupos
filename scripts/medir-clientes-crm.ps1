param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Antes', 'Depois')]
    [string]$Fase,
    [string]$BaseUrl = 'http://127.0.0.1:5000',
    [ValidateRange(1, 150000)]
    [int]$PageSize = 150000,
    [ValidateRange(1, 20)]
    [int]$Repeticoes = 3,
    [string]$DiretorioResultados = (Join-Path $PSScriptRoot '..\build-check\crm-benchmark')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$baseUri = [Uri]($BaseUrl.TrimEnd('/') + '/')
if (-not $baseUri.IsAbsoluteUri -or $baseUri.Scheme -notin @('http', 'https') -or
    $baseUri.UserInfo -or $baseUri.Query -or $baseUri.Fragment) {
    throw 'Informe apenas a URL base HTTP/HTTPS, sem credenciais, query string ou fragmento.'
}
$endpoint = [Uri]::new($baseUri, 'api/clientes/crm').AbsoluteUri
$arquivoAntes = Join-Path $DiretorioResultados 'antes.json'
$antes = $null
if ($Fase -eq 'Antes' -and (Test-Path -LiteralPath $arquivoAntes)) {
    throw 'A medicao Antes ja existe e sera preservada. Para uma nova linha de base, escolha outro DiretorioResultados.'
}
if ($Fase -eq 'Depois') {
    if (-not (Test-Path -LiteralPath $arquivoAntes)) {
        throw 'Execute a fase Antes com a versao original antes de medir a versao alterada.'
    }
    $antes = Get-Content -LiteralPath $arquivoAntes -Raw | ConvertFrom-Json
    if ($antes.Endpoint -ne $endpoint -or $antes.PageSize -ne $PageSize -or $antes.Repeticoes -ne $Repeticoes) {
        throw 'Use a mesma URL, PageSize e quantidade de repeticoes da fase Antes.'
    }
}

function Invoke-ConsultaCompleta {
    param([System.Net.Http.HttpClient]$Cliente)
    $tempoCompleto = [Diagnostics.Stopwatch]::StartNew()
    $httpMs = 0.0
    $bytes = 0L
    $quantidade = 0L
    $pagina = 1
    $total = $null
    do {
        $url = "${endpoint}?page=$pagina&pageSize=$PageSize"
        $tempoHttp = [Diagnostics.Stopwatch]::StartNew()
        $resposta = $Cliente.GetAsync($url).GetAwaiter().GetResult()
        try {
            if (-not $resposta.IsSuccessStatusCode) {
                throw "A consulta retornou HTTP $([int]$resposta.StatusCode). A amostra nao sera registrada. Para HTTP 401, configure APIGRUPOS_API_KEY no ambiente local."
            }
            $texto = $resposta.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $tempoHttp.Stop()
            $httpMs += $tempoHttp.Elapsed.TotalMilliseconds
            $bytes += [Text.Encoding]::UTF8.GetByteCount($texto)
            $dados = $texto | ConvertFrom-Json
            foreach ($campo in @('page', 'pageSize', 'total', 'totalPages', 'items')) {
                if ($campo -notin $dados.PSObject.Properties.Name) {
                    throw "Resposta sem o campo $campo; a amostra nao sera registrada."
                }
            }
            if ($dados.page -ne $pagina -or $dados.pageSize -ne $PageSize -or $null -eq $dados.items) {
                throw 'Paginacao inesperada; a amostra nao sera registrada.'
            }
            if ($null -eq $total) { $total = [long]$dados.total }
            if ($total -lt 0 -or $dados.total -ne $total -or $dados.totalPages -ne [Math]::Ceiling($total / [double]$PageSize)) {
                throw 'Total de clientes inconsistente durante a leitura; repita a medicao.'
            }
            $itensEsperados = [Math]::Min($PageSize, [Math]::Max(0, $total - (($pagina - 1L) * $PageSize)))
            if (@($dados.items).Count -ne $itensEsperados) {
                throw 'Quantidade de itens inesperada; a amostra nao sera registrada.'
            }
            if ($Fase -eq 'Depois' -and $itensEsperados -gt 0 -and
                ($null -eq $dados.items[0].historico)) {
                throw 'A API ainda nao retornou historico. Instale e reinicie a versao atualizada antes de medir Depois.'
            }
            $quantidade += @($dados.items).Count
            Write-Host ("Pagina {0}/{1}: {2} clientes; HTTP {3:N2}s." -f $pagina, [Math]::Max(1, $dados.totalPages), @($dados.items).Count, ($tempoHttp.Elapsed.TotalMilliseconds / 1000))
            $pagina++
        }
        finally { $resposta.Dispose() }
    } while ($pagina -le $dados.totalPages)
    $tempoCompleto.Stop()
    [PSCustomObject]@{
        Clientes = $quantidade
        Paginas = $pagina - 1
        HttpMs = [Math]::Round($httpMs, 2)
        TotalMs = [Math]::Round($tempoCompleto.Elapsed.TotalMilliseconds, 2)
        Bytes = $bytes
    }
}

function Get-Mediana {
    param([double[]]$Valores)
    $ordenados = @($Valores | Sort-Object)
    $meio = [int][Math]::Floor($ordenados.Count / 2)
    if ($ordenados.Count % 2) { return $ordenados[$meio] }
    return ($ordenados[$meio - 1] + $ordenados[$meio]) / 2
}

$cliente = [System.Net.Http.HttpClient]::new()
$cliente.Timeout = [TimeSpan]::FromMinutes(5)
$apiKey = $env:APIGRUPOS_API_KEY
$envPath = Join-Path $PSScriptRoot '..\.env'
if (-not $apiKey -and (Test-Path -LiteralPath $envPath)) {
    foreach ($linha in Get-Content -LiteralPath $envPath) {
        if ($linha -match '^\s*APIGRUPOS_API_KEY\s*=\s*(.*?)\s*$') {
            $apiKey = $Matches[1].Trim().Trim([char]34).Trim([char]39)
        }
    }
}
if ($apiKey) {
    $cliente.DefaultRequestHeaders.Add('X-API-Key', $apiKey)
}
try {
    Write-Host 'Aquecendo a consulta completa (fora das amostras)...'
    $aquecimento = Invoke-ConsultaCompleta $cliente
    Write-Host ("Aquecimento concluido: {0} clientes em {1} paginas." -f $aquecimento.Clientes, $aquecimento.Paginas)
    $amostras = @(for ($numero = 1; $numero -le $Repeticoes; $numero++) {
        Write-Host "Medindo consulta completa $numero de $Repeticoes..."
        Invoke-ConsultaCompleta $cliente
    })
}
finally { $cliente.Dispose() }

$totais = @($amostras.Clientes | Sort-Object -Unique)
if ($totais.Count -ne 1 -or $totais[0] -ne $aquecimento.Clientes) {
    throw 'O total de clientes variou entre as consultas. Repita a medicao em uma base estavel.'
}
$resultado = [PSCustomObject]@{
    Fase = $Fase
    ExecutadoEmUtc = [DateTime]::UtcNow.ToString('o')
    Endpoint = $endpoint
    PageSize = $PageSize
    Repeticoes = $Repeticoes
    MedianaHttpMs = Get-Mediana $amostras.HttpMs
    MedianaTotalMs = Get-Mediana $amostras.TotalMs
    Amostras = $amostras
}
if ($antes) {
    if (@($antes.Amostras.Clientes | Sort-Object -Unique)[0] -ne $totais[0]) {
        throw 'A quantidade de clientes mudou entre Antes e Depois. Os resultados nao sao diretamente comparaveis.'
    }
    $variacao = if ($antes.MedianaHttpMs -gt 0) {
        [Math]::Round(100 * ($resultado.MedianaHttpMs / $antes.MedianaHttpMs - 1), 2)
    } else { $null }
    $resultado | Add-Member -NotePropertyName VariacaoHttpPercentual -NotePropertyValue $variacao
}
New-Item -ItemType Directory -Path $DiretorioResultados -Force | Out-Null
$arquivoResultado = Join-Path $DiretorioResultados ($Fase.ToLowerInvariant() + '.json')
$resultado | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $arquivoResultado -Encoding UTF8
$resultado | Select-Object Fase, MedianaHttpMs, MedianaTotalMs, VariacaoHttpPercentual | Format-Table -AutoSize
Write-Host "Resultado salvo em $arquivoResultado. Nomes, telefones e dados individuais nao sao salvos."
