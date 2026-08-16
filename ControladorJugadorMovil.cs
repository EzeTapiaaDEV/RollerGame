using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ControladorJugadorMovil : MonoBehaviour
{
    [Header("Cámara y Animación")]
    public Camera camaraPrimeraPersona;
    public Animator animatorJugador;

    [Header("Arma y Socket Personal")]
    public GameObject revolverPropio;     // El revólver que está en la mesa
    public Transform manoSocket;          // El punto/hueso de la mano donde se acomodará después

    // Variables internas para guardar posición, rotación y escala original en la mesa
    private Vector3 posicionOriginalMesa;
    private Quaternion rotacionOriginalMesa;
    private Vector3 escalaOriginalMesa;
    private Transform padreOriginalMesa;

    [Header("Configuración de la Cabeza (IK)")]
    [Range(0f, 1f)] public float pesoMiradaCabeza = 1.0f;

    [Header("Objetos de Referencia para la Cámara")]
    public Transform objetivoMazoTransform;

    [Header("Configuración de Enfoque de Cámara")]
    public Vector3 rotacionMirarCartas = new Vector3(45f, 0f, 0f); 
    public Vector3 rotacionEnfoqueMano = new Vector3(30f, 0f, 0f);

    [Header("Sensibilidad Touch (Móvil)")]
    public float sensibilidadTouch = 0.15f;
    public float limiteHorizontal = 70f;
    public float limiteVertical = 50f;

    [Header("Botones de UI")]
    public Button botonVerCartas;
    public Button botonDesconfiar;

    [Header("Nombres Exactos de Parámetros Animator")]
    public string paramMirarCartas = "Mirar Cartas"; 
    public string paramTirarCartas = "TirarCartas";  
    public string paramDesconfiar = "Desconfi";      

    private float rotacionX = 0f;
    private float rotacionY = 0f;
    private bool mirandoCartas = false;
    private Coroutine rutinaTransicionCamara;

    private Vector3 posicionInicialJugador;

    void Start()
    {
        posicionInicialJugador = transform.position;
    
        if (camaraPrimeraPersona != null)
        {
            Vector3 rot =
                camaraPrimeraPersona.transform.localRotation.eulerAngles;
    
            rotacionX = rot.x;
            rotacionY = rot.y;
        }
    
        if (botonVerCartas != null)
            botonVerCartas.onClick.AddListener(ToggleMirarCartas);
    
        if (botonDesconfiar != null)
            botonDesconfiar.onClick.AddListener(AccionDesconfiar);
    
        GuardarDatosRevolver();
    }

    public void EquiparRevolverEnMano()
    {
        if (revolverPropio == null || manoSocket == null)
        {
            Debug.LogError("Falta el revólver o el manoSocket.");
            return;
        }
    
        ActivarFisicaRevolver(false);
    
        revolverPropio.transform.SetParent(manoSocket, false);
        revolverPropio.transform.localPosition = Vector3.zero;
        revolverPropio.transform.localRotation = Quaternion.identity;
        revolverPropio.transform.localScale = Vector3.one;
    }

    public void RegresarRevolverAMesa()
    {
        if (revolverPropio == null)
        {
            Debug.LogError("No hay revólver que devolver a la mesa.");
            return;
        }
    
        revolverPropio.transform.SetParent(padreOriginalMesa, true);
        revolverPropio.transform.position = posicionOriginalMesa;
        revolverPropio.transform.rotation = rotacionOriginalMesa;
        revolverPropio.transform.localScale = escalaOriginalMesa;
    
        ActivarFisicaRevolver(true);
    }

    void Update()
    {
        ManejarVistaTouch();

        if (transform.position != posicionInicialJugador)
        {
            transform.position = posicionInicialJugador;
        }
    }

    void ManejarVistaTouch()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.position.x > Screen.width * 0.25f && touch.phase == TouchPhase.Moved)
            {
                if (mirandoCartas) return;

                float deltaX = touch.deltaPosition.x * sensibilidadTouch;
                float deltaY = touch.deltaPosition.y * sensibilidadTouch;

                rotacionY += deltaX;
                rotacionX -= deltaY;

                rotacionX = Mathf.Clamp(rotacionX, -limiteVertical, limiteVertical);
                rotacionY = Mathf.Clamp(rotacionY, -limiteHorizontal, limiteHorizontal);

                camaraPrimeraPersona.transform.localRotation = Quaternion.Euler(rotacionX, rotacionY, 0f);
            }
        }

        #if UNITY_EDITOR
        if (Input.GetMouseButton(1))
        {
            if (mirandoCartas) return;

            rotacionY += Input.GetAxis("Mouse X") * 2f;
            rotacionX -= Input.GetAxis("Mouse Y") * 2f;

            rotacionX = Mathf.Clamp(rotacionX, -limiteVertical, limiteVertical);
            rotacionY = Mathf.Clamp(rotacionY, -limiteHorizontal, limiteHorizontal);

            camaraPrimeraPersona.transform.localRotation = Quaternion.Euler(rotacionX, rotacionY, 0f);
        }
        #endif
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animatorJugador == null) return;

        if (camaraPrimeraPersona != null && !mirandoCartas)
        {
            animatorJugador.SetLookAtWeight(pesoMiradaCabeza, 0.0f, 1.0f, 0.0f, 0.5f);

            Vector3 puntoHaciaDondeMira = camaraPrimeraPersona.transform.position + (camaraPrimeraPersona.transform.forward * 10f);
            animatorJugador.SetLookAtPosition(puntoHaciaDondeMira);
        }
        else
        {
            animatorJugador.SetLookAtWeight(0f);
        }
    }

    public void ToggleMirarCartas()
    {
        mirandoCartas = !mirandoCartas;

        if (animatorJugador != null)
        {
            animatorJugador.ResetTrigger(paramTirarCartas);
            animatorJugador.SetBool(paramMirarCartas, mirandoCartas);
        }

        ControladorCartasUI uiCartas = FindObjectOfType<ControladorCartasUI>();
        if (uiCartas != null)
        {
            if (mirandoCartas) uiCartas.MostrarUI();
            else uiCartas.OcultarUI();
        }

        if (rutinaTransicionCamara != null) StopCoroutine(rutinaTransicionCamara);
        
        Quaternion objetivo = mirandoCartas ? Quaternion.Euler(rotacionMirarCartas) : Quaternion.Euler(0f, 0f, 0f);
        rutinaTransicionCamara = StartCoroutine(TransicionarCamara(objetivo, 0.4f));
    }

    public void EjecutarAnimacionLanzar()
    {
        if (AudioManager.instancia != null) AudioManager.instancia.ReproducirSonido(AudioManager.instancia.clipTirarCartas);

        mirandoCartas = false;

        ControladorCartasUI uiCartas = FindObjectOfType<ControladorCartasUI>();
        if (uiCartas != null) uiCartas.OcultarUI();

        if (rutinaTransicionCamara != null) StopCoroutine(rutinaTransicionCamara);
        rutinaTransicionCamara = StartCoroutine(SecuenciaTirarSincronizada());
    }

    IEnumerator SecuenciaTirarSincronizada()
    {
        if (camaraPrimeraPersona == null) yield break;

        Quaternion rotInicial = camaraPrimeraPersona.transform.localRotation;
        Quaternion rotMano = Quaternion.Euler(rotacionEnfoqueMano);

        Quaternion rotMazo;
        if (objetivoMazoTransform != null)
        {
            Vector3 direccionHaciaMazo = objetivoMazoTransform.position - camaraPrimeraPersona.transform.position;
            Quaternion rotWorldMazo = Quaternion.LookRotation(direccionHaciaMazo);
            rotMazo = Quaternion.Inverse(camaraPrimeraPersona.transform.parent != null ? camaraPrimeraPersona.transform.parent.rotation : Quaternion.identity) * rotWorldMazo;
        }
        else
        {
            rotMazo = Quaternion.Euler(15f, 0f, 0f);
        }

        float tiempo = 0f;
        float duracionEnfoque = 0.2f;
        while (tiempo < duracionEnfoque)
        {
            tiempo += Time.deltaTime;
            camaraPrimeraPersona.transform.localRotation = Quaternion.Slerp(rotInicial, rotMano, tiempo / duracionEnfoque);
            yield return null;
        }
        camaraPrimeraPersona.transform.localRotation = rotMano;

        if (animatorJugador != null)
        {
            animatorJugador.SetBool(paramMirarCartas, false);
            animatorJugador.ResetTrigger(paramTirarCartas);
            animatorJugador.Update(0f);

            int indiceCapa = animatorJugador.GetLayerIndex("UpperBody");
            if (indiceCapa == -1) indiceCapa = 0; 

            animatorJugador.Play("Tirar Cartas", indiceCapa, 0f);
        }

        tiempo = 0f;
        float duracionTirada = 0.35f;
        while (tiempo < duracionTirada)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracionTirada;
            float tSuave = Mathf.SmoothStep(0f, 1f, t);

            camaraPrimeraPersona.transform.localRotation = Quaternion.Slerp(rotMano, rotMazo, tSuave);
            yield return null;
        }

        camaraPrimeraPersona.transform.localRotation = rotMazo;

        Vector3 eulerFinal = rotMazo.eulerAngles;
        rotacionX = eulerFinal.x > 180 ? eulerFinal.x - 360 : eulerFinal.x;
        rotacionY = eulerFinal.y > 180 ? eulerFinal.y - 360 : eulerFinal.y;
    }

    IEnumerator TransicionarCamara(Quaternion rotFinal, float duracion)
    {
        if (camaraPrimeraPersona == null) yield break;

        Quaternion rotInicial = camaraPrimeraPersona.transform.localRotation;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;
            camaraPrimeraPersona.transform.localRotation = Quaternion.Slerp(rotInicial, rotFinal, t);
            yield return null;
        }

        camaraPrimeraPersona.transform.localRotation = rotFinal;
        if (!mirandoCartas)
        {
            rotacionX = 0f;
            rotacionY = 0f;
        }
    }

    public void AccionDesconfiar()
    {
        if (AudioManager.instancia != null) AudioManager.instancia.ReproducirSonido(AudioManager.instancia.clipDesconfiar);

        if (mirandoCartas) ToggleMirarCartas();

        if (animatorJugador != null)
        {
            animatorJugador.SetTrigger(paramDesconfiar);
        }

        TurnManager turnos = FindObjectOfType<TurnManager>();
        if (turnos != null)
        {
            turnos.EjecutarDesconfiar(0);
        }
    }

    public void PenalizarYTomarArma()
    {
        if (revolverPropio == null)
        {
            Debug.LogError("No se puede coger el revólver porque es null.");
            return;
        }
    
        EquiparRevolverEnMano();
    
        DetectorAgarre detector =
            revolverPropio.GetComponent<DetectorAgarre>();
    
        if (detector != null)
            detector.esperandoAgarre = true;
    
        StartCoroutine(RutinaSoltarArma());
    }


    private void ActivarFisicaRevolver(bool activar)
    {
        if (revolverPropio == null) return;
    
        Rigidbody rb = revolverPropio.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !activar;
    
            if (!activar)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    
        Collider[] colliders =
            revolverPropio.GetComponentsInChildren<Collider>();
    
        foreach (Collider col in colliders)
            col.enabled = activar;
    }

    private void GuardarDatosRevolver()
    {
        if (revolverPropio == null) return;
    
        padreOriginalMesa = revolverPropio.transform.parent;
        posicionOriginalMesa = revolverPropio.transform.position;
        rotacionOriginalMesa = revolverPropio.transform.rotation;
    
        // LocalScale, no LossyScale
        escalaOriginalMesa = revolverPropio.transform.localScale;
    }
}
