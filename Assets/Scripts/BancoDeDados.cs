using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[DisallowMultipleComponent]
public class BancoDeDados : MonoBehaviour
{
    private static BancoDeDados _instancia;
    private const string UrlTabelaPadrao = "https://tjxqpqzvhplgdsujscmd.supabase.co/rest/v1/Ranckin";
    private const string ChaveSupabasePadrao = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
    // Mantém a chave real já usada no projeto, sem espalhar a lógica de banco em outros scripts.
    private const string ChaveSupabaseProjeto = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InRqeHFwcXp2aHBsZ2RzdWpzY21kIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzMzMzY5MzIsImV4cCI6MjA4ODkxMjkzMn0.Wz_dqOn58HchufPd__B5ZKj1_QFZYtqf2IshFQ4j4Fo";

    [Header("Supabase")]
    [SerializeField] private string supabaseUrl = UrlTabelaPadrao;
    [SerializeField] private string supabaseKey = ChaveSupabaseProjeto;
    [SerializeField, Min(5)] private int timeoutSegundos = 15;

    private static string _nome = string.Empty;
    private static float _dinheiroMax;

    public static BancoDeDados Instancia
    {
        get
        {
            if (_instancia != null)
                return _instancia;

            _instancia = FindFirstObjectByType<BancoDeDados>(FindObjectsInactive.Include);
            if (_instancia != null)
                return _instancia;

            GameObject raiz = new GameObject(nameof(BancoDeDados));
            _instancia = raiz.AddComponent<BancoDeDados>();
            DontDestroyOnLoad(raiz);
            return _instancia;
        }
    }

    public static string Nome
    {
        get => _nome;
        set => _nome = NormalizarNome(value);
    }

    public static float dinheiroMax
    {
        get => Mathf.Max(0f, _dinheiroMax);
        set => _dinheiroMax = Mathf.Max(0f, value);
    }

    public static float DinheiroMax
    {
        get => dinheiroMax;
        set => dinheiroMax = value;
    }

    public static bool PossuiNomeValido => !string.IsNullOrWhiteSpace(Nome);

    [Serializable]
    public class DadosRankingSalvar
    {
        public string Nome;
        public long dinheiroMax;
    }

    [Serializable]
    private class DadosRankingAtualizacao
    {
        public long dinheiroMax;
    }

    [Serializable]
    public class DadosRankingFetch
    {
        public int id;
        public string Nome;
        public float dinheiroMax;
        public string created_at;
    }

    [Serializable]
    private class DadosRankingListaWrapper
    {
        public DadosRankingFetch[] items;
    }

