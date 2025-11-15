using UnityEngine;

public class WeaponSoundSystem : MonoBehaviour
{
    [System.Serializable]
    public class WeaponSoundConfig
    {
        public string weaponName;
        public AudioClip sonidoAtaqueNormal;
        public AudioClip sonidoAtaqueCargado;
        public float volumen = 1f;
    }

    [Header("CONFIGURACIÓN DE SONIDOS POR ARMA")]
    [SerializeField] private WeaponSoundConfig[] sonidosArmas;

    private AudioSource audioSource;
    private EquipmentManager equipmentManager;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        equipmentManager = EquipmentManager.Instance;
    }

    // 🔥 MÉTODO PRINCIPAL - Reproduce sonido según el arma actual
    public void ReproducirSonidoAtaque()
    {
        if (equipmentManager == null || equipmentManager.CurrentWeapon == null)
        {
            Debug.LogWarning("No hay arma equipada para reproducir sonido");
            return;
        }

        string nombreArmaActual = equipmentManager.CurrentWeapon.name;
        WeaponSoundConfig config = ObtenerConfiguracionArma(nombreArmaActual);

        if (config != null && config.sonidoAtaqueNormal != null)
        {
            audioSource.PlayOneShot(config.sonidoAtaqueNormal, config.volumen);
            Debug.Log($"🔊 Sonido de {config.weaponName}");
        }
        else
        {
            Debug.LogWarning($"No hay sonido configurado para: {nombreArmaActual}");
        }
    }

    // Para ataques cargados
    public void ReproducirSonidoAtaqueCargado()
    {
        if (equipmentManager == null || equipmentManager.CurrentWeapon == null) return;

        string nombreArmaActual = equipmentManager.CurrentWeapon.name;
        WeaponSoundConfig config = ObtenerConfiguracionArma(nombreArmaActual);

        if (config != null && config.sonidoAtaqueCargado != null)
        {
            audioSource.PlayOneShot(config.sonidoAtaqueCargado, config.volumen);
        }
        else
        {
            // Si no tiene sonido cargado, usa el normal
            ReproducirSonidoAtaque();
        }
    }

    private WeaponSoundConfig ObtenerConfiguracionArma(string nombreArma)
    {
        foreach (var config in sonidosArmas)
        {
            if (nombreArma.Contains(config.weaponName))
            {
                return config;
            }
        }
        return null;
    }

    [ContextMenu("🔊 PROBAR SONIDO ARMA ACTUAL")]
    public void ProbarSonidoArmaActual()
    {
        ReproducirSonidoAtaque();
    }
}