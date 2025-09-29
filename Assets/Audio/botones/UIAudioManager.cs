using UnityEngine;
using UnityEngine.UI;

public class UIAudioManager : MonoBehaviour
{
    public AudioSource audioSource;   // El AudioSource general
    public AudioClip clickSound;      // Sonido de clic

    void Start()
    {
        // Busca todos los botones en la escena y les añade el sonido
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button btn in buttons)
        {
            btn.onClick.AddListener(() => PlayClickSound());
        }
    }

    void PlayClickSound()
    {
        audioSource.PlayOneShot(clickSound);
    }
}
