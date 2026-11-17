using ApiGrupos.Controllers;
using ApiGrupos.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiGrupos.Services;

public class DashboardOpenApi : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType == typeof(SubgruposController))
        {
            var item = context.SchemaGenerator.GenerateSchema(typeof(Subgrupo), context.SchemaRepository);
            var page = context.SchemaGenerator.GenerateSchema(typeof(PaginacaoResposta<Subgrupo>), context.SchemaRepository);
            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "Array sem page/pageSize; envelope paginado quando solicitados. departamentoCodigo = Subgrupos.grupo (0 eletro, 1 moveis, 2 confeccoes); departamento textual preservado. Esquema ERP pendente de homologacao.",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new() { Schema = new OpenApiSchema { OneOf = [new() { Type = "array", Items = item }, page] } }
                }
            };
            return;
        }
        if (context.MethodInfo.DeclaringType != typeof(DashboardController)) return;
        var route = context.ApiDescription.RelativePath?.Split('/').Last() ?? "";
        var eventWindow = route is "recebimentos" or "ativacoes" or "terceiros" or "vendas-agregado" or "entradas-agregado";
        operation.Summary = route switch
        {
            "recebimentos" => "Todas as formas e pedidos de ContaReceber, com baixas desde 2022.",
            "titulos" => "CReceber completo, incluindo pagos e antigos, sem corte de 70 dias.",
            "metas-lojas" => "Metas mensais desde 2022, preservando linhas aditivas.",
            "metas-vendedores" => "Metas desde 2022; JOIN por codigo do vendedor, sem exigir a mesma loja.",
            "vendedores" => "Codigo, nome e loja, incluindo vendedores historicos.",
            "entradas-a-confirmar" => "Fotografia de EntradaControle com ProdutoBarras por barras, sem corte obrigatorio.",
            "transferencias" => "Transferencia desde 2024 UNION ALL controle de origem 5 desde 2026.",
            "ativacoes" => "Primeira compra calculada sobre toda a historia antes do filtro de periodo.",
            "referencias" => "Menor codigo por referencia elegivel nos JOINs originais, sem corte de cadastro em 2025.",
            "terceiros" => "CReceberCob com INNER JOIN em CobrancaTerceiros; filtro por vencimento.",
            "vendas-agregado" => "SUM(ValorTotal) e SUM(quant*ValorCusto), incluindo quantidade liquida zero.",
            "entradas-agregado" => "Entradas deduplicadas desde 2022 antes do filtro de periodo.",
            _ => "Dashboard Book Lojas."
        };
        operation.Description = "Contrato novo. Retorna array completo, sem paginacao; page/pageSize causam HTTP 400. " +
            "Datas ISO de negocio sem conversao UTC; dataFim inclusiva. Nulos preservados. Timeout SQL de 60 segundos. " +
            (eventWindow ? "Periodo obrigatorio de ate 120 dias inclusivos. " : route == "titulos" ? "Periodo opcional de ate 120 dias; omitir ambas as datas para fotografia integral. " : "Filtros opcionais; sem filtros retorna a fotografia completa da fonte. ") +
            "Nomes, tipos e multiplicidade dos JOINs no ERP ainda dependem de homologacao SQL.";
        foreach (var parameter in operation.Parameters)
        {
            parameter.Name = char.ToLowerInvariant(parameter.Name[0]) + parameter.Name[1..];
            var name = parameter.Name.ToLowerInvariant();
            if (name is "datainicio" or "datafim")
            {
                parameter.Required = eventWindow;
                parameter.Description = name == "datainicio" ? "Inicio inclusivo (YYYY-MM-DD), minimo 1753-01-01. Informar junto com dataFim." : "Fim inclusivo (YYYY-MM-DD), maximo 9999-12-30. Informar junto com dataInicio.";
            }
            var values = name switch
            {
                "tipodata" when route == "recebimentos" => new[] { "baixa", "vencimento" },
                "tipodata" => new[] { "pedido", "vencimento", "baixa" },
                "situacao" => new[] { "todos", "baixa-nula", "pago-nulo", "pago-preenchido" },
                "tipotransferencia" => new[] { "AUTOMATICA AUTORIZADA", "AUTOMATICA PENDENTE", "MANUAL", "MANUAL EFETIVADA", "MANUAL PENDENTE" },
                _ => []
            };
            if (values.Length > 0) parameter.Schema.Enum = values.Select(v => (IOpenApiAny)new OpenApiString(v)).ToList();
            if (name == "tipodata") parameter.Schema.Default = new OpenApiString(route == "recebimentos" ? "baixa" : "pedido");
            if (name == "situacao") parameter.Schema.Default = new OpenApiString("todos");
            if (name == "somentependentes") parameter.Schema.Default = new OpenApiBoolean(false);
            if (name is "loja" or "lojarecebimento" or "origem") parameter.Description = "Codigo numerico normalizado; este filtro nao substitui autorizacao por loja no backend do dashboard.";
        }
        if (route == "transferencias" && context.SchemaRepository.Schemas.TryGetValue(nameof(DashboardTransferencia), out var transferSchema))
            transferSchema.Properties["produtoCompleto"].Nullable = false;
        foreach (var (code, description) in new[]
        {
            ("400", "Filtro/periodo invalido ou paginacao nao suportada."), ("401", "X-API-Key ausente ou invalida."),
            ("503", "Banco ou chave da API nao configurados."), ("504", "Timeout SQL (60 segundos)."),
            ("500", "Falha de consulta; nunca equivale a conjunto vazio.")
        }) operation.Responses[code] = new OpenApiResponse { Description = description };
    }
}
