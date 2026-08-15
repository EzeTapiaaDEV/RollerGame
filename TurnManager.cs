using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnManager : MonoBehaviour
{
    [Header("Referencias de Scripts")]
    public MazoManager mazoManager;
    public ControladorJugadorMovil jugadorLocal;
    public List<ControladorRival> rivales = new List<ControladorRival>();
    public MecanicaRevolver revolverGeneral; // Por si lo usas para la lógica de balas

    [Header("Referencias de UI")]
    public GameObject botonLanzar;
    public GameObject botonVerCartas;
    public GameObject botonDesconfiar;
    public GameObject panelCartasUI;
    
    [Header("UI Carta Declarada")]
    public Text textoMesaDeclarada;
    public TextMeshProUGUI textoMesaDeclaradaTMP;

    [Header("Estado del Turno")]
    public int turnoActual = 0; 
    public bool esperandoAccion = false;
    public bool sePuedeDesconfiar = false;

    [Header("Registro Exclusivo del Turno Actual")]
    public int jugadorDeLaUltimaJugada = -1; 
    public List<GameObject> ultimasCartasTiradas = new List<GameObject>();
    public string cartaDeclarada = "Rey";
    public bool fueMentira = false;

    [Header("UI de Nombres")]
    public List<InfoJugadorUI> panelesInfoJugadores = new List<InfoJugadorUI>(); // Índice 0: Tú, 1+: Rivales

    private string[] figurasMesa = new string[] { "As", "Rey", "Reina" };

    void Start()
    {
        turnoActual = 0;
        esperandoAccion = false;
        sePuedeDesconfiar = false;

        if (revolverGeneral == null) revolverGeneral = FindObjectOfType<MecanicaRevolver>();
        if (jugadorLocal == null) jugadorLocal = FindObjectOfType<ControladorJugadorMovil>();

        ElegirCartaDeclaradaParaLaRonda();
        ActualizarEstadoUI();
        RefrescarNombresUI();
    }

    void Update()
    {
        if (turnoActual == 0 && !esperandoAccion && rivales.Count >= 1)
        {
            bool cartasAbiertas = (panelCartasUI != null && panelCartasUI.activeSelf);
            if (botonLanzar != null && botonLanzar.activeSelf != cartasAbiertas)
            {
                botonLanzar.SetActive(cartasAbiertas);
            }
        }
    }

    public void ElegirCartaDeclaradaParaLaRonda()
    {
        int indiceRandom = Random.Range(0, figurasMesa.Length);
        cartaDeclarada = figurasMesa[indiceRandom];

        string textoMostrar = "MESA: " + cartaDeclarada.ToUpper() + "S";

        if (textoMesaDeclarada != null) textoMesaDeclarada.text = textoMostrar;
        if (textoMesaDeclaradaTMP != null) textoMesaDeclaradaTMP.text = textoMostrar;
    }

    public void DesactivarTodaLaUI()
    {
        if (botonLanzar != null) botonLanzar.SetActive(false);
        if (botonVerCartas != null) botonVerCartas.SetActive(false);
        if (botonDesconfiar != null) botonDesconfiar.SetActive(false);
        if (panelCartasUI != null) panelCartasUI.SetActive(false);
    }

    public void ActualizarEstadoUI()
    {
        if (rivales.Count < 1)
        {
            DesactivarTodaLaUI();
            return;
        }

        bool esMiTurno = (turnoActual == 0) && !esperandoAccion;

        if (!esMiTurno)
        {
            DesactivarTodaLaUI();
        }
        else
        {
            bool cartasAbiertas = (panelCartasUI != null && panelCartasUI.activeSelf);
            if (botonVerCartas != null) botonVerCartas.SetActive(!cartasAbiertas); 

            bool puedoDesconfiar = sePuedeDesconfiar && (jugadorDeLaUltimaJugada != 0) && (jugadorDeLaUltimaJugada != -1) && (ultimasCartasTiradas.Count > 0);
            if (botonDesconfiar != null) botonDesconfiar.SetActive(puedoDesconfiar);

            if (botonLanzar != null) botonLanzar.SetActive(cartasAbiertas);
        }
    }

    public void RefrescarNombresUI()
    {
        string nombreJugadorLocal = "Tú"; 

        if (panelesInfoJugadores.Count > 0 && panelesInfoJugadores[0] != null)
        {
            panelesInfoJugadores[0].ActualizarNombre(nombreJugadorLocal);
        }

        for (int i = 0; i < rivales.Count; i++)
        {
            int indiceUI = i + 1;
            if (indiceUI < panelesInfoJugadores.Count && panelesInfoJugadores[indiceUI] != null)
            {
                string nombreRival = rivales[i] != null ? rivales[i].nombreRival : "Rival";
                panelesInfoJugadores[indiceUI].ActualizarNombre(nombreRival);
            }
        }
    }

    public void NotificarJugadorTiro(List<GameObject> cartasTiradas, bool mentio)
    {
        if (turnoActual == 0)
        {
            esperandoAccion = true;
            DesactivarTodaLaUI();

            RegistrarUltimaJugada(0, cartasTiradas, mentio, cartaDeclarada);
            StartCoroutine(AvanzarSiguienteTurno());
        }
    }

    public void RegistrarUltimaJugada(int indiceJugador, List<GameObject> cartas, bool mentio, string declaracion)
    {
        ultimasCartasTiradas.Clear();
        
        jugadorDeLaUltimaJugada = indiceJugador;
        cartaDeclarada = declaracion;
        fueMentira = mentio;
        
        if (cartas != null)
        {
            foreach (var c in cartas)
            {
                if (c != null && !ultimasCartasTiradas.Contains(c))
                {
                    ultimasCartasTiradas.Add(c);
                }
            }
        }

        sePuedeDesconfiar = true;
    }

    private bool JugadorLocalTieneCartas()
    {
        if (mazoManager != null) return mazoManager.ObtenerCantidadCartasJugadorLocal() > 0;
        return true;
    }

    public bool HayCartasEnManoEnLaMesa()
    {
        if (JugadorLocalTieneCartas()) return true;
        foreach (ControladorRival r in rivales)
        {
            if (r != null && r.CantidadCartasRestantes() > 0) return true;
        }
        return false;
    }

    IEnumerator AvanzarSiguienteTurno()
    {
        if (rivales.Count < 1)
        {
            DesactivarTodaLaUI();
            yield break;
        }

        if (!HayCartasEnManoEnLaMesa())
        {
            yield return new WaitForSeconds(1.5f);
            ReiniciarRondaCompleta();
            yield break;
        }

        esperandoAccion = true;
        ActualizarEstadoUI();

        yield return new WaitForSeconds(1.0f);

        int totalJugadores = 1 + rivales.Count;
        turnoActual = (turnoActual + 1) % totalJugadores;

        if (turnoActual == 0)
        {
            if (!JugadorLocalTieneCartas())
            {
                esperandoAccion = false;
                StartCoroutine(AvanzarSiguienteTurno());
            }
            else
            {
                esperandoAccion = false;
                ActualizarEstadoUI();
            }
        }
        else
        {
            ActualizarEstadoUI();

            int indiceRival = turnoActual - 1;
            ControladorRival rivalActivo = rivales[indiceRival];

            if (rivalActivo != null)
            {
                int cartasRestantesBot = rivalActivo.CantidadCartasRestantes();
                if (cartasRestantesBot <= 0)
                {
                    esperandoAccion = false;
                    StartCoroutine(AvanzarSiguienteTurno());
                    yield break;
                }

                yield return new WaitForSeconds(1.5f);

                int maxPosible = Mathf.Min(3, cartasRestantesBot);
                int cartasATirar = Random.Range(1, maxPosible + 1);
                bool rivalMiente = Random.value > 0.5f;

                List<GameObject> cartasDelRival = rivalActivo.TirarCartas(cartasATirar, mazoManager);
                
                if (AudioManager.instancia != null) AudioManager.instancia.ReproducirSonido(AudioManager.instancia.clipTirarCartas);

                RegistrarUltimaJugada(turnoActual, cartasDelRival, rivalMiente, cartaDeclarada);

                yield return new WaitForSeconds(3.5f);
            }

            esperandoAccion = false;
            StartCoroutine(AvanzarSiguienteTurno());
        }
    }

    public void EjecutarDesconfiar(int acusadorIndice)
    {
        if (!sePuedeDesconfiar || jugadorDeLaUltimaJugada == -1 || ultimasCartasTiradas.Count == 0) return;
        if (acusadorIndice == jugadorDeLaUltimaJugada) return;

        sePuedeDesconfiar = false;
        esperandoAccion = true;
        DesactivarTodaLaUI();

        StopAllCoroutines();
        StartCoroutine(RutinaResolverDesconfiar(acusadorIndice));
    }

    IEnumerator RutinaResolverDesconfiar(int acusadorIndice)
    {
        esperandoAccion = true;
        DesactivarTodaLaUI();

        int acusadoIndice = jugadorDeLaUltimaJugada;
        bool mentiraGlobal = false;
        List<bool> mientenIndividualmente = new List<bool>();

        ultimasCartasTiradas.RemoveAll(c => c == null);

        foreach (GameObject carta in ultimasCartasTiradas)
        {
            if (carta != null)
            {
                carta.SetActive(true); 

                string nombre = carta.name.ToLower();
                string decla = cartaDeclarada.ToLower();
                bool esCorrecta = false;

                if (decla == "as" && (nombre.Contains("as") || nombre.Contains("a_") || nombre.StartsWith("a"))) esCorrecta = true;
                else if (decla == "rey" && (nombre.Contains("rey") || nombre.Contains("k_") || nombre.StartsWith("k"))) esCorrecta = true;
                else if (decla == "reina" && (nombre.Contains("reina") || nombre.Contains("q_") || nombre.StartsWith("q"))) esCorrecta = true;
                
                if (nombre.Contains("joker")) esCorrecta = true;

                if (!esCorrecta)
                {
                    mentiraGlobal = true;
                    mientenIndividualmente.Add(true); 
                }
                else
                {
                    mientenIndividualmente.Add(false); 
                }
            }
            else
            {
                mientenIndividualmente.Add(true);
            }
        }

        if (mazoManager != null)
        {
            mazoManager.RevelarUltimasCartasConDetalle(ultimasCartasTiradas, mientenIndividualmente);
        }

        yield return new WaitForSeconds(3.0f);

        int perdedorIndice = mentiraGlobal ? acusadoIndice : acusadorIndice;
        yield return StartCoroutine(RutinaGatillazo(perdedorIndice));
    }

    IEnumerator RutinaGatillazo(int jugadorCastigadoIndice)
    {
        esperandoAccion = true;
        DesactivarTodaLaUI();

        Animator animatorObjetivo = null;
        ControladorRival rivalMortal = null;
        GameObject revolverIndividual = null;

        // Identificar quién recibe el castigo
        if (jugadorCastigadoIndice == 0)
        {
            if (jugadorLocal != null)
            {
                animatorObjetivo = jugadorLocal.animatorJugador;
                revolverIndividual = jugadorLocal.revolverPropio; 
                
                // Disparamos la animación y el método que equipa el revólver en la mano
                jugadorLocal.PenalizarYTomarArma();
            }
        }
        else
        {
            int indiceRival = jugadorCastigadoIndice - 1;
            if (indiceRival < rivales.Count && rivales[indiceRival] != null)
            {
                rivalMortal = rivales[indiceRival];
                animatorObjetivo = rivalMortal.animatorRival;
                revolverIndividual = rivalMortal.revolverRival; 
                
                if (rivalMortal.manoSocketRival != null && revolverIndividual != null)
                {
                    revolverIndividual.transform.SetParent(rivalMortal.manoSocketRival);
                    revolverIndividual.transform.localPosition = Vector3.zero;
                    revolverIndividual.transform.localRotation = Quaternion.identity;
                }
            }
        }

        yield return new WaitForSeconds(1.5f);

        // APUNTAR Y DISPARAR
        if (animatorObjetivo != null) animatorObjetivo.SetBool("Apuntando", true);
        yield return new WaitForSeconds(1.5f);

        if (animatorObjetivo != null) animatorObjetivo.SetTrigger("Disparar");

        bool murio = false;
        if (revolverGeneral != null) murio = revolverGeneral.ApretarGatillo();

        if (murio)
        {
            if (AudioManager.instancia != null) AudioManager.instancia.ReproducirSonido(AudioManager.instancia.clipDisparoMortal);
        }
        else
        {
            if (AudioManager.instancia != null) AudioManager.instancia.ReproducirSonido(AudioManager.instancia.clipGatilloVacio);
        }

        yield return new WaitForSeconds(1.0f);

        if (murio)
        {
            if (animatorObjetivo != null) animatorObjetivo.SetTrigger("Morir");
            if (mazoManager != null) mazoManager.LimpiarManoDeJugador(jugadorCastigadoIndice);

            yield return new WaitForSeconds(2.5f);

            if (jugadorCastigadoIndice > 0 && rivalMortal != null)
            {
                rivales.Remove(rivalMortal);
            }
            else if (jugadorCastigadoIndice == 0)
            {
                DesactivarTodaLaUI();
                yield break;
            }
        }
        else
        {
            if (animatorObjetivo != null) animatorObjetivo.SetBool("Apuntando", false);
            
            // SI SOBREVIVE, devolvemos el revólver a su posición original en la mesa
            if (jugadorCastigadoIndice == 0 && jugadorLocal != null)
            {
                jugadorLocal.RegresarRevolverAMesa();
            }

            yield return new WaitForSeconds(1.0f);
        }

        if (mazoManager != null) mazoManager.LimpiarCartasReveladasDeMesa(ultimasCartasTiradas);
        if (rivales.Count < 1)
        {
            DesactivarTodaLaUI();
            yield break;
        }

        ReiniciarRondaCompleta();
    }

    public void ReiniciarRondaCompleta()
    {
        ultimasCartasTiradas.Clear();
        jugadorDeLaUltimaJugada = -1;
        sePuedeDesconfiar = false;
        ElegirCartaDeclaradaParaLaRonda();

        if (mazoManager != null) mazoManager.IniciarNuevaRonda();

        esperandoAccion = false;
        if (turnoActual >= (1 + rivales.Count)) turnoActual = 0;

        StartCoroutine(AvanzarSiguienteTurno());
    }
}