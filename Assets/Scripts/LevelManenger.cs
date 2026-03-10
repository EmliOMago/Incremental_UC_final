using System.Collections;
using UnityEngine;

public class LevelManenger : MonoBehaviour
{
    public float dinheiro = 0;
    [Space]
    [Header("Quantidade")]
    [Tooltip("Quantidades adiquiridos de multiplicação")] public int qntMultiplicadores;
    [Tooltip("Quantidades adiquiridos de ganhos passivos")] public int qntGanhosPassivos;
    [Space]
    [Header("Valores")]
    [Tooltip("Multiplicação a cada click")] public float valorMultiplicador;
    [Tooltip("Quantidade ganha por ciclo do ganho passivo")] public float valorGanhosPassivos;
    [Space]
    [Header("Preços")]
    [Tooltip("Preço base do multiplicador")] public float precoBaseMultiplicador;
    [Tooltip("Multiplicador de progressão do preço do multiplicador")] public float multiplicadorMultiplicador;
    [Space]
    [Tooltip("Preço base do ganho passivo")] public float precoBaseGanhoPassivo;
    [Tooltip("Multiplicador de progressão do preço do ganho passivo")] public float multiplicadorGanhoPassivo;
    [Space]
    [Header("Ganho passivo")]
    [Tooltip("Tempo entre cada pagamento do ganho passivo, em segundos")] public float intervaloGanhoPassivo = 1f;

    public static event System.Action OnDinheiroMudar;

    private Coroutine rotinaGanhoPassivo;
    private Coroutine rotinaAutoSave;

    private void OnEnable()
    {
        IniciarRotinas();
    }

    private void OnDisable()
    {
        PararRotinas();
    }

    public void InicializarCenaJogo()
    {
        if (GameDirector.instancia != null)
        {
            GameDirector.instancia.AtualizarReferenciasDaCena();
        }

        CarregarJogo();
        IniciarRotinas();

        if (GameDirector.instancia != null && GameDirector.instancia.hudManeger != null)
        {
            GameDirector.instancia.hudManeger.AtualizarTudo();
        }
    }

    private void IniciarRotinas()
    {
        if (!isActiveAndEnabled)
            return;

        if (rotinaGanhoPassivo == null)
            rotinaGanhoPassivo = StartCoroutine(RotinaGanhoPassivo());

        if (rotinaAutoSave == null)
            rotinaAutoSave = StartCoroutine(AutoSave());
    }

    private void PararRotinas()
    {
        if (rotinaGanhoPassivo != null)
        {
            StopCoroutine(rotinaGanhoPassivo);
            rotinaGanhoPassivo = null;
        }

        if (rotinaAutoSave != null)
        {
            StopCoroutine(rotinaAutoSave);
            rotinaAutoSave = null;
        }
    }

    public void AddDinheiro(float valor)
    {
        dinheiro += valor;
        Debug.Log(dinheiro);
        OnDinheiroMudar?.Invoke();
    }

    public void ClickDrink()
    {
        float valor = 1f;
        float multiplicador = 1f + (valorMultiplicador * qntMultiplicadores);
        valor *= multiplicador;

        AddDinheiro(valor);
        GameDirector.instancia?.hudManeger?.AtualizarMutiplicador();
    }

    public void ComprarMultiplicador()
    {
        float preco = ObtemPrecoMultiplicador();

        if (dinheiro < preco)
        {
            Debug.Log("Dinheiro insuficiente para comprar multiplicador.");
            return;
        }

        AddDinheiro(-preco);
        qntMultiplicadores += 1;

        GameDirector.instancia?.hudManeger?.AtualizarMutiplicador();
    }

    public void ComprarGanhosPassivos()
    {
        float preco = ObtemPrecoGanhoPassivo();

        if (dinheiro < preco)
        {
            Debug.Log("Dinheiro insuficiente para comprar ganho passivo.");
            return;
        }

        AddDinheiro(-preco);
        qntGanhosPassivos += 1;

        GameDirector.instancia?.hudManeger?.AtualizarGanhoPassivo();
    }

    public float ObtemPrecoMultiplicador()
    {
        float preco = precoBaseMultiplicador * Mathf.Pow(multiplicadorMultiplicador, qntMultiplicadores);
        return Mathf.Max(precoBaseMultiplicador, preco);
    }

    public float ObtemPrecoGanhoPassivo()
    {
        float preco = precoBaseGanhoPassivo * Mathf.Pow(multiplicadorGanhoPassivo, qntGanhosPassivos);
        return Mathf.Max(precoBaseGanhoPassivo, preco);
    }

    private IEnumerator RotinaGanhoPassivo()
    {
        while (true)
        {
            float espera = intervaloGanhoPassivo > 0f ? intervaloGanhoPassivo : 1f;
            yield return new WaitForSeconds(espera);

            if (qntGanhosPassivos <= 0 || valorGanhosPassivos <= 0f)
                continue;

            float ganho = valorGanhosPassivos * qntGanhosPassivos;
            AddDinheiro(ganho);
            GameDirector.instancia?.hudManeger?.AtualizarGanhoPassivo();
        }
    }

    public IEnumerator AutoSave()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            GameDirector.instancia?.saveManager?.Salvar();
        }
    }

    public void CarregarJogo()
    {
        GameDirector.instancia?.saveManager?.Carregar();
    }
}
