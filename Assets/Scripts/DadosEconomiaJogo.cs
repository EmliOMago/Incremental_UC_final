using System;
using System.Collections.Generic;

[Serializable]
public class DadosEconomiaJogo
{
    public float dinheiroAtual;
    public int totalComprasMelhorias;
    public int quantidadeMelhoriasDesbloqueadas;
    public float valorReferenciaDesbloqueio;
    public float maiorDinheiroAtingido;
    public List<DadosMelhoriaEconomia> melhorias = new List<DadosMelhoriaEconomia>();
}

[Serializable]
public class DadosMelhoriaEconomia
{
    public int indiceTabela;
    public string idMelhoria;
    public string categoria;
    public int quantidadeComprada;
    public float precoBase;
    public float precoAtual;
    public float multiplicadorPreco;
    public float percentualPorCompra;
    public float multiplicadorTotal;
}
