using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class VendaTransferenciasAutomaticasResposta
{
    [JsonPropertyName("loja")]
    public string Loja { get; set; } = string.Empty;

    [JsonPropertyName("pedido")]
    public string Pedido { get; set; } = string.Empty;

    [JsonPropertyName("TemTransferenciaAutomatica")]
    public bool TemTransferenciaAutomatica => Itens.Any(item => item.TransferenciaAutomatica == 1);

    [JsonPropertyName("itens")]
    public List<VendaTransferenciaAutomaticaItem> Itens { get; set; } = new();
}
