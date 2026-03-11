using System.Collections;
using UnityEngine;

public class LevelManenger : MonoBehaviour
{
    public float dinheiro = 0;

    [Header("Ganho passivo")]
    [Tooltip("Tempo entre cada pagamento do ganho passivo, em segundos")]
    public float intervaloGanhoPassivo = 1f;

    public static event System.Action OnDinheiroMudar;

    private Coroutine rotinaGanhoPassivo;
    private Coroutine rotinaAutoSave;
    private bool jogoInicializado;

    private void OnEnable()
    {
        if (jogoInicializado)
            IniciarRotinas();
    }

    private void OnDisable()
    {
        PararRotinas();
    }

    public void InicializarCenaJogo()
    {
        if (GameDirector.instancia != null)
            GameDirector.instancia.AtualizarReferenciasDaCena();

        if (GameDirector.instancia != null && GameDirector.instancia.hudManeger != null)
            GameDirector.instancia.hudManeger.IniciarFluxoDeCarregamentoInicial();
    }

    public void ConcluirInicializacaoCenaJogo()
    {
        jogoInicializado = true;
        IniciarRotinas();
        OnDinheiroMudar?.Invoke();
    }

    private void IniciarRotinas()
    {
        if (!isActiveAndEnabled || !jogoInicializado)
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
        if (dinheiro < 0f)
            dinheiro = 0f;

        OnDinheiroMudar?.Invoke();
    }

    public void ClickDrink()
    {
        float valor = 1f;
        if (GameDirector.instancia != null && GameDirector.instancia.hudManeger != null)
            valor = GameDirector.instancia.hudManeger.ObterValorCliqueFinal();

        AddDinheiro(valor);
    }

    private IEnumerator RotinaGanhoPassivo()
    {
        while (true)
        {
            float espera = intervaloGanhoPassivo > 0f ? intervaloGanhoPassivo : 1f;
            yield return new WaitForSeconds(espera);

            float ganho = 0f;
            if (GameDirector.instancia != null && GameDirector.instancia.hudManeger != null)
                ganho = GameDirector.instancia.hudManeger.ObterValorPassivoFinalPorCiclo();

            if (ganho <= 0f)
                continue;

            AddDinheiro(ganho);
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
}
