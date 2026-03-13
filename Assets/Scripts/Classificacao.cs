using UnityEngine;
using static SaveManagerPlayerPrefs;

public class Classificacao : MonoBehaviour
{
    public Transform clsssificacaoPreab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
     StartCoroutin( GameObject.Find("GameManager").transform.GetComponent<SaveManagerPlayerPrefs>().CarregarLinhaDoBanco(CarregarClassificacao) );
    }

    public void CarregarClassificacao(DadosRankingLista dados)
    {
        foreach(DadosRankingFetch item in dados.items)
        {
            Transform instaciado = Instantiate(clsssificacaoPreab, transform);
            instaciado.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = item.Nome +" | "+ item.dinheiroMax;
            //instaciado.GetComponent<ClassificacaoPrefab>().SetarDados(item.Nome, item.dinheiroMax, item.created_at);
        }
    }
}
