using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour
{
    public static VirtualJoystick Instance { get; private set; }
    
    [Header("Component References")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image handleImage;
    
    [Header("Joystick Settings")]
    [SerializeField] private float handleRange = 1f;
    [SerializeField] private float deadZone = 0.2f;
    
    private Vector2 inputVector = Vector2.zero;
    private bool isActive = false;
    
    public Vector2 Direction { 
        get { 
            if (inputVector.magnitude > deadZone) 
                return inputVector; 
            return Vector2.zero; 
        } 
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Obtener referencias automáticamente si no están asignadas
        if (joystickBackground == null)
            joystickBackground = GetComponent<RectTransform>();
            
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
            
        if (joystickHandle != null && handleImage == null)
            handleImage = joystickHandle.GetComponent<Image>();
        
        // Asegurarse de que el handle esté en el centro al inicio
        ResetJoystick();
    }
    
    // Métodos públicos para el Event Trigger
    public void OnDrag(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData == null) return;
        
        Vector2 position = Vector2.zero;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, pointerData.position, pointerData.pressEventCamera, out position))
        {
            position.x /= joystickBackground.sizeDelta.x * 0.5f;
            position.y /= joystickBackground.sizeDelta.y * 0.5f;
            
            inputVector = new Vector2(position.x, position.y);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;
            
            // Mover el handle visualmente
            joystickHandle.anchoredPosition = new Vector2(
                inputVector.x * (joystickBackground.sizeDelta.x * 0.5f * handleRange),
                inputVector.y * (joystickBackground.sizeDelta.y * 0.5f * handleRange)
            );
        }
    }
    
    public void OnPointerDown(BaseEventData eventData)
    {
        OnDrag(eventData);
        isActive = true;
    }
    
    public void OnPointerUp(BaseEventData eventData)
    {
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
        isActive = false;
    }
    
    // Métodos públicos para acceso desde otros scripts
    public float Horizontal()
    {
        return Direction.x;
    }
    
    public float Vertical()
    {
        return Direction.y;
    }
    
    public void ResetJoystick()
    {
        inputVector = Vector2.zero;
        if (joystickHandle != null)
            joystickHandle.anchoredPosition = Vector2.zero;
        isActive = false;
    }
}