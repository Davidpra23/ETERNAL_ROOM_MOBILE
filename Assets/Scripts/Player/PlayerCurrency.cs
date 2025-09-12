using UnityEngine;

public class PlayerCurrency : MonoBehaviour
{
    public static PlayerCurrency Instance { get; private set; }

    [Header("Currency Settings")]
    [SerializeField] private int startingCoins = 0;
    [SerializeField] private int currentCoins;

    // Eventos para UI y otros sistemas
    public System.Action<int> OnCoinsChanged;
    public System.Action<int> OnCoinsAdded;
    public System.Action<int> OnCoinsSpent;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentCoins = startingCoins;
    }

    // Añadir monedas
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        currentCoins += amount;
        NotifyCoinsChanged();
        OnCoinsAdded?.Invoke(amount);
        
        Debug.Log($"Added {amount} coins. Total: {currentCoins}");
    }

    // Gastar monedas
    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || !HasEnoughCoins(amount)) return false;

        currentCoins -= amount;
        NotifyCoinsChanged();
        OnCoinsSpent?.Invoke(amount);
        
        Debug.Log($"Spent {amount} coins. Remaining: {currentCoins}");
        return true;
    }

    // Verificar si tiene suficientes monedas
    public bool HasEnoughCoins(int amount)
    {
        return currentCoins >= amount;
    }

    // Obtener monedas actuales
    public int GetCurrentCoins()
    {
        return currentCoins;
    }

    // Resetear monedas
    public void ResetCoins()
    {
        currentCoins = startingCoins;
        NotifyCoinsChanged();
    }

    // Guardar monedas (para persistencia entre sesiones)
    public void SaveCoins()
    {
        PlayerPrefs.SetInt("PlayerCoins", currentCoins);
        PlayerPrefs.Save();
    }

    // Cargar monedas guardadas
    public void LoadCoins()
    {
        if (PlayerPrefs.HasKey("PlayerCoins"))
        {
            currentCoins = PlayerPrefs.GetInt("PlayerCoins");
            NotifyCoinsChanged();
        }
    }

    private void NotifyCoinsChanged()
    {
        OnCoinsChanged?.Invoke(currentCoins);
    }

    // Métodos para debugging
    [ContextMenu("Add 100 Coins")]
    private void DebugAddCoins()
    {
        AddCoins(100);
    }

    [ContextMenu("Spend 50 Coins")]
    private void DebugSpendCoins()
    {
        SpendCoins(50);
    }

    [ContextMenu("Reset Coins")]
    private void DebugResetCoins()
    {
        ResetCoins();
    }

    void OnApplicationQuit()
    {
        SaveCoins();
    }
}