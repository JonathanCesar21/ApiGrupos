using System;

namespace ApiGrupos.Models;

public class EstoqueMatrizItem
{
    public int CodProd { get; set; }
    public int? CodCor { get; set; }
    public string? Cor { get; set; }
    public string? DescProd { get; set; }
    public string? Barras { get; set; }
    public string? Grupo { get; set; }
    public string? SubGrupo { get; set; }
    public string? CodNumero { get; set; }
    public string? Numero { get; set; }
    public string? Referencia { get; set; }
    public string? Fornecedor { get; set; }
    public decimal ValorCusto { get; set; }
    public decimal Valor { get; set; }
    public string? Colecao { get; set; }
    public int Quant { get; set; }
    public string? Loja { get; set; }
    public string? Categoria { get; set; }
    public DateTime? UltimaDataEntrada { get; set; }
}