using TMPro;
using UnityEngine;

public class HUDManeger : MonoBehaviour
{
    [System.Serializable]
    public class BlocoUpgradeUI
    {
        [Tooltip("Texto que mostra o preço atual do upgrade nesta instância do prefab.")]
        public TextMeshProUGUI textoValor;

        [Tooltip("Texto de descrição desta instância do prefab. Pode conter a tag {X}.")]
        public TextMeshProUGUI textoDescricao;

        [HideInInspector] public string textoOriginalDescricao;

        public void CapturarTextoOriginal()
        {
            textoOriginalDescricao = textoDescricao != null ? textoDescricao.text : string.Empty;
        }
    }

    [Header("HUD")]
    [Tooltip("Texto que mostra o dinheiro atual do jogador.")]
    public TextMeshProUGUI dinheiroText;

    [Header("Instância do prefab - Multiplicador")]
    [Tooltip("Campos da instância do prefab usada para o multiplicador.")]
    public BlocoUpgradeUI multiplicadorUI;

    [Header("Instância do prefab - Ganho passivo")]
    [Tooltip("Campos da instância do prefab usada para o ganho passivo.")]
    public BlocoUpgradeUI ganhoPassivoUI;

    private void OnEnable()
    {
        LevelManenger.OnDinheiroMudar += AtualizarDinheiro;
    }

    private void OnDisable()
    {
        LevelManenger.OnDinheiroMudar -= AtualizarDinheiro;
    }

    private void Start()
    {
        multiplicadorUI.CapturarTextoOriginal();
        ganhoPassivoUI.CapturarTextoOriginal();

        Invoke("AtualizarTudo", 0.2f);
    }

    public void AtualizarTudo()
    {
        AtualizarDinheiro();
        AtualizarMutiplicador();
        AtualizarGanhoPassivo();
    }

    public void AtualizarDinheiro()
    {
        if (dinheiroText == null || GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        dinheiroText.text = GameDirector.instancia.levelManenger.dinheiro.ToString("0.00");
    }

    public static string ComPrefixo(float valor, string unidade = "", int casaDecimais = 0)
    {
        string[] prefixos = { "", "K", "M", "B", "T", "P", "E", "Z", "Y" };
        int indice = 0;
        float valorAbsoluto = Mathf.Abs(valor);

        while (valorAbsoluto >= 1000 && indice < prefixos.Length - 1)
        {
            valorAbsoluto /= 1000;
            indice++;
        }

        string resultado = $"F{casaDecimais}";
        return $"{valorAbsoluto.ToString(resultado)}{prefixos[indice]}{unidade}";
    }

    public void AtualizarMutiplicador()
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        LevelManenger level = GameDirector.instancia.levelManenger;

        if (multiplicadorUI.textoValor != null)
            multiplicadorUI.textoValor.text = "$ " + level.ObtemPrecoMultiplicador().ToString("0.##");

        if (multiplicadorUI.textoDescricao != null)
        {
            float ganho = 10f * level.valorMultiplicador * level.qntMultiplicadores;
            string baseDescricao = string.IsNullOrEmpty(multiplicadorUI.textoOriginalDescricao)
                ? multiplicadorUI.textoDescricao.text
                : multiplicadorUI.textoOriginalDescricao;
            multiplicadorUI.textoDescricao.text = baseDescricao.Replace("{X}", ganho.ToString("0.##"));
        }
    }

    public void AtualizarGanhoPassivo()
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        LevelManenger level = GameDirector.instancia.levelManenger;

        if (ganhoPassivoUI.textoValor != null)
            ganhoPassivoUI.textoValor.text = "$ " + level.ObtemPrecoGanhoPassivo().ToString("0.##");

        if (ganhoPassivoUI.textoDescricao != null)
        {
            float ganho = level.valorGanhosPassivos * level.qntGanhosPassivos;
            string baseDescricao = string.IsNullOrEmpty(ganhoPassivoUI.textoOriginalDescricao)
                ? ganhoPassivoUI.textoDescricao.text
                : ganhoPassivoUI.textoOriginalDescricao;
            ganhoPassivoUI.textoDescricao.text = baseDescricao.Replace("{X}", ganho.ToString("0.##"));
        }
    }
}
