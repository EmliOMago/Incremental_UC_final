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
    [Tooltip("Quantida a cada click")] public float valorGanhosPassivos;
    [Space]
    [Header("Preços")]
    [Tooltip("Preço base do multiplicador")] public float precoBaseMultiplicador;
    [Tooltip("multiplicador do multiplicador")] public float multiplicadorMultiplicador;
    [Space]
    [Tooltip("Preço base do ganho passivo")] public float precoBaseGanhoPassivo;
    [Tooltip("multiplicador do ganho passivo")] public float multiplicadorGanhoPassivo;

    public static event System.Action OnDinheiroMudar;

    public void AddDinheiro(float valor)
    {
        dinheiro += valor;

        Debug.Log(dinheiro);
        OnDinheiroMudar?.Invoke();
    }

    public void ClickDrink()
    {
        float valor = 1;
        float multiplicador = 1 + (valorMultiplicador * qntMultiplicadores);

        valor = valor * multiplicador;

        AddDinheiro(valor);
    }

    public void ComprarMultiplicador()
    {
        float preco = qntMultiplicadores * precoBaseMultiplicador * multiplicadorMultiplicador;

        if (dinheiro < preco)
        {
            Debug.Log("Dinheiro insuficiente para comprar multiplicador.");
            return;
        }
        dinheiro = dinheiro - preco;
        OnDinheiroMudar?.Invoke();

        /*AddDinheiro(-preco);*/
        qntMultiplicadores +=1;

        GameDirector.instancia.hudManeger.AtualizarMutiplicador();
    }

    public void ComprarGanhosPassivos()
    {
        float preco = 20 * Mathf.Pow(2, qntGanhosPassivos);
        if (dinheiro < preco)
        {
            Debug.Log("Dinheiro insuficiente para comprar ganho passivo.");
            return;
        }

        AddDinheiro(-preco);
        qntGanhosPassivos++;

        GameDirector.instancia.hudManeger.AtualizarGanhoPassivo();
    }

    public float ObtemPrecoMultiplicador()
    {
        float preco = qntMultiplicadores * precoBaseMultiplicador * multiplicadorMultiplicador;
        if (preco <= 0)
            preco = precoBaseMultiplicador;

        return preco;
    }

    public float ObtemPrecoGanhoPassivo()
    {         
        float preco = qntGanhosPassivos * precoBaseGanhoPassivo * multiplicadorGanhoPassivo;
        if (preco <= 0)
            preco = precoBaseGanhoPassivo;
        return preco;
    }
}
    
    
