using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Classificacao : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoDestino;

    private void Start()
    {
        if (!isActiveAndEnabled)
            return;

        StartCoroutine(RotinaCarregarClassificacao());
    }

    private IEnumerator RotinaCarregarClassificacao()
    {
        if (textoDestino == null)
            textoDestino = GetComponentInChildren<TextMeshProUGUI>(true);

        if (textoDestino == null)
            yield break;

        textoDestino.text = "Carregando ranking...";

        List<BancoDeDados.DadosRankingFetch> itens = null;
        yield return BancoDeDados.Instancia.CarregarTop5(resultado => itens = resultado);

        if (itens == null || itens.Count == 0)
        {
            textoDestino.text = "Sem pontuadores cadastrados.";
            yield break;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < itens.Count; i++)
        {
            BancoDeDados.DadosRankingFetch item = itens[i];
            if (item == null)
                continue;

            if (sb.Length > 0)
                sb.AppendLine();

            sb.Append(i + 1)
              .Append(". ")
              .Append(item.Nome)
              .Append(" | ")
              .Append(item.dinheiroMax.ToString("0.##"))
              .Append(" | ")
              .Append(BancoDeDados.FormatarDataRegistro(item.created_at));
        }

        textoDestino.text = sb.ToString();
    }
}
