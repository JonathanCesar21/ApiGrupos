using ApiGrupos.Models;

namespace ApiGrupos.Services;

public static class ClienteCrmHistoricoCalculos
{
    public readonly record struct DiaPagamento(int Dia, decimal Peso, DateTime UltimaData);

    public static void AplicarAtrasos(ClienteCrmHistorico historico, int pagas, int atrasadas,
        int semVencimento, long totalDiasAtraso)
    {
        historico.QtdParcelasPagas12Meses = pagas;
        historico.QtdParcelasPagasComAtraso12Meses = atrasadas;
        historico.QtdParcelasPagasSemVencimento12Meses = semVencimento;
        historico.MediaDiasAtraso12Meses = atrasadas > 0
            ? decimal.Round((decimal)totalDiasAtraso / atrasadas, 2, MidpointRounding.AwayFromZero)
            : pagas > 0 && semVencimento == 0 ? 0m : null;
        historico.PercentualParcelasPagasComAtraso12Meses = pagas > 0 && semVencimento == 0
            ? decimal.Round(100m * atrasadas / pagas, 2, MidpointRounding.AwayFromZero)
            : null;
    }

    public static void AplicarDiaHabitual(ClienteCrmHistorico historico, IReadOnlyList<DiaPagamento> dias)
    {
        historico.DiaHabitualPagamento = null;
        historico.ConcentracaoDiaHabitualPagamento = null;
        if (historico.QtdMesesComPagamento12Meses < 3 || dias.Count == 0)
            return;

        var pesoTotal = dias.Sum(item => item.Peso);
        if (pesoTotal <= 0)
            return;

        var melhorDia = 0;
        var melhorPeso = -1m;
        var menorDistancia = decimal.MaxValue;
        var melhorPesoExato = -1m;
        var ultimaData = DateTime.MinValue;
        for (var candidato = 1; candidato <= 31; candidato++)
        {
            var peso = 0m;
            var distanciaPonderada = 0m;
            var pesoExato = 0m;
            var maisRecente = DateTime.MinValue;
            foreach (var item in dias)
            {
                var distancia = Math.Abs(item.Dia - candidato);
                distancia = Math.Min(distancia, 31 - distancia);
                if (distancia > 2)
                    continue;
                peso += item.Peso;
                distanciaPonderada += item.Peso * distancia;
                if (distancia == 0) pesoExato += item.Peso;
                if (item.UltimaData > maisRecente) maisRecente = item.UltimaData;
            }

            // Arredondar evita que a divisao dos votos mensais decida empates por residuos.
            peso = decimal.Round(peso, 6);
            distanciaPonderada = decimal.Round(distanciaPonderada, 6);
            pesoExato = decimal.Round(pesoExato, 6);
            if (peso > melhorPeso || peso == melhorPeso &&
                (distanciaPonderada < menorDistancia || distanciaPonderada == menorDistancia &&
                 (pesoExato > melhorPesoExato || pesoExato == melhorPesoExato && maisRecente > ultimaData)))
            {
                melhorDia = candidato;
                melhorPeso = peso;
                menorDistancia = distanciaPonderada;
                melhorPesoExato = pesoExato;
                ultimaData = maisRecente;
            }
        }

        var concentracao = Math.Min(100m, decimal.Round(100m * melhorPeso / pesoTotal, 2, MidpointRounding.AwayFromZero));
        historico.ConcentracaoDiaHabitualPagamento = concentracao;
        if (concentracao >= 60m)
            historico.DiaHabitualPagamento = melhorDia;
    }
}