    private void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        _instancia = this;
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);

        if (string.IsNullOrWhiteSpace(supabaseUrl))
            supabaseUrl = UrlTabelaPadrao;

        if (string.IsNullOrWhiteSpace(supabaseKey) || supabaseKey == ChaveSupabasePadrao)
            supabaseKey = ChaveSupabaseProjeto;
    }

    private void OnDestroy()
    {
        if (_instancia == this)
            _instancia = null;
    }

    public IEnumerator VerificarSeNomeExiste(string nome, Action<bool, bool> callback)
    {
        nome = NormalizarNome(nome);
        if (string.IsNullOrWhiteSpace(nome))
        {
            callback?.Invoke(true, false);
            yield break;
        }

        bool sucesso = false;
        DadosRankingFetch registro = null;
        yield return ConsultarRegistroPorNome(nome, (consultaOk, item) =>
        {
            sucesso = consultaOk;
            registro = item;
        });

        callback?.Invoke(sucesso, registro != null);
    }

    public IEnumerator SalvarOuAtualizarRegistroAtual(string nome, float novoDinheiroMax, Action<bool> callback = null)
    {
        nome = NormalizarNome(nome);
        if (string.IsNullOrWhiteSpace(nome))
        {
            Debug.LogWarning("BancoDeDados: nome vazio. O registro online não foi salvo.");
            callback?.Invoke(false);
            yield break;
        }

        bool sucessoConsulta = false;
        DadosRankingFetch existente = null;
        yield return ConsultarRegistroPorNome(nome, (ok, item) =>
        {
            sucessoConsulta = ok;
            existente = item;
        });

        if (!sucessoConsulta)
        {
            callback?.Invoke(false);
            yield break;
        }

        float dinheiroFinal = Mathf.Max(novoDinheiroMax, existente != null ? existente.dinheiroMax : 0f);
        dinheiroMax = dinheiroFinal;
        Nome = nome;

        if (existente != null)
        {
            yield return AtualizarRegistro(nome, dinheiroFinal, callback);
            yield break;
        }

        yield return CriarRegistro(nome, dinheiroFinal, callback);
    }

    public IEnumerator ExcluirRegistroPorNome(string nome, Action<bool> callback = null)
    {
        nome = NormalizarNome(nome);
        if (string.IsNullOrWhiteSpace(nome))
        {
            callback?.Invoke(false);
            yield break;
        }

        string url = $"{supabaseUrl}?Nome=eq.{UnityWebRequest.EscapeURL(nome)}";
        using (UnityWebRequest requisicao = UnityWebRequest.Delete(url))
        {
            PrepararRequisicao(requisicao);
            requisicao.timeout = timeoutSegundos;
            requisicao.downloadHandler = new DownloadHandlerBuffer();
            requisicao.SetRequestHeader("Prefer", "return=representation");

            yield return requisicao.SendWebRequest();

            bool sucesso = requisicao.result == UnityWebRequest.Result.Success;
            if (!sucesso)
            {
                Debug.LogWarning($"BancoDeDados: erro ao excluir '{nome}': {requisicao.error}\n{requisicao.downloadHandler.text}");
            }
            else if (string.Equals(Nome, nome, StringComparison.Ordinal))
            {
                dinheiroMax = 0f;
            }

            callback?.Invoke(sucesso);
        }
    }

    public IEnumerator CarregarTop5(Action<List<DadosRankingFetch>> callback)
    {
        string url = $"{supabaseUrl}?select=Nome,dinheiroMax,created_at&order=dinheiroMax.desc,created_at.asc&limit=5";
        using (UnityWebRequest requisicao = UnityWebRequest.Get(url))
        {
            PrepararRequisicao(requisicao);
            requisicao.timeout = timeoutSegundos;

            yield return requisicao.SendWebRequest();

            if (requisicao.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"BancoDeDados: erro ao carregar ranking: {requisicao.error}\n{requisicao.downloadHandler.text}");
                callback?.Invoke(new List<DadosRankingFetch>());
                yield break;
            }

            List<DadosRankingFetch> itens = DesserializarLista(requisicao.downloadHandler.text)
                .OrderByDescending(item => item != null ? item.dinheiroMax : 0f)
                .ThenBy(item => ParseData(item != null ? item.created_at : null))
                .Take(5)
                .ToList();

            callback?.Invoke(itens);
        }
    }

    public static string FormatarDataRegistro(string createdAt)
    {
        DateTimeOffset data = ParseData(createdAt);
        if (data == DateTimeOffset.MinValue)
            return "data indisponível";

        return data.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    public static string NormalizarNome(string nome)
    {
        return string.IsNullOrWhiteSpace(nome) ? string.Empty : nome.Trim();
    }

    private IEnumerator ConsultarRegistroPorNome(string nome, Action<bool, DadosRankingFetch> callback)
    {
        string url = $"{supabaseUrl}?select=id,Nome,dinheiroMax,created_at&Nome=eq.{UnityWebRequest.EscapeURL(nome)}&limit=1";
        using (UnityWebRequest requisicao = UnityWebRequest.Get(url))
        {
            PrepararRequisicao(requisicao);
            requisicao.timeout = timeoutSegundos;

            yield return requisicao.SendWebRequest();

            if (requisicao.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"BancoDeDados: erro ao consultar '{nome}': {requisicao.error}\n{requisicao.downloadHandler.text}");
                callback?.Invoke(false, null);
                yield break;
            }

            DadosRankingFetch registro = DesserializarLista(requisicao.downloadHandler.text).FirstOrDefault();
            callback?.Invoke(true, registro);
        }
    }

    private IEnumerator CriarRegistro(string nome, float novoDinheiroMax, Action<bool> callback)
    {
        DadosRankingSalvar dados = new DadosRankingSalvar
        {
            Nome = nome,
            dinheiroMax = ConverterDinheiroParaBigInt(novoDinheiroMax)
        };

        string corpo = JsonUtility.ToJson(dados);
        using (UnityWebRequest requisicao = new UnityWebRequest(supabaseUrl, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(corpo);
            requisicao.uploadHandler = new UploadHandlerRaw(bodyRaw);
            requisicao.downloadHandler = new DownloadHandlerBuffer();
            PrepararRequisicao(requisicao);
            requisicao.timeout = timeoutSegundos;
            requisicao.SetRequestHeader("Content-Type", "application/json");
            requisicao.SetRequestHeader("Prefer", "return=representation");

            yield return requisicao.SendWebRequest();

            bool sucesso = requisicao.result == UnityWebRequest.Result.Success;
            if (!sucesso)
                Debug.LogWarning($"BancoDeDados: erro ao criar registro '{nome}': {requisicao.error}\n{requisicao.downloadHandler.text}");

            callback?.Invoke(sucesso);
        }
    }

    private IEnumerator AtualizarRegistro(string nome, float novoDinheiroMax, Action<bool> callback)
    {
        DadosRankingAtualizacao dados = new DadosRankingAtualizacao
        {
            dinheiroMax = ConverterDinheiroParaBigInt(novoDinheiroMax)
        };

        string corpo = JsonUtility.ToJson(dados);
        string url = $"{supabaseUrl}?Nome=eq.{UnityWebRequest.EscapeURL(nome)}";
        using (UnityWebRequest requisicao = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(corpo);
            requisicao.uploadHandler = new UploadHandlerRaw(bodyRaw);
            requisicao.downloadHandler = new DownloadHandlerBuffer();
            PrepararRequisicao(requisicao);
            requisicao.timeout = timeoutSegundos;
            requisicao.SetRequestHeader("Content-Type", "application/json");
            requisicao.SetRequestHeader("Prefer", "return=representation");

            yield return requisicao.SendWebRequest();

            bool sucesso = requisicao.result == UnityWebRequest.Result.Success;
            if (!sucesso)
                Debug.LogWarning($"BancoDeDados: erro ao atualizar registro '{nome}': {requisicao.error}\n{requisicao.downloadHandler.text}");

            callback?.Invoke(sucesso);
        }
    }

    private void PrepararRequisicao(UnityWebRequest requisicao)
    {
        requisicao.SetRequestHeader("apikey", supabaseKey);
        requisicao.SetRequestHeader("Authorization", "Bearer " + supabaseKey);
    }

    private static List<DadosRankingFetch> DesserializarLista(string jsonArray)
    {
        if (string.IsNullOrWhiteSpace(jsonArray))
            return new List<DadosRankingFetch>();

        string json = "{\"items\":" + jsonArray + "}";
        DadosRankingListaWrapper wrapper = JsonUtility.FromJson<DadosRankingListaWrapper>(json);
        if (wrapper == null || wrapper.items == null)
            return new List<DadosRankingFetch>();

        return new List<DadosRankingFetch>(wrapper.items);
    }


    private static long ConverterDinheiroParaBigInt(float valor)
    {
        if (float.IsNaN(valor) || float.IsInfinity(valor))
            return 0L;

        double valorSeguro = Math.Max(0d, valor);
        return checked((long)Math.Round(valorSeguro, MidpointRounding.AwayFromZero));
    }

    private static DateTimeOffset ParseData(string dataTexto)
    {
        if (string.IsNullOrWhiteSpace(dataTexto))
            return DateTimeOffset.MinValue;

        if (DateTimeOffset.TryParse(dataTexto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset data))
            return data;

        return DateTimeOffset.MinValue;
    }
}
