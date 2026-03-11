using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class HUDManeger : MonoBehaviour
{
    public enum CategoriaMelhoria
    {
        Ativo = 0,
        Passivo = 1
    }

    [Serializable]
    public class DefinicaoMelhoria
    {
        [Header("Identificação")]
        [Tooltip("Número usado para montar as chaves automáticas da tabela: upgrade.{indice}.title / description / value")]
        public int indiceTabela;

        [Tooltip("Categoria da melhoria. Ativo influencia o clique. Passivo influencia o ganho automático.")]
        public CategoriaMelhoria categoria;

        [Header("Economia")]
        [Tooltip("Preço inicial da melhoria.")]
        public float precoBase = 10f;

        [Tooltip("Progressão do preço a cada compra.")]
        public float multiplicadorPreco = 1.15f;

        [Tooltip("Percentual multiplicativo aplicado por compra. Ex.: 0.5 = 50% por compra.")]
        public float percentualPorCompra = 0.5f;

        [Tooltip("Se verdadeiro, esta melhoria já começa liberada quando não houver save.")]
        public bool liberadaNoInicio;

        [Header("Localização opcional")]
        [Tooltip("Se vazio, usa fallback automático upgrade.{indice}.title")]
        public string chaveTitulo;

        [Tooltip("Se vazio, usa fallback automático upgrade.{indice}.description")]
        public string chaveDescricao;

        [Tooltip("Se vazio, usa fallback automático upgrade.{indice}.value")]
        public string chaveValor;

        [NonSerialized] public int quantidadeComprada;

        public string ObterChaveTitulo() => string.IsNullOrWhiteSpace(chaveTitulo) ? $"upgrade.{indiceTabela}.title" : chaveTitulo;
        public string ObterChaveDescricao() => string.IsNullOrWhiteSpace(chaveDescricao) ? $"upgrade.{indiceTabela}.description" : chaveDescricao;
        public string ObterChaveValor() => string.IsNullOrWhiteSpace(chaveValor) ? $"upgrade.{indiceTabela}.value" : chaveValor;

        public float ObterPrecoAtual()
        {
            float preco = precoBase * Mathf.Pow(multiplicadorPreco, quantidadeComprada);
            return Mathf.Max(precoBase, preco);
        }

        public float ObterPercentualTotal()
        {
            if (quantidadeComprada <= 0)
                return 0f;

            float multiplicador = Mathf.Pow(1f + Mathf.Max(0f, percentualPorCompra), quantidadeComprada);
            return (multiplicador - 1f) * 100f;
        }

        public float ObterMultiplicadorTotal()
        {
            if (quantidadeComprada <= 0)
                return 1f;

            return Mathf.Pow(1f + Mathf.Max(0f, percentualPorCompra), quantidadeComprada);
        }

        public float AplicarMultiplicacaoProgressivaAoValor(float valorBase)
        {
            return Mathf.Max(0f, valorBase) * ObterMultiplicadorTotal();
        }

        public float ObterValorPassivoPorCiclo(float valorBasePorCompra)
        {
            if (categoria != CategoriaMelhoria.Passivo || quantidadeComprada <= 0)
                return 0f;

            float valorBaseTotal = Mathf.Max(0f, valorBasePorCompra) * quantidadeComprada;
            return AplicarMultiplicacaoProgressivaAoValor(valorBaseTotal);
        }
    }

    [Serializable]
    public class EstadoMelhoriaSave
    {
        public int indice;
        public int quantidade;
    }

    [Serializable]
    public class SaveMelhoriasHUD
    {
        public int quantidadeDesbloqueadas;
        public float valorReferenciaDesbloqueio;
        public float maiorDinheiroAtingido;
        public List<EstadoMelhoriaSave> melhorias = new List<EstadoMelhoriaSave>();
    }

    [Serializable]
    public class ItemUpgradeUI
    {
        public GameObject instancia;
        public Button botaoComprar;
        public TextMeshProUGUI textoTitulo;
        public TextMeshProUGUI textoDescricao;
        public TextMeshProUGUI textoValorAumentado;
        public TextMeshProUGUI textoPreco;
        public TextMeshProUGUI textoBotaoComprar;
    }

    #region ReferenciasHUD

    [Header("HUD")]
    public TextMeshProUGUI dinheiroText;

    [Header("Lista dinâmica de melhorias")]
    [Tooltip("Pai onde os prefabs de melhoria serão instanciados automaticamente.")]
    public Transform contentMelhorias;

    [Tooltip("Prefab do card de melhoria. O botão será encontrado automaticamente.")]
    public GameObject prefabMelhoria;

    [Tooltip("Tabela de localização usada para os textos das melhorias.")]
    public string nomeTabelaLocalizacao = "HUD_Upgrades";

    [Tooltip("Chave de localização usada no texto do botão de compra.")]
    public string chaveBotaoComprar = "upgrade.acquire";

    [Tooltip("Melhorias adicionais criadas por você no inspector. As 6 melhorias fallback são adicionadas automaticamente.")]
    public List<DefinicaoMelhoria> melhoriasManuais = new List<DefinicaoMelhoria>();

    #endregion

    #region TelaDeCarregamento

    [Header("Tela de carregamento")]
    [Tooltip("Objeto raiz opcional da tela de carregamento. Pode conter a imagem e o slider.")]
    public GameObject telaCarregamentoRaiz;

    [Tooltip("Imagem de tela cheia definida no inspector. Ela permanece ativa até o jogo terminar de carregar save, HUD e melhorias.")]
    public Image imagemTelaCarregamento;

    [Tooltip("Slider visual do carregamento. Ele será controlado de 0 a 100.")]
    public Slider sliderCarregamento;

    #endregion

    #region EconomiaBase

    [Header("Economia base")]
    [Tooltip("Valor base ganho por drink manual antes dos multiplicadores.")]
    public float valorBaseDrinkManual = 1f;

    [Tooltip("Valor base por ciclo de cada compra passiva antes dos multiplicadores.")]
    public float valorBaseDrinkPassivoPorCompra = 1f;

    [Header("Desbloqueio progressivo")]
    [Tooltip("Multiplicador necessário para liberar a próxima melhoria.")]
    public float multiplicadorDesbloqueio = 100f;

    [Tooltip("Valor mínimo usado como primeira referência de desbloqueio.")]
    public float valorInicialReferenciaDesbloqueio = 1f;

    #endregion

    #region EstadoInterno

    private readonly List<DefinicaoMelhoria> _melhorias = new List<DefinicaoMelhoria>();
    private readonly Dictionary<int, ItemUpgradeUI> _itensInstanciados = new Dictionary<int, ItemUpgradeUI>();
    private readonly Dictionary<string, LocalizedString> _cacheStringsLocalizadas = new Dictionary<string, LocalizedString>();

    private bool _foiInicializado;
    private bool _estaInicializando;
    private bool _estaAtualizandoHUD;
    private bool _listaVisualSuja = true;

    private bool _carregamentoConcluido;
    private bool _carregamentoEmAndamento;
    private Coroutine _rotinaCarregamento;

    private int _quantidadeDesbloqueadas;
    private float _valorReferenciaDesbloqueio = 1f;
    private float _maiorDinheiroAtingido;

    #endregion

    #region CicloDeVida

    private void OnEnable()
    {
        LevelManenger.OnDinheiroMudar += AoDinheiroMudarNoHUD;
        LocalizationSettings.SelectedLocaleChanged += AoMudarLocale;
    }

    private void OnDisable()
    {
        LevelManenger.OnDinheiroMudar -= AoDinheiroMudarNoHUD;
        LocalizationSettings.SelectedLocaleChanged -= AoMudarLocale;
    }

    private void Start()
    {
        IniciarFluxoDeCarregamentoInicial();
    }

    private void AoMudarLocale(UnityEngine.Localization.Locale _)
    {
        if (_carregamentoEmAndamento)
            return;

        AtualizarTudo();
    }

    private void AoDinheiroMudarNoHUD()
    {
        AtualizarDinheiro();

        if (_carregamentoEmAndamento || !_foiInicializado)
            return;

        VerificarDesbloqueios();
    }

    #endregion

    #region InicializacaoECarregamento

    public void IniciarFluxoDeCarregamentoInicial()
    {
        if (_carregamentoConcluido || _carregamentoEmAndamento || !isActiveAndEnabled)
            return;

        if (_rotinaCarregamento != null)
            StopCoroutine(_rotinaCarregamento);

        _rotinaCarregamento = StartCoroutine(RotinaCarregamentoInicial());
    }

    private IEnumerator RotinaCarregamentoInicial()
    {
        _carregamentoEmAndamento = true;

        MostrarTelaCarregamento(true);
        DefinirProgressoCarregamento(0f);
        yield return null;

        if (GameDirector.instancia != null)
            GameDirector.instancia.AtualizarReferenciasDaCena();

        DefinirProgressoCarregamento(10f);
        yield return null;

        GarantirEstruturaInicializada();

        DefinirProgressoCarregamento(30f);
        yield return null;

        if (GameDirector.instancia != null && GameDirector.instancia.saveManager != null)
            GameDirector.instancia.saveManager.CarregarOuCriarNovoSave(false);

        DefinirProgressoCarregamento(65f);
        yield return null;

        SincronizarListaVisualSeNecessario();

        DefinirProgressoCarregamento(80f);
        yield return null;

        AtualizarTudo();

        DefinirProgressoCarregamento(90f);
        yield return null;

        if (GameDirector.instancia != null && GameDirector.instancia.levelManenger != null)
            GameDirector.instancia.levelManenger.ConcluirInicializacaoCenaJogo();

        DefinirProgressoCarregamento(100f);
        yield return null;

        MostrarTelaCarregamento(false);

        _carregamentoConcluido = true;
        _carregamentoEmAndamento = false;
        _rotinaCarregamento = null;
    }

    public void GarantirInicializado()
    {
        GarantirEstruturaInicializada();
        SincronizarListaVisualSeNecessario();
    }

    private void GarantirEstruturaInicializada()
    {
        if (_foiInicializado || _estaInicializando)
            return;

        _estaInicializando = true;

        try
        {
            CriarListaFallbackSeNecessario();
            AplicarMelhoriasManuais();
            OrdenarMelhorias();
            InicializarEstadoPadraoSeNecessario();
            _foiInicializado = true;
            MarcarListaVisualComoSuja();
        }
        finally
        {
            _estaInicializando = false;
        }
    }

    private void MostrarTelaCarregamento(bool mostrar)
    {
        if (telaCarregamentoRaiz != null)
            telaCarregamentoRaiz.SetActive(mostrar);

        if (imagemTelaCarregamento != null)
            imagemTelaCarregamento.gameObject.SetActive(mostrar);

        if (sliderCarregamento != null)
            sliderCarregamento.gameObject.SetActive(mostrar);
    }

    private void DefinirProgressoCarregamento(float percentual)
    {
        if (sliderCarregamento == null)
            return;

        sliderCarregamento.minValue = 0f;
        sliderCarregamento.maxValue = 100f;
        sliderCarregamento.SetValueWithoutNotify(Mathf.Clamp(percentual, 0f, 100f));
    }

    #endregion

    #region SaveEEstado

    private void CriarListaFallbackSeNecessario()
    {
        _melhorias.Clear();

        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 0, categoria = CategoriaMelhoria.Ativo, precoBase = 10f, multiplicadorPreco = 1.18f, percentualPorCompra = 0.50f, liberadaNoInicio = true });
        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 1, categoria = CategoriaMelhoria.Passivo, precoBase = 100f, multiplicadorPreco = 1.20f, percentualPorCompra = 0.35f, liberadaNoInicio = false });
        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 2, categoria = CategoriaMelhoria.Ativo, precoBase = 1000f, multiplicadorPreco = 1.22f, percentualPorCompra = 0.75f, liberadaNoInicio = false });
        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 3, categoria = CategoriaMelhoria.Passivo, precoBase = 10000f, multiplicadorPreco = 1.25f, percentualPorCompra = 0.60f, liberadaNoInicio = false });
        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 4, categoria = CategoriaMelhoria.Ativo, precoBase = 100000f, multiplicadorPreco = 1.28f, percentualPorCompra = 1.00f, liberadaNoInicio = false });
        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 5, categoria = CategoriaMelhoria.Passivo, precoBase = 1000000f, multiplicadorPreco = 1.30f, percentualPorCompra = 0.90f, liberadaNoInicio = false });
    }

    private void AplicarMelhoriasManuais()
    {
        if (melhoriasManuais == null)
            return;

        foreach (DefinicaoMelhoria melhoriaManual in melhoriasManuais)
        {
            if (melhoriaManual == null)
                continue;

            bool jaExiste = false;
            for (int i = 0; i < _melhorias.Count; i++)
            {
                if (_melhorias[i].indiceTabela == melhoriaManual.indiceTabela)
                {
                    _melhorias[i] = melhoriaManual;
                    jaExiste = true;
                    break;
                }
            }

            if (!jaExiste)
                _melhorias.Add(melhoriaManual);
        }
    }

    private void OrdenarMelhorias()
    {
        _melhorias.Sort((a, b) => a.indiceTabela.CompareTo(b.indiceTabela));
    }

    private void InicializarEstadoPadraoSeNecessario()
    {
        _quantidadeDesbloqueadas = Mathf.Clamp(ContarLiberadasNoInicio(), 1, Mathf.Max(1, _melhorias.Count));
        _valorReferenciaDesbloqueio = Mathf.Max(1f, valorInicialReferenciaDesbloqueio);
        _maiorDinheiroAtingido = 0f;
    }

    private int ContarLiberadasNoInicio()
    {
        int quantidade = 0;
        for (int i = 0; i < _melhorias.Count; i++)
        {
            if (_melhorias[i].liberadaNoInicio)
                quantidade++;
        }

        return quantidade;
    }

    public void AplicarSaveHUD(string json, bool atualizarHUD = true)
    {
        GarantirEstruturaInicializada();

        foreach (DefinicaoMelhoria melhoria in _melhorias)
            melhoria.quantidadeComprada = 0;

        if (!string.IsNullOrWhiteSpace(json))
        {
            SaveMelhoriasHUD save = JsonUtility.FromJson<SaveMelhoriasHUD>(json);
            if (save != null)
            {
                _quantidadeDesbloqueadas = Mathf.Clamp(save.quantidadeDesbloqueadas, 1, Mathf.Max(1, _melhorias.Count));
                _valorReferenciaDesbloqueio = Mathf.Max(1f, save.valorReferenciaDesbloqueio);
                _maiorDinheiroAtingido = Mathf.Max(0f, save.maiorDinheiroAtingido);

                if (save.melhorias != null)
                {
                    foreach (EstadoMelhoriaSave estado in save.melhorias)
                    {
                        DefinicaoMelhoria melhoria = ObterMelhoriaPorIndice(estado.indice);
                        if (melhoria != null)
                            melhoria.quantidadeComprada = Mathf.Max(0, estado.quantidade);
                    }
                }
            }
        }
        else
        {
            InicializarEstadoPadraoSeNecessario();
        }

        _quantidadeDesbloqueadas = Mathf.Clamp(_quantidadeDesbloqueadas, 1, Mathf.Max(1, _melhorias.Count));
        MarcarListaVisualComoSuja();

        if (atualizarHUD)
            AtualizarTudo();
    }

    public string ExportarSaveHUD()
    {
        GarantirEstruturaInicializada();

        SaveMelhoriasHUD save = new SaveMelhoriasHUD
        {
            quantidadeDesbloqueadas = _quantidadeDesbloqueadas,
            valorReferenciaDesbloqueio = _valorReferenciaDesbloqueio,
            maiorDinheiroAtingido = _maiorDinheiroAtingido,
            melhorias = new List<EstadoMelhoriaSave>()
        };

        foreach (DefinicaoMelhoria melhoria in _melhorias)
        {
            save.melhorias.Add(new EstadoMelhoriaSave
            {
                indice = melhoria.indiceTabela,
                quantidade = melhoria.quantidadeComprada
            });
        }

        return JsonUtility.ToJson(save);
    }

    public DadosEconomiaJogo CriarDadosEconomia(float dinheiroAtual)
    {
        GarantirEstruturaInicializada();

        DadosEconomiaJogo dados = new DadosEconomiaJogo
        {
            dinheiroAtual = Mathf.Max(0f, dinheiroAtual),
            totalComprasMelhorias = ObterTotalComprasMelhorias(),
            quantidadeMelhoriasDesbloqueadas = _quantidadeDesbloqueadas,
            valorReferenciaDesbloqueio = _valorReferenciaDesbloqueio,
            maiorDinheiroAtingido = _maiorDinheiroAtingido,
            melhorias = new List<DadosMelhoriaEconomia>()
        };

        foreach (DefinicaoMelhoria melhoria in _melhorias)
        {
            dados.melhorias.Add(new DadosMelhoriaEconomia
            {
                indiceTabela = melhoria.indiceTabela,
                idMelhoria = $"upgrade.{melhoria.indiceTabela}",
                categoria = melhoria.categoria.ToString(),
                quantidadeComprada = melhoria.quantidadeComprada,
                precoBase = melhoria.precoBase,
                precoAtual = melhoria.ObterPrecoAtual(),
                multiplicadorPreco = melhoria.multiplicadorPreco,
                percentualPorCompra = melhoria.percentualPorCompra,
                multiplicadorTotal = melhoria.ObterMultiplicadorTotal()
            });
        }

        return dados;
    }

    public void AplicarDadosEconomia(DadosEconomiaJogo dados, bool atualizarHUD = true)
    {
        GarantirEstruturaInicializada();

        foreach (DefinicaoMelhoria melhoria in _melhorias)
            melhoria.quantidadeComprada = 0;

        if (dados == null)
        {
            InicializarEstadoPadraoSeNecessario();
            MarcarListaVisualComoSuja();

            if (atualizarHUD)
                AtualizarTudo();

            return;
        }

        _quantidadeDesbloqueadas = Mathf.Clamp(dados.quantidadeMelhoriasDesbloqueadas, 1, Mathf.Max(1, _melhorias.Count));
        _valorReferenciaDesbloqueio = Mathf.Max(1f, dados.valorReferenciaDesbloqueio);
        _maiorDinheiroAtingido = Mathf.Max(0f, dados.maiorDinheiroAtingido);

        if (dados.melhorias != null)
        {
            foreach (DadosMelhoriaEconomia estado in dados.melhorias)
            {
                DefinicaoMelhoria melhoria = ObterMelhoriaPorIndice(estado.indiceTabela);
                if (melhoria != null)
                    melhoria.quantidadeComprada = Mathf.Max(0, estado.quantidadeComprada);
            }
        }

        _quantidadeDesbloqueadas = Mathf.Clamp(_quantidadeDesbloqueadas, 1, Mathf.Max(1, _melhorias.Count));
        MarcarListaVisualComoSuja();

        if (atualizarHUD)
            AtualizarTudo();
    }

    #endregion

    #region ListaVisual

    private void MarcarListaVisualComoSuja()
    {
        _listaVisualSuja = true;
    }

    private void SincronizarListaVisualSeNecessario()
    {
        if (!_listaVisualSuja)
            return;

        RecriarListaVisual();
        _listaVisualSuja = false;
    }

    private void RecriarListaVisual()
    {
        if (contentMelhorias == null || prefabMelhoria == null)
            return;

        if (_itensInstanciados.Count == 0)
            LimparFilhosPreExistentesDoContainer();

        LimparReferenciasInvalidasDosItens();

        HashSet<int> indicesVisiveis = new HashSet<int>();
        int limite = Mathf.Min(_quantidadeDesbloqueadas, _melhorias.Count);

        for (int i = 0; i < limite; i++)
        {
            DefinicaoMelhoria melhoria = _melhorias[i];
            ItemUpgradeUI item = ObterOuCriarItem(melhoria);
            if (item == null || item.instancia == null)
                continue;

            item.instancia.name = $"Melhoria_{melhoria.indiceTabela}";
            item.instancia.SetActive(true);
            item.instancia.transform.SetSiblingIndex(i);
            indicesVisiveis.Add(melhoria.indiceTabela);
        }

        foreach (KeyValuePair<int, ItemUpgradeUI> par in _itensInstanciados)
        {
            if (indicesVisiveis.Contains(par.Key))
                continue;

            if (par.Value != null && par.Value.instancia != null)
                par.Value.instancia.SetActive(false);
        }
    }

    private void LimparFilhosPreExistentesDoContainer()
    {
        for (int i = contentMelhorias.childCount - 1; i >= 0; i--)
            Destroy(contentMelhorias.GetChild(i).gameObject);
    }

    private void LimparReferenciasInvalidasDosItens()
    {
        List<int> indicesInvalidos = null;

        foreach (KeyValuePair<int, ItemUpgradeUI> par in _itensInstanciados)
        {
            if (par.Value != null && par.Value.instancia != null)
                continue;

            indicesInvalidos ??= new List<int>();
            indicesInvalidos.Add(par.Key);
        }

        if (indicesInvalidos == null)
            return;

        foreach (int indice in indicesInvalidos)
            _itensInstanciados.Remove(indice);
    }

    private ItemUpgradeUI ObterOuCriarItem(DefinicaoMelhoria melhoria)
    {
        if (_itensInstanciados.TryGetValue(melhoria.indiceTabela, out ItemUpgradeUI item) && item != null && item.instancia != null)
            return item;

        item = InstanciarItem(melhoria);
        _itensInstanciados[melhoria.indiceTabela] = item;
        return item;
    }

    private ItemUpgradeUI InstanciarItem(DefinicaoMelhoria melhoria)
    {
        GameObject instancia = Instantiate(prefabMelhoria, contentMelhorias);
        instancia.name = $"Melhoria_{melhoria.indiceTabela}";

        Button botaoComprar = EncontrarBotaoPorNomeOuCaminho(instancia.transform, "Corpo/Button", "Button");

        ItemUpgradeUI item = new ItemUpgradeUI
        {
            instancia = instancia,
            botaoComprar = botaoComprar,
            textoTitulo = EncontrarTMPPorNomeOuCaminho(instancia.transform, "Cabecalho/Textos/Titulo", "Titulo"),
            textoDescricao = EncontrarTMPPorNomeOuCaminho(instancia.transform, "Cabecalho/Textos/Descricao", "Descricao"),
            textoValorAumentado = EncontrarTMPPorNomeOuCaminho(instancia.transform, "Cabecalho/Textos/Ganho", "Ganho"),
            textoPreco = EncontrarTMPPorNomeOuCaminho(instancia.transform, "Corpo/Preco", "Preco"),
            textoBotaoComprar = botaoComprar != null
                ? EncontrarTMPPorNomeOuCaminho(botaoComprar.transform, "Text (TMP)", "Text (TMP)")
                : null
        };

        if (item.botaoComprar != null)
        {
            item.botaoComprar.onClick.RemoveAllListeners();
            item.botaoComprar.onClick.AddListener(() => ComprarMelhoria(melhoria.indiceTabela));
        }

        return item;
    }

    private static Button EncontrarBotaoPorNomeOuCaminho(Transform raiz, string caminhoPreferencial, string nomeFallback)
    {
        if (raiz == null)
            return null;

        Transform porCaminho = raiz.Find(caminhoPreferencial);
        if (porCaminho != null && porCaminho.TryGetComponent(out Button botaoPorCaminho))
            return botaoPorCaminho;

        Transform porNome = EncontrarTransformRecursivo(raiz, nomeFallback);
        if (porNome != null && porNome.TryGetComponent(out Button botaoPorNome))
            return botaoPorNome;

        return raiz.GetComponentInChildren<Button>(true);
    }

    private static TextMeshProUGUI EncontrarTMPPorNomeOuCaminho(Transform raiz, string caminhoPreferencial, string nomeFallback)
    {
        if (raiz == null)
            return null;

        Transform porCaminho = raiz.Find(caminhoPreferencial);
        if (porCaminho != null && porCaminho.TryGetComponent(out TextMeshProUGUI textoPorCaminho))
            return textoPorCaminho;

        Transform porNome = EncontrarTransformRecursivo(raiz, nomeFallback);
        if (porNome != null && porNome.TryGetComponent(out TextMeshProUGUI textoPorNome))
            return textoPorNome;

        return null;
    }

    private static Transform EncontrarTransformRecursivo(Transform raiz, string nome)
    {
        if (raiz == null || string.IsNullOrWhiteSpace(nome))
            return null;

        if (raiz.name == nome)
            return raiz;

        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform encontrado = EncontrarTransformRecursivo(raiz.GetChild(i), nome);
            if (encontrado != null)
                return encontrado;
        }

        return null;
    }

    #endregion

    #region AtualizacaoHUD

    public void AtualizarTudo()
    {
        if (_estaAtualizandoHUD)
            return;

        GarantirEstruturaInicializada();
        _estaAtualizandoHUD = true;

        try
        {
            SincronizarListaVisualSeNecessario();
            AtualizarDinheiro();

            float valorCliqueFinal = CalcularValorCliqueFinal();
            float valorPassivoFinal = CalcularValorPassivoFinalPorCiclo();
            int limite = Mathf.Min(_quantidadeDesbloqueadas, _melhorias.Count);

            for (int i = 0; i < limite; i++)
                AtualizarItemVisual(_melhorias[i], valorCliqueFinal, valorPassivoFinal);
        }
        finally
        {
            _estaAtualizandoHUD = false;
        }
    }

    public void AtualizarDinheiro()
    {
        if (dinheiroText == null || GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        float dinheiroAtual = GameDirector.instancia.levelManenger.dinheiro;
        dinheiroText.text = FormatarMoeda(dinheiroAtual);
    }

    private void VerificarDesbloqueios()
    {
        if (!_foiInicializado || _carregamentoEmAndamento || _estaAtualizandoHUD || GameDirector.instancia == null || GameDirector.instancia.levelManenger == null || _melhorias.Count == 0)
            return;

        float dinheiroAtual = GameDirector.instancia.levelManenger.dinheiro;
        _maiorDinheiroAtingido = Mathf.Max(_maiorDinheiroAtingido, dinheiroAtual);

        if (_quantidadeDesbloqueadas >= _melhorias.Count || multiplicadorDesbloqueio <= 1f)
            return;

        float limiteDesbloqueio = Mathf.Max(1f, _valorReferenciaDesbloqueio) * multiplicadorDesbloqueio;
        if (_maiorDinheiroAtingido < limiteDesbloqueio)
            return;

        _quantidadeDesbloqueadas = Mathf.Clamp(_quantidadeDesbloqueadas + 1, 1, _melhorias.Count);
        _valorReferenciaDesbloqueio = Mathf.Max(1f, _maiorDinheiroAtingido);

        MarcarListaVisualComoSuja();
        AtualizarTudo();
    }

    private void AtualizarItemVisual(DefinicaoMelhoria melhoria, float valorCliqueFinal, float valorPassivoFinal)
    {
        if (!_itensInstanciados.TryGetValue(melhoria.indiceTabela, out ItemUpgradeUI item))
            return;

        string titulo = ObterTextoLocalizado(melhoria.ObterChaveTitulo(), ObterFallbackTitulo(melhoria));
        string descricao = ObterTextoLocalizado(
            melhoria.ObterChaveDescricao(),
            ObterFallbackDescricao(melhoria),
            FormatarPercentual(melhoria.ObterPercentualTotal()));
        string valor = ObterTextoLocalizado(
            melhoria.ObterChaveValor(),
            ObterFallbackValor(melhoria),
            FormatarMoeda(melhoria.categoria == CategoriaMelhoria.Ativo ? valorCliqueFinal : valorPassivoFinal));

        string textoBotaoComprar = ObterTextoLocalizado(chaveBotaoComprar, "Adquirir");

        if (item.textoTitulo != null)
            item.textoTitulo.text = titulo;
        if (item.textoDescricao != null)
            item.textoDescricao.text = descricao;
        if (item.textoValorAumentado != null)
            item.textoValorAumentado.text = valor;
        if (item.textoPreco != null)
            item.textoPreco.text = FormatarMoeda(melhoria.ObterPrecoAtual());
        if (item.textoBotaoComprar != null)
            item.textoBotaoComprar.text = textoBotaoComprar;
    }

    private string ObterTextoLocalizado(string chave, string fallback, params object[] argumentos)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nomeTabelaLocalizacao))
                return argumentos != null && argumentos.Length > 0 ? string.Format(fallback, argumentos) : fallback;

            if (!_cacheStringsLocalizadas.TryGetValue(chave, out LocalizedString localizedString))
            {
                localizedString = new LocalizedString(nomeTabelaLocalizacao, chave);
                _cacheStringsLocalizadas[chave] = localizedString;
            }

            string resultado = argumentos != null && argumentos.Length > 0
                ? localizedString.GetLocalizedString(argumentos)
                : localizedString.GetLocalizedString();

            return string.IsNullOrWhiteSpace(resultado)
                ? (argumentos != null && argumentos.Length > 0 ? string.Format(fallback, argumentos) : fallback)
                : resultado;
        }
        catch
        {
            return argumentos != null && argumentos.Length > 0 ? string.Format(fallback, argumentos) : fallback;
        }
    }

    #endregion

    #region CompraECalculos

    public bool ComprarMelhoria(int indiceTabela)
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return false;

        DefinicaoMelhoria melhoria = ObterMelhoriaPorIndice(indiceTabela);
        if (melhoria == null)
            return false;

        float preco = melhoria.ObterPrecoAtual();
        LevelManenger level = GameDirector.instancia.levelManenger;
        if (level.dinheiro < preco)
            return false;

        level.AddDinheiro(-preco);
        melhoria.quantidadeComprada += 1;
        AtualizarTudo();
        return true;
    }

    public float ObterValorCliqueFinal()
    {
        GarantirEstruturaInicializada();
        return CalcularValorCliqueFinal();
    }

    public float ObterValorPassivoFinalPorCiclo()
    {
        GarantirEstruturaInicializada();
        return CalcularValorPassivoFinalPorCiclo();
    }

    public float ObterMultiplicadorCategoria(CategoriaMelhoria categoria)
    {
        GarantirEstruturaInicializada();

        float acumulado = 1f;
        foreach (DefinicaoMelhoria melhoria in _melhorias)
        {
            if (melhoria.categoria != categoria)
                continue;

            acumulado *= melhoria.ObterMultiplicadorTotal();
        }

        return Mathf.Max(1f, acumulado);
    }

    public int ObterTotalComprasMelhorias()
    {
        GarantirEstruturaInicializada();

        int total = 0;
        foreach (DefinicaoMelhoria melhoria in _melhorias)
            total += melhoria.quantidadeComprada;

        return total;
    }

    public int ObterQuantidadeCompradaMelhoria(int indiceTabela)
    {
        GarantirEstruturaInicializada();

        DefinicaoMelhoria melhoria = ObterMelhoriaPorIndice(indiceTabela);
        return melhoria != null ? Mathf.Max(0, melhoria.quantidadeComprada) : 0;
    }

    public int ObterTotalComprasPorCategoria(CategoriaMelhoria categoria)
    {
        GarantirEstruturaInicializada();

        int total = 0;
        foreach (DefinicaoMelhoria melhoria in _melhorias)
        {
            if (melhoria.categoria == categoria)
                total += melhoria.quantidadeComprada;
        }

        return total;
    }

    private DefinicaoMelhoria ObterMelhoriaPorIndice(int indiceTabela)
    {
        for (int i = 0; i < _melhorias.Count; i++)
        {
            if (_melhorias[i].indiceTabela == indiceTabela)
                return _melhorias[i];
        }

        return null;
    }

    private float CalcularValorCliqueFinal()
    {
        float valor = Mathf.Max(0f, valorBaseDrinkManual);
        foreach (DefinicaoMelhoria melhoria in _melhorias)
        {
            if (melhoria.categoria != CategoriaMelhoria.Ativo)
                continue;

            valor = melhoria.AplicarMultiplicacaoProgressivaAoValor(valor);
        }

        return valor;
    }

    private float CalcularValorPassivoFinalPorCiclo()
    {
        float total = 0f;
        foreach (DefinicaoMelhoria melhoria in _melhorias)
            total += melhoria.ObterValorPassivoPorCiclo(valorBaseDrinkPassivoPorCompra);

        return Mathf.Max(0f, total);
    }

    #endregion

    #region Formatacao

    private string ObterFallbackTitulo(DefinicaoMelhoria melhoria)
    {
        switch (melhoria.indiceTabela)
        {
            case 0: return "Canudo Turbo";
            case 1: return "Máquina de Drinks";
            case 2: return "Receita da Casa";
            case 3: return "Entrega Gelada";
            case 4: return "Cardápio VIP";
            case 5: return "Franquia Automática";
            default: return $"Melhoria {melhoria.indiceTabela}";
        }
    }

    private string ObterFallbackDescricao(DefinicaoMelhoria melhoria)
    {
        switch (melhoria.indiceTabela)
        {
            case 0: return "Aumenta o lucro por drink manual em {0}.";
            case 1: return "Multiplica o lucro automático de drinks em {0}.";
            case 2: return "Multiplica os drinks vendidos no clique em {0}.";
            case 3: return "Multiplica os drinks automáticos em {0}.";
            case 4: return "Valoriza cada drink manual em {0}.";
            case 5: return "Expande a renda automática de drinks em {0}.";
            default:
                return melhoria.categoria == CategoriaMelhoria.Ativo
                    ? "Multiplica os drinks manuais em {0}."
                    : "Multiplica os drinks automáticos em {0}.";
        }
    }

    private string ObterFallbackValor(DefinicaoMelhoria melhoria)
    {
        return melhoria.categoria == CategoriaMelhoria.Ativo
            ? "Drink manual rende {0}"
            : "Auto drinks rendem {0}";
    }

    public static string FormatarMoeda(float valor)
    {
        return "$ " + FormatarComPrefixo(valor, 2);
    }

    public static string FormatarPercentual(float valor)
    {
        return FormatarComPrefixo(valor, 2) + "%";
    }

    public static string FormatarComPrefixo(float valor, int casasDecimais)
    {
        string[] prefixos = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };
        double valorAbsoluto = Math.Abs(valor);
        double valorFormatado = valor;
        int indice = 0;

        while (valorAbsoluto >= 1000d && indice < prefixos.Length - 1)
        {
            valorFormatado /= 1000d;
            valorAbsoluto /= 1000d;
            indice++;
        }

        return valorFormatado.ToString($"F{casasDecimais}") + prefixos[indice];
    }

    #endregion
}
