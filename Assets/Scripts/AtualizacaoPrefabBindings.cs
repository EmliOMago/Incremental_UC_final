using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AtualizacaoPrefabBindings : MonoBehaviour
{
    [Header("Referências usadas pelo HUDManeger")]
    [SerializeField] private Button botaoComprar;
    [SerializeField] private TextMeshProUGUI textoBotaoComprar;
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [SerializeField] private TextMeshProUGUI textoDescricao;
    [SerializeField] private TextMeshProUGUI textoGanho;
    [SerializeField] private TextMeshProUGUI textoPreco;

    [Header("Listas auxiliares para configuração manual")]
    [Tooltip("Todos os TMPs encontrados nos filhos. Serve de apoio para você decidir qual arrastar para cada campo acima.")]
    [SerializeField] private List<TextMeshProUGUI> textosDisponiveis = new List<TextMeshProUGUI>();

    [Tooltip("Todos os botões encontrados nos filhos. Serve de apoio para você decidir qual arrastar para o campo do botão.")]
    [SerializeField] private List<Button> botoesDisponiveis = new List<Button>();

    public Button BotaoComprar => botaoComprar;
    public TextMeshProUGUI TextoBotaoComprar => textoBotaoComprar;
    public TextMeshProUGUI TextoTitulo => textoTitulo;
    public TextMeshProUGUI TextoDescricao => textoDescricao;
    public TextMeshProUGUI TextoGanho => textoGanho;
    public TextMeshProUGUI TextoPreco => textoPreco;

    private void Reset()
    {
        AtualizarListasDeComponentes();
        PreencherReferenciasPelosNomesPadrao();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AtualizarListasDeComponentes();
    }
#endif

    [ContextMenu("Atualizar listas de componentes filhos")]
    public void AtualizarListasDeComponentes()
    {
        textosDisponiveis.Clear();
        botoesDisponiveis.Clear();

        GetComponentsInChildren(true, textosDisponiveis);
        GetComponentsInChildren(true, botoesDisponiveis);
    }

    [ContextMenu("Preencher referências pelos nomes padrão")]
    public void PreencherReferenciasPelosNomesPadrao()
    {
        AtualizarListasDeComponentes();

        if (botaoComprar == null)
            botaoComprar = BuscarBotaoPorNome("Button");
        if (textoBotaoComprar == null)
            textoBotaoComprar = BuscarTMPPorNome("Text (TMP)");
        if (textoTitulo == null)
            textoTitulo = BuscarTMPPorNome("Titulo");
        if (textoDescricao == null)
            textoDescricao = BuscarTMPPorNome("Descricao");
        if (textoGanho == null)
            textoGanho = BuscarTMPPorNome("Ganho");
        if (textoPreco == null)
            textoPreco = BuscarTMPPorNome("Preco");
    }

    private TextMeshProUGUI BuscarTMPPorNome(string nome)
    {
        foreach (TextMeshProUGUI texto in textosDisponiveis)
        {
            if (texto != null && string.Equals(texto.gameObject.name, nome, StringComparison.OrdinalIgnoreCase))
                return texto;
        }

        return null;
    }

    private Button BuscarBotaoPorNome(string nome)
    {
        foreach (Button botao in botoesDisponiveis)
        {
            if (botao != null && string.Equals(botao.gameObject.name, nome, StringComparison.OrdinalIgnoreCase))
                return botao;
        }

        return null;
    }
}
