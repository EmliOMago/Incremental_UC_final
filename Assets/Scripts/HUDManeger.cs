using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class HUDManeger : MonoBehaviour
{
    [Header("Hud")]
    [Tooltip("")] public TextMeshProUGUI dinheiroText;
    [Tooltip("")] public TextMeshProUGUI textoValorMultipplicador;
    [Tooltip("")] public TextMeshProUGUI textoDescricaoMultiplicador;
    string textoOriginalMultiplicador;

    [Tooltip("")] public TextMeshProUGUI textoValorGanhoMultiplicador;
    [Tooltip("")] public TextMeshProUGUI textoValorGanhoPassivo;
    [Tooltip("")] public TextMeshProUGUI textoDescricaoGanhoPassivo;
    string textoOriginalGanhoPassivo;

    private string dinheiroFormatado;

    private void OnEnable()
    {
        LevelManenger.OnDinheiroMudar += AtualizarDinheiro;
    }

    private void Start()
    {
        textoOriginalMultiplicador = textoDescricaoMultiplicador.text;
        textoOriginalGanhoPassivo = textoDescricaoGanhoPassivo.text;

        AtualizarDinheiro();
        AtualizarMutiplicador();
    }

    public void AtualizarDinheiro()
    {
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

        double valorFormatado = System.Math.Pow(1000, casaDecimais);

        string resultado = $"F{casaDecimais}";
        return $"{valorAbsoluto.ToString(resultado)}{prefixos[indice]}{unidade}";
    }

    private void Formatar()
    {
        
    }

    public void AtualizarMutiplicador()
    {
        textoValorMultipplicador.text = "$ "+GameDirector.instancia.levelManenger.ObtemPrecoMultiplicador().ToString();

        float ganho = 10 * GameDirector.instancia.levelManenger.valorMultiplicador * GameDirector.instancia.levelManenger.qntMultiplicadores;
        textoDescricaoMultiplicador.text = textoOriginalMultiplicador.Replace("{X}", ganho.ToString());
    }

    public void AtualizarGanhoPassivo()
    {
        textoValorGanhoMultiplicador.text = "$ "+GameDirector.instancia.levelManenger.ObtemPrecoGanhoPassivo().ToString();
        float ganho = GameDirector.instancia.levelManenger.valorGanhosPassivos * GameDirector.instancia.levelManenger.qntGanhosPassivos;
        textoDescricaoGanhoPassivo.text = textoOriginalGanhoPassivo.Replace("{X}", ganho.ToString());
    }
}
