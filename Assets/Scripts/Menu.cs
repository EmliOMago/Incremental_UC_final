using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] private TMP_InputField InsNome;
    [SerializeField] private TextMeshProUGUI textoRankingTop5;

    private bool _acaoBloqueada;
    private CanvasGroup _canvasGroup;
    private EventSystem _eventSystem;

    private void Awake()
    {
        GarantirReferencias();
    }

    private void Start()
    {
        GarantirReferencias();
        PreencherNomeAtual();

        if (SceneManager.GetActiveScene().name == "CenaMenu")
            StartCoroutine(RotinaAtualizarRanking());
    }

    private void Update()
    {
        if (_acaoBloqueada)
            return;

        if (!SceneManager.GetActiveScene().name.Equals("CenaMenu") || !Input.GetKeyDown(KeyCode.F12))
            return;

        StartCoroutine(RotinaLimparSaveEExcluirRegistro());
    }

    public void AtualizarNome()
    {
        GarantirReferencias();
        BancoDeDados.Nome = InsNome != null ? InsNome.text : BancoDeDados.Nome;
    }

    public void IniciarJogo()
    {
        AtualizarNome();
        Invoke(nameof(Comecar), 1f);
    }

    private void Comecar()
    {
        SceneManager.LoadScene("CenaJogo");
    }

    public void VoltarMenu()
    {
        if (_acaoBloqueada)
            return;

        StartCoroutine(RotinaSalvarEExecutar(() => SceneManager.LoadScene("CenaMenu")));
    }

    public void SairJogo()
    {
        if (_acaoBloqueada)
            return;

        StartCoroutine(RotinaSalvarEExecutar(() =>
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }));
    }

    private IEnumerator RotinaSalvarEExecutar(System.Action acaoFinal)
    {
        BloquearComandos(true);
        AtualizarNome();

        SaveManagerPlayerPrefs saveManager = GameDirector.instancia != null ? GameDirector.instancia.saveManager : null;
        bool sucesso = true;

        if (saveManager != null)
            yield return saveManager.SalvarEstadoAtualNoEncerramento(resultado => sucesso = resultado);

        if (sucesso)
        {
            BloquearComandos(false);
            acaoFinal?.Invoke();
            yield break;
        }

        Debug.LogWarning("Menu: o jogo permaneceu na tela atual porque o registro online não pôde ser salvo/atualizado no Supabase.");
        BloquearComandos(false);
    }

    private IEnumerator RotinaLimparSaveEExcluirRegistro()
    {
        BloquearComandos(true);
        AtualizarNome();

        SaveManagerPlayerPrefs.LimparSaveGlobal();
        BancoDeDados.dinheiroMax = 0f;

        bool sucessoExclusao = true;
        if (BancoDeDados.PossuiNomeValido)
            yield return BancoDeDados.Instancia.ExcluirRegistroPorNome(BancoDeDados.Nome, resultado => sucessoExclusao = resultado);

        Debug.Log(sucessoExclusao
            ? "Save local limpo e registro do banco excluído no menu inicial."
            : "Save local limpo no menu inicial, mas o registro online não pôde ser excluído.");

        yield return RotinaAtualizarRanking();
        BloquearComandos(false);
    }

    private IEnumerator RotinaAtualizarRanking()
    {
        GarantirReferencias();
        if (textoRankingTop5 == null)
            yield break;

        textoRankingTop5.text = "Carregando ranking...";

        List<BancoDeDados.DadosRankingFetch> itens = null;
        yield return BancoDeDados.Instancia.CarregarTop5(resultado => itens = resultado);

        if (itens == null || itens.Count == 0)
        {
            textoRankingTop5.text = "Sem pontuadores cadastrados.";
            yield break;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
        for (int i = 0; i < itens.Count; i++)
        {
            BancoDeDados.DadosRankingFetch item = itens[i];
            if (item == null)
                continue;

            if (sb.Length > 0)
                sb.AppendLine();

            sb.Append(i + 1)
              .Append(". ")
              .Append(string.IsNullOrWhiteSpace(item.Nome) ? "Sem nome" : item.Nome)
              .Append(" | ")
              .Append(item.dinheiroMax.ToString("0.##"))
              .Append(" | ")
              .Append(BancoDeDados.FormatarDataRegistro(item.created_at));
        }

        textoRankingTop5.text = sb.Length > 0 ? sb.ToString() : "Sem pontuadores cadastrados.";
    }

    private void PreencherNomeAtual()
    {
        GarantirReferencias();
        if (InsNome != null && string.IsNullOrEmpty(InsNome.text))
            InsNome.text = BancoDeDados.Nome;
    }

    private void GarantirReferencias()
    {
        if (InsNome == null)
            InsNome = FindFirstObjectByType<TMP_InputField>(FindObjectsInactive.Include);

        if (textoRankingTop5 == null)
            textoRankingTop5 = EncontrarOuCriarTextoRanking();

        if (_canvasGroup == null)
            _canvasGroup = GetComponentInParent<CanvasGroup>();

        if (_eventSystem == null)
            _eventSystem = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
    }

    private TextMeshProUGUI EncontrarOuCriarTextoRanking()
    {
        TextMeshProUGUI textoExistente = null;
        TextMeshProUGUI[] todosTextos = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TextMeshProUGUI texto in todosTextos)
        {
            if (texto == null)
                continue;

            string nomeObjeto = texto.gameObject.name;
            if (nomeObjeto.Contains("Rank") || nomeObjeto.Contains("Classifica") || nomeObjeto.Contains("Pontu"))
            {
                textoExistente = texto;
                break;
            }
        }

        if (textoExistente != null)
            return textoExistente;

        Transform area = EncontrarTransformPorNome("Area", "Rank");
        if (area == null)
            area = EncontrarTransformPorNome("Rank");
        if (area == null)
            return null;

        GameObject novoTexto = new GameObject("TextoRankingTop5", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = novoTexto.GetComponent<RectTransform>();
        rect.SetParent(area, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI textoCriado = novoTexto.GetComponent<TextMeshProUGUI>();
        textoCriado.enableWordWrapping = true;
        textoCriado.alignment = TextAlignmentOptions.TopLeft;
        textoCriado.fontSize = 28f;
        textoCriado.color = Color.black;
        textoCriado.text = string.Empty;
        return textoCriado;
    }

    private Transform EncontrarTransformPorNome(string nomeFilho, string nomePaiContem = null)
    {
        Transform[] todos = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform item in todos)
        {
            if (item == null)
                continue;

            bool nomeConfere = item.name == nomeFilho;
            bool paiConfere = string.IsNullOrWhiteSpace(nomePaiContem)
                || (item.parent != null && item.parent.name.Contains(nomePaiContem));

            if (nomeConfere && paiConfere)
                return item;
        }

        return null;
    }

    private void BloquearComandos(bool bloquear)
    {
        _acaoBloqueada = bloquear;
        Time.timeScale = bloquear ? 0f : 1f;

        GarantirReferencias();
        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = !bloquear;
            _canvasGroup.blocksRaycasts = !bloquear;
        }

        if (_eventSystem != null)
            _eventSystem.enabled = !bloquear;
    }
}
