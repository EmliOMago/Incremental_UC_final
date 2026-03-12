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
    private Coroutine rotinaInicializacao;

    private void Start()
    {
        GameDirector.instancia?.AtualizarReferenciasDaCena();
        GameDirector.instancia?.saveManager?.SolicitarCarregamentoInicial();
    }

    private void OnDisable()
    {
        if (rotinaInicializacao != null)
        {
            StopCoroutine(rotinaInicializacao);
            rotinaInicializacao = null;
        }

        PararRotinas();
    }

    public void InicializarCenaJogo()
    {
        if (rotinaInicializacao != null)
        {
            StopCoroutine(rotinaInicializacao);
            rotinaInicializacao = null;
        }

        rotinaInicializacao = StartCoroutine(RotinaInicializarCenaJogo());
    }

    private IEnumerator RotinaInicializarCenaJogo()
    {
        PararRotinas();

        if (GameDirector.instancia != null)
            GameDirector.instancia.AtualizarReferenciasDaCena();

        HUDManeger hud = GameDirector.instancia != null ? GameDirector.instancia.hudManeger : null;
        SaveManagerPlayerPrefs saveManager = GameDirector.instancia != null ? GameDirector.instancia.saveManager : null;

        hud?.PrepararTelaCarregamento();
        yield return null;

        if (hud != null)
            hud.ReinicializarHUDDaCena();

        saveManager?.SolicitarCarregamentoInicial();
        while (saveManager != null && !saveManager.CarregamentoInicialConcluido)
            yield return null;

        OnDinheiroMudar?.Invoke();
        yield return new WaitForSeconds(0.1f);

        hud?.AtualizarTudo();
        yield return null;

        IniciarRotinas();
        hud?.ConcluirTelaCarregamento();
        rotinaInicializacao = null;
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
        if (dinheiro < 0f)
            dinheiro = 0f;

        GameDirector.instancia?.saveManager?.MarcarSaveComoSujo();
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
            SaveManagerPlayerPrefs saveManager = GameDirector.instancia != null ? GameDirector.instancia.saveManager : null;
            float espera = saveManager != null ? saveManager.ObterIntervaloAutoSaveSegundos() : 5f;
            yield return new WaitForSeconds(espera);
            saveManager?.SalvarSeNecessario();
        }
    }

    public void CarregarJogo()
    {
        GameDirector.instancia?.saveManager?.CarregarOuCriar();
    }
}
